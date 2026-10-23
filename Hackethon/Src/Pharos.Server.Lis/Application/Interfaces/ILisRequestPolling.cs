namespace Pharos.Server.Lis.Application.Interfaces;

/// <summary>
/// Defines the contract for handling periodic polling for LIS requests.
/// </summary>
internal interface ILisRequestPolling
{
    /// <summary>
    /// Checks if polling is currently active for a specific channel.
    /// </summary>
    /// <param name="lisChannelId">The LIS channel ID to check.</param>
    /// <returns>True if polling is active for the specified channel, otherwise false.</returns>
    bool IsPollingForChannel(int lisChannelId);

    /// <summary>
    /// Starts the polling timer with the specified frequency.
    /// </summary>
    /// <param name="lisChannelId">The LIS channel ID to poll for requests.</param>
    void Start(int lisChannelId);

    /// <summary>
    /// Stops the polling timer for a specific channel.
    /// </summary>
    /// <param name="lisChannelId">The LIS channel ID to stop polling for.</param>
    void Stop(int lisChannelId);

    /// <summary>
    /// Stops all polling timers.
    /// </summary>
    void StopAll();
}