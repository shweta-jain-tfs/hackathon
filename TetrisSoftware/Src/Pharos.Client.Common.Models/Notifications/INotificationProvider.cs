namespace Pharos.Client.Common.Models.Notifications;

/// <summary>
/// Defines a provider for a group of related server-side notifications.
/// The notification providers are registered as singleton services.
/// </summary>
public interface INotificationProvider
{
    /// <summary>
    /// Raised if a server-side error happens.
    /// </summary>
    event EventHandler<Exception> Error;
}