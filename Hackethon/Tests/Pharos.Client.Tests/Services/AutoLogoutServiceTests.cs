using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pharos.Client.App.Identity;
using Pharos.Client.App.Identity.Services;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Contracts;

namespace Pharos.Client.Tests.Services;

public class AutoLogoutServiceTests
{
    private readonly Mock<ISingletonDataSource<UiSettings>> _data = new();
    private readonly Mock<ICurrentUserNotificationProvider> _notifications = new();
    private readonly Mock<ICurrentInstrumentNotificationProvider> _instrumentNotifications = new();
    private readonly TestAuthenticationStateProvider _authentication = new();
    private readonly Mock<IClientDeviceService> _device = new();
    private readonly AutoLogoutNavigationManager _navigation = new();
    private readonly UiSettingsService _settings;
    private bool _enabled = true;
    private int _timeoutMinutes = 10;
    private Func<Task>? _serverSettingsChanged;

    public AutoLogoutServiceTests()
    {
        _device.SetupGet(x => x.ClientDeviceType).Returns(ClientDeviceType.Instrument);
        _notifications.SetupAdd(x => x.AutoLogoutSettingsChanged += It.IsAny<Func<Task>>())
            .Callback<Func<Task>>(handler => _serverSettingsChanged += handler);
        _notifications.SetupRemove(x => x.AutoLogoutSettingsChanged -= It.IsAny<Func<Task>>())
            .Callback<Func<Task>>(handler => _serverSettingsChanged -= handler);
        _data.Setup(x => x.GetAsync()).ReturnsAsync(() => new UiSettings
        {
            SecuritySettings = new SecuritySettings { AutoLogoutEnabled = _enabled, AutoLogoutTimeoutMinutes = _timeoutMinutes }
        });
        _settings = new UiSettingsService(_data.Object);
    }

    private AutoLogoutService CreateSut()
    {
        return new AutoLogoutService(_settings, _notifications.Object, _instrumentNotifications.Object, _authentication,
            _device.Object, _navigation, NullLogger<AutoLogoutService>.Instance);
    }

    private void RaiseCompartmentNotifications()
    {
        _instrumentNotifications.Raise(x => x.LeftSampleDrawerLockStateChanged += null, LockState.Open, false);
        _instrumentNotifications.Raise(x => x.RightSampleDrawerLockStateChanged += null, LockState.Open, false);
        _instrumentNotifications.Raise(x => x.ReagentDrawerLockStateChanged += null, LockState.Open, false);
        _instrumentNotifications.Raise(x => x.BottleDrawerLockStateChanged += null, LockState.Open);
        _instrumentNotifications.Raise(x => x.LiquidDrawerLockStateChanged += null, LockState.Open);
        _instrumentNotifications.Raise(x => x.WasteDrawerLockStateChanged += null, LockState.Open, false);
        _instrumentNotifications.Raise(x => x.HoodLockStateChanged += null, LockState.Open, false);
    }

    private Task NotifyServerSettingsChangedAsync()
    {
        return Task.WhenAll(
            (_serverSettingsChanged?.GetInvocationList() ?? []).Cast<Func<Task>>().Select(handler => handler()));
    }

