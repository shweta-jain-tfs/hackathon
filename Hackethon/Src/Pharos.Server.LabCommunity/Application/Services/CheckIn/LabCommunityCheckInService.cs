using LabCommunity.API.Client;
using Microsoft.Extensions.Logging;
using Pharos.Server.LabCommunity.Application.Interfaces.CheckIn;

namespace Pharos.Server.LabCommunity.Application.Services.CheckIn;

internal sealed class LabCommunityCheckInService(
    ILabCommunityApiClient client,
    ILogger<LabCommunityCheckInService> logger)
    : ILabCommunityCheckInService
{
    public async Task<bool> CheckIn(CancellationToken cancellationToken = default)
    {
        try
        {
            await client.CheckInAsync(cancellationToken);

            logger.LogInformation("LabCommunity check-in completed.");

            return true;
        }
        catch (ApiException ex)
        {
            logger.LogWarning(
                "LabCommunity check-in call failed with status {StatusCode}: {Error}",
                ex.StatusCode, ex.Response);

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while calling the LabCommunity check-in API.");

            return false;
        }
    }
}
