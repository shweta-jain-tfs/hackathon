namespace MediatR;

public interface INotificationHandlingStrategy
{
    Task HandleNotification(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification,
        CancellationToken cancellationToken);
}