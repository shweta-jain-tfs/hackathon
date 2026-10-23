namespace Pharos.Common.Web.Utilities;

/// <summary>
/// Throttles actions so they are not executed more often than the specified interval.
/// </summary>
/// <remarks>
/// Use at the source of bursty updates, such as notifications, polling, or background events.
///
/// Avoid using in Blazor lifecycle-driven refresh paths, where it can delay user interactions
/// or interfere with rendering behavior. Prefer throttling or debouncing at the source of
/// the updates, where the cause of refreshes is known.
/// </remarks>
public sealed class AsyncThrottler : IDisposable
{
    public static readonly TimeSpan DefaultThrottleWindow = TimeSpan.FromSeconds(2);
    private const int _actionCountWarningThreshold = 10;

    private readonly TimeSpan _interval;
    private readonly Func<Exception, Task>? _onError;
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly Lock _gate = new();
    private Func<Task>? _pending;
    private DateTime? _lastEmittedUtc;
    private bool _flushScheduled;
    private bool _disposed;
    private int _actionCount;

    private string SourceName { get; }

    /// <summary>
    /// Throttles actions to ensure that they are not executed more frequently than the specified interval.
    /// </summary>
    /// <param name="interval">The minimum interval between action executions.</param>
    /// <param name="onError">An optional error handler for exceptions thrown by the actions.</param>
    /// <param name="name">Name of throttler to show in log messages.</param>
    public AsyncThrottler(
        TimeSpan interval,
        Func<Exception, Task>? onError = null,
        string? name = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(interval, TimeSpan.Zero);

        _interval = interval;
        _onError = onError;
        SourceName = string.IsNullOrWhiteSpace(name)
            ? nameof(AsyncThrottler)
            : $"{nameof(AsyncThrottler)} [{name}]";
    }

    /// <summary>
    /// Pushes an asynchronous action. This is the preferred overload.
    /// </summary>
    /// <remarks>
    /// The first action executes immediately when allowed. Later actions replace the
    /// pending action and execute after the throttle interval. Await the returned task
    /// to observe exceptions from immediate execution. Exceptions from deferred
    /// execution are sent to the configured error handler.
    /// </remarks>
    public async Task PushAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var flushImmediately = false;
        TimeSpan? delay = null;

        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _pending = action;
            _actionCount++;

            var remaining = _lastEmittedUtc is null
                ? TimeSpan.Zero
                : _interval - (DateTime.UtcNow - _lastEmittedUtc.Value);

            if (remaining <= TimeSpan.Zero)
            {
                flushImmediately = true;
            }
            else if (!_flushScheduled)
            {
                _flushScheduled = true;
                delay = remaining;
            }
        }

        if (flushImmediately)
        {
            await FlushAsync();
        }

        if (delay.HasValue)
        {
            _ = ScheduleFlushAsync(delay.Value);
        }
    }

    /// <summary>
    /// Pushes a synchronous action. Prefer <see cref="PushAsync(Func{Task})"/>.
    /// </summary>
    /// <remarks>
    /// Use this overload from synchronous code. If the action executes immediately,
    /// its exception is rethrown when no error handler is configured. If execution is
    /// deferred, exceptions are passed to the configured error handler.
    /// </remarks>
    public void Push(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        PushAsync(() =>
        {
            action();
            return Task.CompletedTask;
        }).GetAwaiter().GetResult();
    }

    public Task FlushAsync()
    {
        Func<Task>? action;
        int actionCount;

        lock (_gate)
        {
            action = _pending;
            _pending = null;
            _lastEmittedUtc = action is null ? _lastEmittedUtc : DateTime.UtcNow;

            actionCount = _actionCount;
            _actionCount = 0;
        }

        LogThrottleBurst(actionCount);

        return action is null
            ? Task.CompletedTask
            : ExecuteAsync(action);
    }

    private async Task ExecuteAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            if (_onError == null)
            {
                Console.WriteLine($"{SourceName} caught an exception without an error handler: {ex.Message}");
                // Preserve the exception for callers awaiting the initial push.
                throw;
            }
            else
            {
                await _onError.Invoke(ex);
            }
        }
    }

    private async Task ScheduleFlushAsync(TimeSpan delay)
    {
        try
        {
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, _disposeCts.Token);
            }

            await FlushAsync();
        }
        catch (OperationCanceledException)
            when (_disposeCts.IsCancellationRequested)
        {
            // Normal disposal.
        }
        finally
        {
            bool reschedule;

            lock (_gate)
            {
                reschedule = _pending is not null && !_disposeCts.IsCancellationRequested;
                _flushScheduled = reschedule;
            }

            if (reschedule)
            {
                _ = ScheduleFlushAsync(_interval);
            }
        }
    }

    private void LogThrottleBurst(int actionCount)
    {
        if (actionCount < _actionCountWarningThreshold)
        {
            return;
        }

        Console.WriteLine($"!! [THROTTLE] !! {SourceName} suppressed a burst of {actionCount} actions.");
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            _pending = null;
        }

        _disposeCts.Cancel();
        _disposeCts.Dispose();
    }
}
