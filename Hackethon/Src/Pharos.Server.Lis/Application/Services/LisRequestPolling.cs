using System.Collections.Concurrent;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Commands;
using Pharos.Server.Lis.Application.Interfaces;

namespace Pharos.Server.Lis.Application.Services;

/// <summary>
/// Handles periodic polling for LIS requests with configurable frequency per channel.
/// </summary>
internal sealed class LisRequestPolling(
    IServiceProvider serviceProvider,
    ILogger<LisRequestPolling> logger,
    IOptionsMonitor<LisSettings> lisSettings) : ILisRequestPolling
{
    private readonly ConcurrentDictionary<int, Timer> _pollTimers = new();

    /// <summary>
    /// Checks if polling is currently active for a specific channel.
    /// </summary>
    /// <param name="lisChannelId">The LIS channel ID to check.</param>
    /// <returns>True if polling is active for the specified channel, otherwise false.</returns>
    public bool IsPollingForChannel(int lisChannelId)
        => _pollTimers.ContainsKey(lisChannelId);

    /// <summary>
    /// Starts the polling timer if conditions are met.
    /// </summary>
    public void Start(int lisChannelId)
    {
        if (IsPollingForChannel(lisChannelId))
        {
            return;
        }

        var importSettings = lisSettings.CurrentValue.LisChannels[lisChannelId].ImportSettings;
        if (importSettings is not { PollForRequests: true, Frequency: > 0 })
        {
            return;
        }

        var intervalMilliseconds = importSettings.IntervalsAreMilliseconds ? importSettings.Frequency : (long)importSettings.Frequency * 60 * 1000;
        var timer = new Timer(_ => PollForRequestsAsync(lisChannelId), null, intervalMilliseconds, intervalMilliseconds);
        _pollTimers[lisChannelId] = timer;

        logger.LogInformation("Started polling for LIS requests on channel {LisChannelId} with frequency of {Frequency} minute(s).", lisChannelId, importSettings.Frequency);
    }

    /// <summary>
    /// Stops all polling timers.
    /// </summary>
    public void StopAll()
    {
        foreach (var keyValue in _pollTimers)
        {
            Stop(keyValue.Key);
        }
    }

    /// <summary>
    /// Stops the polling timer for a specific channel.
    /// </summary>
    public void Stop(int lisChannelId)
    {
        if (_pollTimers.TryRemove(lisChannelId, out var timer))
        {
            timer.Dispose();
            logger.LogInformation("Stopped polling for LIS requests on channel {LisChannelId}.", lisChannelId);
        }
    }

    /// <summary>
    /// Performs a single poll operation, sending a query command
    /// </summary>
    private void PollForRequestsAsync(int lisChannelId)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            sender.Send(new SendAllTestOrderRequestsToLisCommand(lisChannelId)).Wait();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while polling for LIS requests on channel {LisChannelId}.", lisChannelId);
        }
    }
}