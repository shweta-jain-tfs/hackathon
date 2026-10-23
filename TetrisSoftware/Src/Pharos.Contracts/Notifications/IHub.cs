namespace Pharos.Contracts.Notifications;

/// <summary>
/// Defines an abstract server-side notification hub. Hubs are, essentially,
/// groups of notifications that require the same permissions to receive them.
/// </summary>
public interface IHub
{
    /// <summary>
    /// Subscribes a client to the hub. This method checks if the client has the
    /// required permissions to listen to the notification from the hub.
    /// </summary>
    /// <returns><see cref="Task"/> object.</returns>
    Task Subscribe();
}
