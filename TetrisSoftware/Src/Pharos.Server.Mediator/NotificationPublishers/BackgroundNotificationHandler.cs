using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MediatR.NotificationPublishers;

/// <summary>
/// This service processes notifications in the background using a single queue (based on <see cref="Channel{T}"/>).
/// </summary>
public class BackgroundNotificationHandler : BackgroundService, IBackgroundNotificationHandler
{
    private readonly Channel<INotification> _channel = Channel.CreateUnbounded<INotification>();

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly bool _testMode;
    private int _unprocessedNotificationCount;
    private readonly List<TaskCompletionSource>? _waitHandles;
    private readonly Lock? _lock;

    public BackgroundNotificationHandler(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public BackgroundNotificationHandler(IServiceScopeFactory serviceScopeFactory, bool testMode)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _testMode = testMode;
        if (_testMode)
        {
            _lock = new Lock();
            _waitHandles = [];
        }
    }

    public async ValueTask EnqueueNotification(INotification notification)
    {
        await _channel.Writer.WriteAsync(notification).ConfigureAwait(false);
    }

    public void SignalForUpcomingNotification()
    {
        if (_testMode)
        {
            lock (_lock!)
            {
                _unprocessedNotificationCount++;
            }
        }
    }

    public void ResetNotificationCount()
    {
        if (_testMode)
        {
            lock (_lock!)
            {
                _unprocessedNotificationCount = 0;

                // Wake any waiters so they don't hang — we are in a recovery path.
                if (_waitHandles != null && _waitHandles.Count > 0)
                {
                    foreach (var tcs in _waitHandles)
                    {
                        tcs.TrySetResult();
                    }
                    _waitHandles.Clear();
                }
            }
        }
    }

    public async Task WaitForNotificationsAsync(TimeSpan timeout)
    {
        if (!_testMode)
        {
            throw new InvalidOperationException($"{nameof(WaitForNotificationsAsync)} can only be called in test mode.");
        }

        TaskCompletionSource tcs;
        lock (_lock!)
        {
            if (_unprocessedNotificationCount == 0)
            {
                return;
            }

            tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _waitHandles!.Add(tcs);
        }

        await tcs.Task.WaitAsync(timeout).ConfigureAwait(false);
    }

    public event Action<Exception>? Error;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            var notification = await _channel.Reader.ReadAsync(stoppingToken).ConfigureAwait(false);

            // we want to use an async scope here with await to ensure all new notifications
            // created during publishing are enqueued properly. See IDelayedPublisher.AsyncDispose
            await using (var scope = _serviceScopeFactory.CreateAsyncScope())
            {
                using var activity = new Activity("EnqueueNotification background event");
                activity.Start();
                var logger = scope.ServiceProvider.GetService<ILogger<BackgroundNotificationHandler>>();
                try
                {
                    var publisher = scope.ServiceProvider.GetRequiredService<IImmediatePublisher>();

                    await publisher.Publish(notification, stoppingToken);
                }
                catch (Exception ex)
                {
                    Error?.Invoke(ex);
                    logger?.LogError(ex, "An error occured when processing the notification {Notification}", notification.GetType().Name);
                }
            }

            CheckQueueIsEmpty();
        }
        while (!stoppingToken.IsCancellationRequested);
    }

    private void CheckQueueIsEmpty()
    {
        if (_testMode)
        {
            lock (_lock!)
            {
                if (_unprocessedNotificationCount == 0)
                {
                    return;
                }

                _unprocessedNotificationCount--;
                if (_unprocessedNotificationCount == 0 && _waitHandles!.Count > 0)
                {
                    // notify that the queue is empty
                    foreach (var tcs in _waitHandles!)
                    {
                        tcs.TrySetResult();
                    }
                    _waitHandles.Clear();
                }
            }
        }
    }
}
