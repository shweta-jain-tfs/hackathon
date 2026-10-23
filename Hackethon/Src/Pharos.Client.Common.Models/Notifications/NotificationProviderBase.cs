namespace Pharos.Client.Common.Models.Notifications;

/// <summary>
/// An abstract notification provider that implements the observer subscription mechanism.
/// </summary>
public abstract class NotificationProviderBase : INotificationProvider
{
    private readonly Dictionary<object, Delegate> _subscribers = new();

    protected NotificationProviderBase()
    {
    }

    /// <summary>
    /// Returns true if the provider as at least one notification handler.
    /// </summary>
    public bool HasSubscribers => _subscribers.Count > 0;

    /// <summary>
    /// Gets the handler for a specified notification.
    /// </summary>
    /// <typeparam name="TDelegate">The notification handler type.</typeparam>
    /// <param name="notification">The key that identifies a specific notification.</param>
    /// <returns>
    /// The notification handler delegate that can be invoked or null if there are no subscribers for the notification.
    /// </returns>
    public TDelegate? GetHandler<TDelegate>(object notification) where TDelegate : Delegate
    {
        _subscribers.TryGetValue(notification, out var handler);
        return (TDelegate?)handler;
    }

    /// <summary>
    /// Subscribes an observer to a notification delivered by this provider.
    /// </summary>
    /// <param name="notification">The key that identifies a specific notification.</param>
    /// <param name="value">The notification handler.</param>
    public void Subscribe(object notification, Delegate? value)
    {
        OnSubscribing(notification, value);
        if (_subscribers.TryGetValue(notification, out var handler))
        {
            _subscribers[notification] = Delegate.Combine(handler, value);
        }
        else if (value != null)
        {
            _subscribers[notification] = value;
        }
    }

    /// <summary>
    /// Unsubscribes an observer from a notification.
    /// </summary>
    /// <param name="notification">The key that identifies a specific notification.</param>
    /// <param name="value">The notification handler.</param>
    public void Unsubscribe(object notification, Delegate? value)
    {
        if (_subscribers.TryGetValue(notification, out var handler))
        {
            var newHandler = Delegate.Remove(handler, value);
            if (newHandler != null)
            {
                _subscribers[notification] = newHandler;
            }
            else
            {
                _subscribers.Remove(notification);
            }
        }

        OnUnsubscribed(notification, value);
    }

    public event EventHandler<Exception>? Error
    {
        add => Subscribe(nameof(Error), value);
        remove => Unsubscribe(nameof(Error), value);
    }

    protected void OnError(Exception error)
    {
        GetHandler<EventHandler<Exception>>(nameof(Error))?.Invoke(this, error);
    }

    protected virtual void OnSubscribing(object notification, Delegate? value)
    {
    }

    protected virtual void OnUnsubscribed(object notification, Delegate? value)
    {
    }
}