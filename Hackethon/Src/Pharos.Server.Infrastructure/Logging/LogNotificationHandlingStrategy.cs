using MediatR;
using Microsoft.Extensions.Logging;

namespace Pharos.Server.Infrastructure.Logging;
/// <summary>
/// Act the same as ForeachAwaitPublisher, but adds logging for each notification handler execution.
/// </summary>
public class LogNotificationHandlingStrategy(ILogger<MediatRLogger> logger) : INotificationHandlingStrategy
{
    public async Task HandleNotification(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken)
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["@Notification"] = notification
        }))
        {
            logger.LogDebug("Handling {NotificationName}.", notification.GetType().FullName);
        }
        foreach (var handler in handlerExecutors)
        {
            var handlerType = handler.HandlerInstance.GetType();
            var handlerName = handlerType.Name;
            var module = handlerType.Assembly.GetName().Name?.Replace("Pharos.Server.", string.Empty);
            logger.LogDebug("Started {Handler} MediatR notification handler in {Module} module.", handlerName, module);

            // execute the notification handler
            await handler.HandlerCallback(notification, cancellationToken).ConfigureAwait(false);

            logger.LogDebug("Ended {Handler} MediatR notification handler in {Module} module.", handlerName, module);
        }
    }
}