    [Fact]
    public async Task SettingsRefreshFailure_NextNotificationEnablesMonitoring()
    {
        _enabled = false;
        using var sut = CreateSut();
        await sut.StartAsync();
        _data.SetupSequence(x => x.GetAsync())
            .ThrowsAsync(new HttpRequestException("Temporary settings failure."))
            .ReturnsAsync(new UiSettings
            {
                SecuritySettings = new SecuritySettings { AutoLogoutEnabled = true, AutoLogoutTimeoutMinutes = 10 }
            });

        await NotifyServerSettingsChangedAsync().WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        _data.Verify(x => x.GetAsync(), Times.Exactly(2));
        await NotifyServerSettingsChangedAsync();

        var warned = false;
        sut.UserIdle += _ => { warned = true; return Task.CompletedTask; };
        await sut.StartLogoutCountdownAsync();
        warned.Should().BeTrue();
        _data.Verify(x => x.GetAsync(), Times.Exactly(3));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SettingsRefreshes_ReadAndApplyInOrder(bool latestEnabled)
    {
        using var sut = CreateSut();
        await sut.StartAsync();
        var firstRead = new TaskCompletionSource<UiSettings>(TaskCreationOptions.RunContinuationsAsynchronously);
        _data.SetupSequence(x => x.GetAsync()).Returns(firstRead.Task).ReturnsAsync(new UiSettings
        {
            SecuritySettings = new SecuritySettings { AutoLogoutEnabled = latestEnabled, AutoLogoutTimeoutMinutes = 10 }
        });
        var firstRefresh = _settings.NotifySettingsChangedAsync();
        var secondRefresh = NotifyServerSettingsChangedAsync();
        // The second notification must wait before starting its HTTP read.
        _data.Verify(x => x.GetAsync(), Times.Exactly(2));
        firstRead.SetResult(new UiSettings
        {
            SecuritySettings = new SecuritySettings { AutoLogoutEnabled = !latestEnabled, AutoLogoutTimeoutMinutes = 10 }
        });
        await Task.WhenAll(firstRefresh, secondRefresh).WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        var warned = false;
        sut.UserIdle += _ => { warned = true; return Task.CompletedTask; };
        await sut.StartLogoutCountdownAsync();
        warned.Should().Be(latestEnabled);
        _data.Verify(x => x.GetAsync(), Times.Exactly(3));
    }

    [Fact]
    public async Task SettingsRefreshFailure_KeepsMonitoringAndAllowsTheNextRead()
    {
        _timeoutMinutes = 20;
        using var sut = CreateSut();
        await sut.StartAsync();
        var dismissed = false;
        sut.UserActive += () => { dismissed = true; return Task.CompletedTask; };
        await sut.StartLogoutCountdownAsync();
        _data.Setup(x => x.GetAsync()).ThrowsAsync(new HttpRequestException("Offline."));
        var refresh = NotifyServerSettingsChangedAsync();
        var queuedRefresh = _settings.NotifySettingsChangedAsync();
        await Task.WhenAll(refresh, queuedRefresh);
        dismissed.Should().BeFalse();
        var warned = false;
        sut.UserIdle += _ => { warned = true; return Task.CompletedTask; };
        await sut.StartLogoutCountdownAsync();
        warned.Should().BeTrue();

        sut.Dispose();
        await Task.WhenAll(refresh, queuedRefresh).WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        _data.Verify(x => x.GetAsync(), Times.Exactly(3));
    }

    [Fact]
    public async Task StartupSettingsFailure_UsesTenMinutesAndKeepsDeadlineOnRepeatedFailureOrUnchangedSettings()
    {
        _data.SetupSequence(x => x.GetAsync())
            .ThrowsAsync(new HttpRequestException("Temporary settings failure."))
            .ThrowsAsync(new HttpRequestException("Still offline."))
            .ReturnsAsync(new UiSettings
            {
                SecuritySettings = new SecuritySettings { AutoLogoutEnabled = true, AutoLogoutTimeoutMinutes = 10 }
            });
        using var sut = CreateSut();
        var initialization = sut.StartAsync();
        sut.StartAsync().Should().BeSameAs(initialization);
        await initialization.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        _data.Verify(x => x.GetAsync(), Times.Once);

        var countdown = false;
        var dismissed = false;
        sut.UserIdle += seconds => { countdown = true; return Task.CompletedTask; };
        sut.UserActive += () => { dismissed = true; return Task.CompletedTask; };
        await sut.StartLogoutCountdownAsync();
        countdown.Should().BeTrue();
        await NotifyServerSettingsChangedAsync();
        await NotifyServerSettingsChangedAsync();
        // Loading the configured ten minutes must leave the fallback's warning and deadline unchanged.
        dismissed.Should().BeFalse();
        _data.Verify(x => x.GetAsync(), Times.Exactly(3));
        _notifications.VerifyAdd(x => x.AutoLogoutSettingsChanged += It.IsAny<Func<Task>>(), Times.Once);
        await sut.HandleTimeoutAsync();
        _navigation.LastUri.Should().Be($"{Pharos.Contracts.Security.AccountRoutes.Logout}?returnUrl={Uri.EscapeDataString(Pharos.Client.Common.App.PageRouting.PageRoute.LockScreen)}");
    }

    [Fact]
    public async Task StartupSettingsFailure_StartsMonitoringWithoutRetryOrNotification()
    {
        _data.Setup(x => x.GetAsync()).ThrowsAsync(new HttpRequestException("Offline."));
        using var sut = CreateSut();
        var initialization = sut.StartAsync();
        await initialization.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        _data.Verify(x => x.GetAsync(), Times.Once);
        sut.StartAsync().Should().BeSameAs(initialization);
        _notifications.VerifyAdd(x => x.AutoLogoutSettingsChanged += It.IsAny<Func<Task>>(), Times.Once);
        await sut.HandleTimeoutAsync();
        _navigation.LastUri.Should().Contain(Pharos.Contracts.Security.AccountRoutes.Logout);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StartupSettingsFailure_LaterSettingsReplaceFallback(bool enabled)
    {
        _data.SetupSequence(x => x.GetAsync())
            .ThrowsAsync(new InvalidOperationException("Invalid settings."))
            .ReturnsAsync(new UiSettings
            {
                SecuritySettings = new SecuritySettings { AutoLogoutEnabled = enabled, AutoLogoutTimeoutMinutes = 20 }
            });
        using var sut = CreateSut();
        await sut.StartAsync().WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        _data.Verify(x => x.GetAsync(), Times.Once);

        var dismissed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        sut.UserActive += () => { dismissed.TrySetResult(); return Task.CompletedTask; };
        await sut.StartLogoutCountdownAsync();
        await NotifyServerSettingsChangedAsync();
        await dismissed.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        _data.Verify(x => x.GetAsync(), Times.Exactly(2));
        var warned = false;
        sut.UserIdle += _ => { warned = true; return Task.CompletedTask; };
        await sut.StartLogoutCountdownAsync();
        warned.Should().Be(enabled);
        await sut.HandleTimeoutAsync();
        (_navigation.LastUri is not null).Should().Be(enabled);
    }

    [Fact]
    public async Task SettingsRefreshFailure_KeepsConfiguredDisabledSetting()
    {
        _enabled = false;
        using var sut = CreateSut();
        await sut.StartAsync();
        _data.Setup(x => x.GetAsync()).ThrowsAsync(new HttpRequestException("Offline."));

        await NotifyServerSettingsChangedAsync();
        var warned = false;
        sut.UserIdle += _ => { warned = true; return Task.CompletedTask; };
        await sut.StartLogoutCountdownAsync();
        await sut.HandleTimeoutAsync();

        warned.Should().BeFalse();
        _navigation.LastUri.Should().BeNull();
    }

    [Fact]
    public async Task Dispose_CancelsPendingAndQueuedSettingsReads()
    {
        var sut = CreateSut();
        await sut.StartAsync();
        var request = new TaskCompletionSource<UiSettings>(TaskCreationOptions.RunContinuationsAsynchronously);
        _data.Setup(x => x.GetAsync()).Returns(request.Task);
        var refresh = NotifyServerSettingsChangedAsync();
        var queuedRefresh = _settings.NotifySettingsChangedAsync();
        sut.Dispose();

        await Task.WhenAll(refresh, queuedRefresh).WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        _data.Verify(x => x.GetAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task DisposeDuringSettingsRequest_IgnoresItsLaterFailure()
    {
        var request = new TaskCompletionSource<UiSettings>(TaskCreationOptions.RunContinuationsAsynchronously);
        _data.Setup(x => x.GetAsync()).Returns(request.Task);
        var sut = CreateSut();
        var initialization = sut.StartAsync();
        sut.Dispose();
        request.SetException(new HttpRequestException("Request failed after the session closed."));

        await initialization.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        _data.Verify(x => x.GetAsync(), Times.Once);
        _navigation.LastUri.Should().BeNull();
    }

    [Fact]
    public void IdentityModule_RegistersAutoLogoutAsScopedService()
    {
        var services = new ServiceCollection();
        IdentityModule.Instance.AddServices(services);
        services.AddSingleton(_settings);
        services.AddSingleton(_notifications.Object);
        services.AddSingleton(_instrumentNotifications.Object);
        services.AddSingleton<AuthenticationStateProvider>(_authentication);
        services.AddSingleton(_device.Object);
        services.AddSingleton<NavigationManager>(_navigation);
        services.AddSingleton<Microsoft.Extensions.Logging.ILogger<AutoLogoutService>>(NullLogger<AutoLogoutService>.Instance);
        using var provider = services.BuildServiceProvider();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var service = first.ServiceProvider.GetRequiredService<IAutoLogoutService>();
        first.ServiceProvider.GetRequiredService<IAutoLogoutService>().Should().BeSameAs(service);
        second.ServiceProvider.GetRequiredService<IAutoLogoutService>().Should().NotBeSameAs(service);
    }

    [Fact]
    public async Task UserIdle_ReportsThirtySecondsRemaining()
    {
        using var sut = CreateSut();
        int? secondsRemaining = null;
        sut.UserIdle += seconds => { secondsRemaining = seconds; return Task.CompletedTask; };
        await sut.StartAsync();
        await sut.StartLogoutCountdownAsync();
        secondsRemaining.Should().Be(30);
    }

    [Theory]
    [InlineData("ui")]
    [InlineData("compartment")]
    [InlineData("disabled")]
    [InlineData("timeout")]
    [InlineData("authentication")]
    [InlineData("logout")]
    [InlineData("dispose")]
    public async Task UserActive_DismissesCountdownForEveryResetReason(string reason)
    {
        using var sut = CreateSut();
        var changes = new List<int?>();
        var dismissed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        sut.UserIdle += seconds => { changes.Add(seconds); return Task.CompletedTask; };
        sut.UserActive += () =>
        {
            changes.Add(null);
            dismissed.TrySetResult();
            return Task.CompletedTask;
        };
        await sut.StartAsync();
        changes.Should().BeEmpty();
        await sut.StartLogoutCountdownAsync();
        changes.Should().ContainSingle().Which.Should().NotBeNull();
        switch (reason)
        {
            case "ui": sut.StayLoggedIn(); break;
            case "compartment": RaiseCompartmentNotifications(); break;
            case "disabled": _enabled = false; await NotifyServerSettingsChangedAsync(); break;
            case "timeout": _timeoutMinutes = 20; await NotifyServerSettingsChangedAsync(); break;
            case "authentication": _authentication.SetAuthenticated(false); break;
            case "logout": await sut.HandleTimeoutAsync(); break;
            case "dispose": sut.Dispose(); break;
        }
        await dismissed.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        changes.Should().HaveCount(2);
        changes[1].Should().BeNull();
    }

    [Theory]
    [InlineData(ClientDeviceType.Instrument, true)]
    [InlineData(ClientDeviceType.Desktop, false)]
    [InlineData(ClientDeviceType.Tablet, false)]
    [InlineData(ClientDeviceType.Phone, false)]
    [InlineData(ClientDeviceType.Auto, false)]
    public async Task CompartmentNotifications_ResetCountdownOnlyInInstrumentMode(
        ClientDeviceType device, bool expectedReset)
    {
        _device.SetupGet(x => x.ClientDeviceType).Returns(device);
        using var sut = CreateSut();
        var dismissed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        sut.UserActive += () =>
        {
            dismissed.TrySetResult();
            return Task.CompletedTask;
        };
        await sut.StartAsync();
        await sut.StartLogoutCountdownAsync();
        RaiseCompartmentNotifications();
        if (expectedReset)
        {
            await dismissed.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        }
        dismissed.Task.IsCompleted.Should().Be(expectedReset);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueuedCountdownChanges_OnlyDeliverTheLatestState(bool showAnotherCountdown)
    {
        using var sut = CreateSut();
        var changes = new List<int?>();
        sut.UserIdle += seconds => { changes.Add(seconds); return Task.CompletedTask; };
        sut.UserActive += () => { changes.Add(null); return Task.CompletedTask; };
        await sut.StartAsync();
        var pending = new QueuedSynchronizationContext();
        var previous = SynchronizationContext.Current;
        Task firstCountdown;
        Task latestCountdown;
        try
        {
            SynchronizationContext.SetSynchronizationContext(pending);
            firstCountdown = sut.StartLogoutCountdownAsync();
            sut.StayLoggedIn();
            latestCountdown = showAnotherCountdown ? sut.StartLogoutCountdownAsync() : Task.CompletedTask;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        pending.RunPending();
        await Task.WhenAll(firstCountdown, latestCountdown);
        changes.Should().ContainSingle();
        changes[0].HasValue.Should().Be(showAnotherCountdown);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Activity_DismissesOnceAndAllowsAnotherCountdown(bool compartment)
    {
        using var sut = CreateSut();
        var changes = new List<int?>();
        var dismissed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        sut.UserIdle += seconds => { changes.Add(seconds); return Task.CompletedTask; };
        sut.UserActive += () =>
        {
            changes.Add(null);
            dismissed.TrySetResult();
            return Task.CompletedTask;
        };
        void RecordActivity()
        {
            if (compartment) { RaiseCompartmentNotifications(); }
            else { sut.StayLoggedIn(); }
        }
        await sut.StartAsync();
        RecordActivity();
        changes.Should().BeEmpty();
        await sut.StartLogoutCountdownAsync();
        RecordActivity();
        RecordActivity();
        await dismissed.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        changes.Should().HaveCount(2);
        await sut.StartLogoutCountdownAsync();
        changes.Should().HaveCount(3);
        changes[2].Should().NotBeNull();
    }

    [Theory]
    [InlineData(ClientDeviceType.Instrument)]
    [InlineData(ClientDeviceType.Desktop)]
    public async Task Timeout_NavigatesDirectlyToServerLogoutWithDeviceReturnUrl(ClientDeviceType device)
    {
        _device.SetupGet(x => x.ClientDeviceType).Returns(device);
        using var sut = CreateSut();
        await sut.StartAsync();
        await sut.HandleTimeoutAsync();
        await _navigation.Navigated.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        var returnUrl = device == ClientDeviceType.Instrument
            ? Pharos.Client.Common.App.PageRouting.PageRoute.LockScreen : Pharos.Contracts.Security.AccountRoutes.Login;
        _navigation.LastUri.Should().Be($"{Pharos.Contracts.Security.AccountRoutes.Logout}?returnUrl={Uri.EscapeDataString(returnUrl)}");
        _navigation.Options!.Value.ForceLoad.Should().BeTrue();
        _navigation.Options.Value.ReplaceHistoryEntry.Should().BeTrue();
    }

    [Fact]
    public async Task Timeout_BypassesInternalNavigationGuardAndStopsMonitoring()
    {
        var navigation = new GuardedAutoLogoutNavigationManager();
        var guarded = 0;
        using var registration = navigation.RegisterLocationChangingHandler(context =>
        {
            guarded++;
            context.PreventNavigation();
            return ValueTask.CompletedTask;
        });
        using var sut = new AutoLogoutService(_settings, _notifications.Object, _instrumentNotifications.Object, _authentication,
            _device.Object, navigation, NullLogger<AutoLogoutService>.Instance);
        var countdowns = 0;
        sut.UserIdle += seconds => { countdowns++; return Task.CompletedTask; };
        await sut.StartAsync();

        await sut.HandleTimeoutAsync();
        var permitted = await navigation.NavigationCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        await sut.StartLogoutCountdownAsync();

        permitted.Should().BeTrue();
        guarded.Should().Be(0);
        navigation.Uri.Should().Contain(Pharos.Contracts.Security.AccountRoutes.Logout);
        countdowns.Should().Be(0);
    }

    [Fact]
    public async Task AnonymousSession_DoesNotReadSettingsWarnOrLogOut()
    {
        _authentication.Authenticated = false;
        using var sut = CreateSut();
        var countdowns = 0;
        sut.UserIdle += seconds => { countdowns++; return Task.CompletedTask; };
        await sut.StartAsync();
        sut.StayLoggedIn();
        await sut.StartLogoutCountdownAsync();
        await sut.HandleTimeoutAsync();
        _data.Verify(x => x.GetAsync(), Times.Never);
        countdowns.Should().Be(0);
        _navigation.LastUri.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticationLost_StopsMonitoring()
    {
        using var sut = CreateSut();
        var countdowns = 0;
        sut.UserIdle += seconds => { countdowns++; return Task.CompletedTask; };
        await sut.StartAsync();
        _authentication.SetAuthenticated(false);
        await sut.StartLogoutCountdownAsync();
        await sut.HandleTimeoutAsync();
        countdowns.Should().Be(0);
        _navigation.LastUri.Should().BeNull();
    }

    [Fact]
    public async Task SettingsChanged_Disabled_StopsBothSessions()
    {
        using var first = CreateSut();
        using var second = CreateSut();
        var countdowns = 0;
        foreach (var client in new[] { first, second })
        {
            client.UserIdle += seconds => { countdowns++; return Task.CompletedTask; };
            await client.StartAsync();
        }
        _enabled = false;
        await NotifyServerSettingsChangedAsync();
        foreach (var client in new[] { first, second })
        {
            client.StayLoggedIn();
            await client.StartLogoutCountdownAsync();
            await client.HandleTimeoutAsync();
        }
        countdowns.Should().Be(0);
        _navigation.LastUri.Should().BeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SettingsChanged_OnlyChangedTimeoutDismissesCountdown(bool changeTimeout)
    {
        using var sut = CreateSut();
        var dismissed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        sut.UserActive += () =>
        {
            dismissed.TrySetResult();
            return Task.CompletedTask;
        };
        await sut.StartAsync();
        await sut.StartLogoutCountdownAsync();
        if (changeTimeout) { _timeoutMinutes = 20; }
        await NotifyServerSettingsChangedAsync();
        if (changeTimeout)
        {
            await dismissed.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        }
        dismissed.Task.IsCompleted.Should().Be(changeTimeout);
        _data.Verify(x => x.GetAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task SettingsChanged_Enabled_ResumesMonitoring()
    {
        _enabled = false;
        using var sut = CreateSut();
        var countdowns = 0;
        sut.UserIdle += seconds => { countdowns++; return Task.CompletedTask; };
        await sut.StartAsync();
        await sut.StartLogoutCountdownAsync();
        countdowns.Should().Be(0);
        _enabled = true;
        await _settings.NotifySettingsChangedAsync();
        await sut.StartLogoutCountdownAsync();
        countdowns.Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Timeout_LogsOutWithBlockedOrFailedUserIdleHandler(bool slow)
    {
        _timeoutMinutes = 1;
        using var sut = CreateSut();
        var prompt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var countdownStarted = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        sut.UserIdle += seconds =>
        {
            countdownStarted.TrySetResult(seconds);
            return slow ? prompt.Task : throw new InvalidOperationException("Countdown failed.");
        };
        try
        {
            await sut.StartAsync();
            // Exercise the handlers directly; this test does not verify wall-clock scheduling.
            var countdown = sut.StartLogoutCountdownAsync();
            var secondsRemaining = await countdownStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            secondsRemaining.Should().Be(30);
            _navigation.LastUri.Should().BeNull();

            if (slow)
            {
                countdown.IsCompleted.Should().BeFalse();
            }
            else
            {
                await countdown;
            }

            await sut.HandleTimeoutAsync();
            await _navigation.Navigated.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            prompt.Task.IsCompleted.Should().BeFalse();
        }
        finally
        {
            prompt.TrySetResult();
        }
    }

    [Fact]
    public async Task StartTwiceAndDispose_DoesNotDuplicateOrRetainSubscriptions()
    {
        var sut = CreateSut();
        await sut.StartAsync();
        await sut.StartAsync();
        sut.Dispose();
        await NotifyServerSettingsChangedAsync();
        RaiseCompartmentNotifications();
        _notifications.VerifyAdd(x => x.AutoLogoutSettingsChanged += It.IsAny<Func<Task>>(), Times.Once);
        _notifications.VerifyRemove(x => x.AutoLogoutSettingsChanged -= It.IsAny<Func<Task>>(), Times.Once);
        _data.Verify(x => x.GetAsync(), Times.Once);
        _navigation.LastUri.Should().BeNull();
    }
}

internal sealed class QueuedSynchronizationContext : SynchronizationContext
{
    private readonly Queue<(SendOrPostCallback Callback, object? State)> _callbacks = new();

    public override void Post(SendOrPostCallback callback, object? state)
    {
        _callbacks.Enqueue((callback, state));
    }

    public void RunPending()
    {
        while (_callbacks.TryDequeue(out var callback))
        {
            callback.Callback(callback.State);
        }
    }
}

internal sealed class TestAuthenticationStateProvider : AuthenticationStateProvider
{
    public bool Authenticated { get; set; } = true;
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(
            new ClaimsPrincipal(Authenticated ? new ClaimsIdentity("Test") : new ClaimsIdentity())));
    }

    public void SetAuthenticated(bool authenticated)
    {
        Authenticated = authenticated;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}

internal sealed class AutoLogoutNavigationManager : NavigationManager
{
    public string? LastUri { get; private set; }
    public NavigationOptions? Options { get; private set; }
    public TaskCompletionSource Navigated { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public AutoLogoutNavigationManager()
    {
        Initialize("http://localhost/", "http://localhost/");
    }

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        NavigateToCore(uri, new NavigationOptions
        {
            ForceLoad = forceLoad
        });
    }

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        LastUri = uri;
        Options = options;
        Navigated.TrySetResult();
    }
}

internal sealed class GuardedAutoLogoutNavigationManager : NavigationManager
{
    public TaskCompletionSource<bool> NavigationCompleted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public GuardedAutoLogoutNavigationManager()
    {
        Initialize("http://localhost/", "http://localhost/");
    }

    protected override void SetNavigationLockState(bool value) { }

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        NavigateToCore(uri, new NavigationOptions
        {
            ForceLoad = forceLoad
        });
    }

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        _ = NavigateAsync(uri, options);
    }

    private async Task NavigateAsync(string uri, NavigationOptions options)
    {
        var target = ToAbsoluteUri(uri).AbsoluteUri;
        // Browser full navigation bypasses Blazor's internal navigation handlers.
        var permitted = options.ForceLoad || await NotifyLocationChangingAsync(target, options.HistoryEntryState, false);
        if (permitted)
        {
            Uri = target;
            NotifyLocationChanged(false);
        }
        NavigationCompleted.TrySetResult(permitted);
    }
}
