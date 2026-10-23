using System.Text.Json;
using LabCommunity.API.Client;
using Microsoft.Extensions.Logging;
using Pharos.Server.LabCommunity.Application.Interfaces.PharosStatus;

namespace Pharos.Server.LabCommunity.Application.Services.PharosStatus;

internal sealed class LabCommunityPharosStatusService(
    ILabCommunityApiClient client,
    IPharosStatusSnapshotProvider snapshotProvider,
    ILogger<LabCommunityPharosStatusService> logger) : ILabCommunityPharosStatusService
{
    public async Task<bool> SendStatusAsync(CancellationToken cancellationToken = default)
    {
        var request = snapshotProvider.CreateSnapshot();

        logger.LogDebug("LabCommunity Pharos status payload: {Payload}", JsonSerializer.Serialize(request));

        try
        {
            await client.UpdateStatusAsync(request, cancellationToken);

            logger.LogInformation("LabCommunity Pharos status uploaded.");
            return true;
        }
        catch (ApiException ex)
        {
            logger.LogWarning("LabCommunity Pharos status call failed with status {StatusCode}: {Error}", ex.StatusCode, ex.Response);

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while calling the LabCommunity Pharos status API.");

            return false;
        }
    }
}
