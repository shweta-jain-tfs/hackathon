using Pharos.Client.Common.Models.Notifications;

namespace Pharos.Client.App.SystemUpdate.Notifications;

/// <summary>
/// Notification provider for system updates discovery.
/// </summary>
public interface ISystemUpdateNotificationProvider : INotificationProvider
{
    /// <summary>
    /// Raised when one or more updates become available.
    /// Provides the list of update package identifiers.
    /// </summary>
    event EventHandler<List<string>> UpdatesAvailable;

    /// <summary>
    /// Raised when the update process has been initiated and is in progress.
    /// </summary>
    event EventHandler UpdateInProgress;

    /// <summary>
    /// Raised when the system update process has failed.
    /// Useful to get an immediate signal in case the update process fails before the installation process triggers application restart.
    /// </summary>
    event EventHandler UpdateFailed;

    /// <summary>
    /// Raised when the outcome of the update is acknowledged.
    /// </summary>
    event EventHandler UpdateOutcomeAcknowledged;
}
