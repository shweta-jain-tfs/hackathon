using System.Text.Json;
using LabCommunity.API.Client;
using Microsoft.Extensions.Logging;
using Pharos.Server.LabCommunity.Application.Interfaces.InstrumentStatus;

namespace Pharos.Server.LabCommunity.Application.Services.InstrumentStatus;

internal sealed class LabCommunityInstrumentStatusService(
    ILabCommunityApiClient client,
    IInstrumentStatusSnapshotProvider snapshotProvider,
    ILogger<LabCommunityInstrumentStatusService> logger) : ILabCommunityInstrumentStatusService
{
    public async Task<bool> SendStatusAsync(CancellationToken cancellationToken = default)
    {
        var request = snapshotProvider.CreateSnapshot();
        if (request.Instruments.Count == 0)
        {
            logger.LogDebug("LabCommunity instrument status call skipped because there are no instruments to send.");
            return true;
        }

        logger.LogDebug("LabCommunity instrument status payload: {Payload}", JsonSerializer.Serialize(request));

        try
        {
            await client.UpsertInstrumentStatusesAsync(request, cancellationToken);

            logger.LogInformation("LabCommunity instrument status uploaded.");
            return true;
        }
        catch (ApiException ex)
        {
            logger.LogWarning("LabCommunity instrument status call failed with status {StatusCode}: {Error}", ex.StatusCode, ex.Response);

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while calling the LabCommunity instrument status API.");

            return false;
        }
    }
}
