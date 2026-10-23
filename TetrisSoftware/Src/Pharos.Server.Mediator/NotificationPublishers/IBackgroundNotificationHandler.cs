namespace MediatR.NotificationPublishers;

public interface IBackgroundNotificationHandler
{
    /// <summary>
    /// Puts one or more notification into the background processing queue.
    /// </summary>
    ValueTask EnqueueNotification(INotification notification);

    /// <summary>
    /// Signals that there is a notification that will be enqueued soon via <see cref="EnqueueNotification"/>.
    /// NOTE: this is a part of the testing infrastructure.
    /// </summary>
    void SignalForUpcomingNotification();

    /// <summary>
    /// Call this method to wait until all enqueued notifications have been processed and the service is about
    /// to enter the "endless read" mode.
    /// NOTE: this is a part of the testing infrastructure.
    /// </summary>
    Task WaitForNotificationsAsync(TimeSpan timeout);

    /// <summary>
    /// Resets notification tracking state to prevent leakage between tests.
    /// NOTE: this is part of the testing infrastructure.
    /// </summary>
    void ResetNotificationCount();

    event Action<Exception> Error;
}
