using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Contracts;

[assembly: InternalsVisibleTo("Pharos.Client.Tests")]

namespace Pharos.Client.App.Identity.Services;

/// <summary>
/// Monitors authenticated client inactivity and owns automatic logout.
/// Activity restarts the timeout. The final 30 seconds show a warning.
/// </summary>
public sealed class AutoLogoutService : IAutoLogoutService, IDisposable
{
    private static readonly TimeSpan _logoutCountdownDuration = TimeSpan.FromSeconds(30);
    private readonly UiSettingsService _uiSettingsService;
    private readonly ICurrentUserNotificationProvider _userNotifications;
    private readonly ICurrentInstrumentNotificationProvider _instrumentNotifications;
    private readonly AuthenticationStateProvider _authentication;
    private readonly NavigationManager _navigation;
    private readonly ILogger<AutoLogoutService> _logger;
    private readonly SynchronizationContext? _synchronizationContext;
    // Methods ending in UnderLock must hold this lock.
    private readonly Lock _lock = new();
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "The monitoring task owns and disposes its cancellation source.")]
    private CancellationTokenSource? _timerCancellation;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private Task? _startupTask;
    private Task _settingsRefreshTask = Task.CompletedTask;
    private TimeSpan? _inactivityTimeout;
    // True after either configured settings or the startup fallback have been applied.
    private bool _settingsInitialized;
    private bool _authenticated;
    // Logout deadline during the warning stage; otherwise null.
    private DateTimeOffset? _logoutAt;
    private bool _loggingOut;
    private bool _disposed;
    private readonly bool _isInstrument;

    public event Func<int, Task>? UserIdle;
    public event Func<Task>? UserActive;

    public AutoLogoutService(UiSettingsService uiSettingsService,
        ICurrentUserNotificationProvider userNotifications,
        ICurrentInstrumentNotificationProvider instrumentNotifications,
        AuthenticationStateProvider authentication,
        IClientDeviceService device,
        NavigationManager navigation,
        ILogger<AutoLogoutService> logger)
    {
        _uiSettingsService = uiSettingsService;
        _userNotifications = userNotifications;
        _instrumentNotifications = instrumentNotifications;
        _authentication = authentication;
        _isInstrument = device.ClientDeviceType == ClientDeviceType.Instrument;
        _navigation = navigation;
        _logger = logger;
        _synchronizationContext = SynchronizationContext.Current;
    }

    /// <summary>
    /// Starts inactivity monitoring once.
    /// </summary>
    public Task StartAsync()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            // Share one startup task across callers.
            return _startupTask ??= InitializeAsync(_lifetimeCancellation.Token);
        }
    }

    /// <summary>
    /// Subscribes once and reports startup failures to the caller.
    /// </summary>
    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _uiSettingsService.SettingsChanged += ApplySettingsAsync;
        _userNotifications.AutoLogoutSettingsChanged += ApplySettingsAsync;
        if (_isInstrument)
        {
            _instrumentNotifications.LeftSampleDrawerLockStateChanged += OnLockedCompartmentStateChanged;
            _instrumentNotifications.RightSampleDrawerLockStateChanged += OnLockedCompartmentStateChanged;
            _instrumentNotifications.ReagentDrawerLockStateChanged += OnLockedCompartmentStateChanged;
            _instrumentNotifications.WasteDrawerLockStateChanged += OnLockedCompartmentStateChanged;
            _instrumentNotifications.HoodLockStateChanged += OnLockedCompartmentStateChanged;
            _instrumentNotifications.BottleDrawerLockStateChanged += OnCompartmentStateChanged;
            _instrumentNotifications.LiquidDrawerLockStateChanged += OnCompartmentStateChanged;
        }
        _authentication.AuthenticationStateChanged += OnAuthenticationStateChanged;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UpdateAuthenticationAsync(_authentication.GetAuthenticationStateAsync());
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            // Disposal cancels startup and ignores a disposed session's in-flight request failure.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Automatic logout initialization failed.");
            throw;
        }
    }

    private void OnCompartmentStateChanged(LockState state)
    {
        StayLoggedIn();
    }

    private void OnLockedCompartmentStateChanged(LockState state, bool isUnlockInProgress)
    {
        StayLoggedIn();
    }

    /// <summary>
    /// Starts an authentication update when the user's state changes.
    /// </summary>
    private void OnAuthenticationStateChanged(Task<AuthenticationState> state)
    {
        _ = UpdateAuthenticationSafelyAsync(state);
    }

    /// <summary>
    /// Updates authentication and logs any failure.
    /// </summary>
    private async Task UpdateAuthenticationSafelyAsync(Task<AuthenticationState> state)
    {
        try
        {
            await UpdateAuthenticationAsync(state);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to update automatic logout authentication state.");
        }
    }

    /// <summary>
    /// Stops monitoring users who are not logged in and loads settings for logged-in users.
    /// </summary>
    private async Task UpdateAuthenticationAsync(Task<AuthenticationState> state)
    {
        var authenticated = (await state).User.Identity?.IsAuthenticated == true;
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _authenticated = authenticated;
            if (!authenticated)
            {
                StopTimerUnderLock();
                _inactivityTimeout = null;
                _settingsInitialized = false;
                return;
            }
        }

        await ApplySettingsAsync();
    }

    /// <summary>
    /// Queues settings reads so responses are applied in order.
    /// </summary>
    private Task ApplySettingsAsync()
    {
        lock (_lock)
        {
            if (_disposed || !_authenticated)
            {
                return Task.CompletedTask;
            }

            return _settingsRefreshTask = RefreshSettingsAsync(_settingsRefreshTask, _lifetimeCancellation.Token);
        }
    }

    /// <summary>
    /// Reads settings in order. An initial failure uses a ten-minute fallback; later failures keep the current timer.
    /// </summary>
    private async Task RefreshSettingsAsync(Task previousRefresh, CancellationToken cancellationToken)
    {
        try
        {
            await previousRefresh.WaitAsync(cancellationToken);

            lock (_lock)
            {
                if (_disposed || !_authenticated)
                {
                    return;
                }
            }

            var settings = (await _uiSettingsService.GetSettingsAsync().WaitAsync(cancellationToken)).SecuritySettings;
            lock (_lock)
            {
                ApplySettingsUnderLock(settings);
            }
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            // Disposal cancels queued reads.
        }
        catch (Exception exception)
        {
            lock (_lock)
            {
                if (_disposed || !_authenticated)
                {
                    return;
                }

                if (!_settingsInitialized)
                {
                    ApplySettingsUnderLock(new SecuritySettings
                    {
                        AutoLogoutEnabled = true,
                        AutoLogoutTimeoutMinutes = 10
                    });
                    _logger.LogWarning(exception, "Automatic logout settings could not be loaded; using the ten-minute fallback.");
                    return;
                }
            }

            // The next settings notification or reconnect will try again.
            _logger.LogWarning(exception, "Automatic logout settings refresh failed; keeping the current settings.");
        }
    }

    /// <summary>
    /// Restarts monitoring only when the timeout setting changes.
    /// </summary>
    private void ApplySettingsUnderLock(SecuritySettings settings)
    {
        // The session may have ended during the settings request.
        if (_disposed || !_authenticated)
        {
            return;
        }

        var timeout = settings.AutoLogoutEnabled
            ? TimeSpan.FromMinutes(settings.AutoLogoutTimeoutMinutes)
            : (TimeSpan?)null;
        _settingsInitialized = true;
        // Unchanged settings must not reset inactivity.
        if (_inactivityTimeout != timeout)
        {
            _inactivityTimeout = timeout;
            RestartTimerUnderLock();
        }
    }

    /// <summary>
    /// Restarts the timeout after user activity.
    /// </summary>
    public void StayLoggedIn()
    {
        lock (_lock)
        {
            if (_disposed || !_authenticated || _loggingOut)
            {
                return;
            }

            RestartTimerUnderLock();
        }
    }

    /// <summary>
    /// Updates the warning deadline and notifies its subscribers.
    /// </summary>
    private Task SetLogoutDeadlineUnderLock(DateTimeOffset? deadline)
    {
        if (_logoutAt == deadline)
        {
            return Task.CompletedTask;
        }

        _logoutAt = deadline;
        return NotifyUserStateAsync(deadline);
    }

    /// <summary>
    /// Shows or dismisses the warning, ignoring outdated updates.
    /// </summary>
    private async Task NotifyUserStateAsync(DateTimeOffset? deadline)
    {
        try
        {
            // Finish updating timer state before notifying the UI.
            await Task.Yield();
            Task notification;
            lock (_lock)
            {
                if (_logoutAt != deadline)
                {
                    // A newer state has replaced this warning.
                    return;
                }

                // Start handlers under the lock so a newer state cannot be delivered before this one.
                if (deadline.HasValue)
                {
                    var secondsRemaining = (int)Math.Ceiling((deadline.Value - DateTimeOffset.UtcNow).TotalSeconds);
                    if (secondsRemaining <= 0)
                    {
                        return;
                    }

                    var handlers = UserIdle;
                    notification = handlers is null ? Task.CompletedTask : Task.WhenAll(
                        handlers.GetInvocationList().Cast<Func<int, Task>>().Select(handler => handler(secondsRemaining)));
                }
                else
                {
                    var handlers = UserActive;
                    notification = handlers is null ? Task.CompletedTask : Task.WhenAll(
                        handlers.GetInvocationList().Cast<Func<Task>>().Select(handler => handler()));
                }
            }

            await notification;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to notify the automatic logout user state change.");
        }
    }

    /// <summary>
    /// Replaces the current timer when automatic logout is enabled.
    /// </summary>
    private void RestartTimerUnderLock()
    {
        StopTimerUnderLock();
        if (_inactivityTimeout is not null && !_loggingOut)
        {
            var timer = new CancellationTokenSource();
            _timerCancellation = timer;
            _ = MonitorInactivityAsync(_inactivityTimeout.Value, timer);
        }
    }

    internal Task StartLogoutCountdownAsync()
    {
        // Lets tests enter the warning stage without waiting.
        lock (_lock)
        {
            if (_timerCancellation is null || _inactivityTimeout is null)
            {
                return Task.CompletedTask;
            }

            var remaining = _inactivityTimeout.Value < _logoutCountdownDuration ? _inactivityTimeout.Value : _logoutCountdownDuration;
            return StartLogoutCountdownAsync(_timerCancellation, DateTimeOffset.UtcNow.Add(remaining));
        }
    }

    /// <summary>
    /// Starts the warning if this timer is still active.
    /// </summary>
    private Task StartLogoutCountdownAsync(CancellationTokenSource expectedTimer, DateTimeOffset logoutAt)
    {
        lock (_lock)
        {
            if (!IsTimerActiveUnderLock(expectedTimer) || logoutAt <= DateTimeOffset.UtcNow)
            {
                return Task.CompletedTask;
            }
            return SetLogoutDeadlineUnderLock(logoutAt);
        }
    }

    internal Task HandleTimeoutAsync()
    {
        // Lets tests expire the current timer without waiting.
        lock (_lock)
        {
            return _timerCancellation is not null ? HandleTimeoutAsync(_timerCancellation) : Task.CompletedTask;
        }
    }

    /// <summary>
    /// Starts logout only if this timer still belongs to the active session.
    /// </summary>
    private async Task HandleTimeoutAsync(CancellationTokenSource expectedTimer)
    {
        var state = await _authentication.GetAuthenticationStateAsync();
        lock (_lock)
        {
            if (state.User.Identity?.IsAuthenticated != true || !IsTimerActiveUnderLock(expectedTimer))
            {
                return;
            }

            // Stop accepting activity once logout has started.
            _loggingOut = true;
            StopTimerUnderLock();
        }

        if (_synchronizationContext is null)
        {
            NavigateToLogout();
        }
        else
        {
            // Navigation must run on the UI context.
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _synchronizationContext.Post(_ =>
            {
                try
                {
                    NavigateToLogout();
                    completion.TrySetResult();
                }
                catch (Exception exception)
                {
                    completion.TrySetException(exception);
                }
            }, null);
            await completion.Task;
        }
    }

    /// <summary>
    /// Navigates to server logout with the device's return page.
    /// </summary>
    private void NavigateToLogout()
    {
        lock (_lock)
        {
            if (_disposed || !_authenticated)
            {
                return;
            }
        }

        var returnUrl = _isInstrument ? PageRoute.LockScreen : AccountRoutes.Login;
        // Full navigation logs out even with unsaved edits; those edits are discarded.
        _navigation.NavigateTo($"{AccountRoutes.Logout}?returnUrl={Uri.EscapeDataString(returnUrl)}",
            new NavigationOptions { ForceLoad = true, ReplaceHistoryEntry = true });
    }

    /// <summary>
    /// Schedules the warning and logout, then disposes this timer.
    /// </summary>
    private async Task MonitorInactivityAsync(TimeSpan timeout, CancellationTokenSource timer)
    {
        try
        {
            var logoutAt = DateTimeOffset.UtcNow.Add(timeout);
            var delayUntilCountdown = timeout > _logoutCountdownDuration ? timeout - _logoutCountdownDuration : TimeSpan.Zero;
            // Register both deadlines immediately. A delayed countdown handler must not extend the session.
            var waitForCountdown = Task.Delay(delayUntilCountdown, timer.Token);
            var waitForLogout = Task.Delay(timeout, timer.Token);
            // Do not invoke navigation or event handlers synchronously while starting a timer under the lock.
            await Task.Yield();
            await waitForCountdown;
            // A slow warning handler must not delay logout.
            _ = StartLogoutCountdownAsync(timer, logoutAt);

            await waitForLogout;
            await HandleTimeoutAsync(timer);
        }
        catch (OperationCanceledException) when (timer.IsCancellationRequested)
        {
            // Activity, authentication, settings, or disposal stopped this deadline.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to automatically log out the user.");
        }
        finally
        {
            lock (_lock)
            {
                if (ReferenceEquals(_timerCancellation, timer))
                {
                    _timerCancellation = null;
                }
            }

            timer.Dispose();
        }
    }

    /// <summary>
    /// Checks whether this timer still controls the current session.
    /// </summary>
    private bool IsTimerActiveUnderLock(CancellationTokenSource expectedTimer)
    {
        // An old timer must not log out a newly active user.
        return !_disposed && _authenticated && !_loggingOut && _inactivityTimeout is not null
            && ReferenceEquals(_timerCancellation, expectedTimer) && !expectedTimer.IsCancellationRequested;
    }

    /// <summary>
    /// Cancels the current timer and dismisses the warning.
    /// </summary>
    private void StopTimerUnderLock()
    {
        _timerCancellation?.Cancel();
        _timerCancellation = null;
        _ = SetLogoutDeadlineUnderLock(null);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _lifetimeCancellation.Cancel();
            _lifetimeCancellation.Dispose();
            _uiSettingsService.SettingsChanged -= ApplySettingsAsync;
            _userNotifications.AutoLogoutSettingsChanged -= ApplySettingsAsync;
            _authentication.AuthenticationStateChanged -= OnAuthenticationStateChanged;
            if (_isInstrument)
            {
                _instrumentNotifications.LeftSampleDrawerLockStateChanged -= OnLockedCompartmentStateChanged;
                _instrumentNotifications.RightSampleDrawerLockStateChanged -= OnLockedCompartmentStateChanged;
                _instrumentNotifications.ReagentDrawerLockStateChanged -= OnLockedCompartmentStateChanged;
                _instrumentNotifications.WasteDrawerLockStateChanged -= OnLockedCompartmentStateChanged;
                _instrumentNotifications.HoodLockStateChanged -= OnLockedCompartmentStateChanged;
                _instrumentNotifications.BottleDrawerLockStateChanged -= OnCompartmentStateChanged;
                _instrumentNotifications.LiquidDrawerLockStateChanged -= OnCompartmentStateChanged;
            }
            StopTimerUnderLock();
        }
    }
}
