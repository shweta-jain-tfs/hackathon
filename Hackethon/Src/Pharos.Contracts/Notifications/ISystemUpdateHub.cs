namespace Pharos.Contracts.Notifications;

/// <summary>
/// Hub contract for system update related notifications.
/// </summary>
public interface ISystemUpdateHub : IHub
{
    /// <summary>
    /// Signals that system updates are available.
    /// </summary>
    /// <param name="updatePackages">List of available update package identifiers / file names.</param>
    Task UpdatesAvailable(List<string> updatePackages);

    /// <summary>
    /// Signals that the system update process has started and is in progress.
    /// </summary>
    Task UpdateInProgress();

    /// <summary>
    /// Signals that the system update process has failed.
    /// </summary>
    Task UpdateFailed();

    /// <summary>
    /// Signals that the update outcome has been acknowledged by the client (e.g. user has seen the update result notification).
    /// </summary>
    Task UpdateOutcomeAcknowledged();
}
