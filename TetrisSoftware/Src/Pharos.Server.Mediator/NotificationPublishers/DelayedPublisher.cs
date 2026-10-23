using System.Collections.Concurrent;

namespace MediatR.NotificationPublishers;

/// <summary>
/// This publisher delays publishing notifications until the scope is disposed.
/// </summary>
/// <remarks>
/// This is needed to decouple notification publishing from the current request pipeline (HTTP or gRPC).
/// </remarks>
internal sealed class DelayedPublisher(IBackgroundNotificationHandler backgroundProcessor) : IPublisher, IDisposable, IAsyncDisposable
{
    private readonly ConcurrentQueue<INotification> _delayedPublishQueue = new();

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (notification == null)
        {
            throw new ArgumentNullException(nameof(notification));
        }

        _delayedPublishQueue.Enqueue(notification);
        backgroundProcessor.SignalForUpcomingNotification();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Enqueues the notifications for background processing. Used in synchronous scopes.
    /// </summary>
    /// <remarks>
    /// In ASP.NET Core request scopes, all resolved scoped services are disposed after the request completes.
    /// </remarks>
    public void Dispose()
    {
        if (!_delayedPublishQueue.IsEmpty)
        {
            _ = Task.Run(async () => await EnqueueNotifications().ConfigureAwait(false));
        }
    }

    /// <summary>
    /// Asynchronously enqueues the notifications for background processing and awaits. Used in asynchronous scopes.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_delayedPublishQueue.IsEmpty)
        {
            await EnqueueNotifications().ConfigureAwait(false);
        }
    }

    private async Task EnqueueNotifications()
    {
        while (_delayedPublishQueue.TryDequeue(out var notification))
        {
            await backgroundProcessor.EnqueueNotification(notification).ConfigureAwait(false);
        }
    }
}
