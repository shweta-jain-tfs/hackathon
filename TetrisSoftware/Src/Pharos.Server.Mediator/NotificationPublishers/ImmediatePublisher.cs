using System.Collections.Concurrent;
using MediatR.Wrappers;

namespace MediatR.NotificationPublishers;

/// <summary>
/// The notification publisher implementation that processes notifications immediately (within the same scope).
/// </summary>
public class ImmediatePublisher : IImmediatePublisher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationHandlingStrategy _handlingStrategy;
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> _notificationHandlers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmediatePublisher"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider. Can be a scoped or root provider</param>
    public ImmediatePublisher(IServiceProvider serviceProvider)
        : this(serviceProvider, new ForeachAwaitHandlingStrategy()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmediatePublisher"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider. Can be a scoped or root provider</param>
    /// <param name="handlingStrategy">Notification handling strategy. Defaults to <see cref="ForeachAwaitHandlingStrategy"/>.</param>
    public ImmediatePublisher(IServiceProvider serviceProvider, INotificationHandlingStrategy handlingStrategy)
    {
        _serviceProvider = serviceProvider;
        _handlingStrategy = handlingStrategy;
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (notification == null)
        {
            throw new ArgumentNullException(nameof(notification));
        }

        var handler = _notificationHandlers.GetOrAdd(notification.GetType(), static notificationType =>
        {
            var wrapperType = typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(notificationType);
            var wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper for type {notificationType}");
            return (NotificationHandlerWrapper)wrapper;
        });

        return handler.Handle(notification, _serviceProvider, _handlingStrategy.HandleNotification, cancellationToken);
    }
}
