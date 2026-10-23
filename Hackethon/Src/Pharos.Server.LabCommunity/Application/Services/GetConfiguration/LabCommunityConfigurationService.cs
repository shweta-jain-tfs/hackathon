using LabCommunity.API.Client;
using Microsoft.Extensions.Logging;
using Pharos.Server.LabCommunity.Application.Interfaces.GetConfiguration;

namespace Pharos.Server.LabCommunity.Application.Services.GetConfiguration;

internal sealed class LabCommunityConfigurationService(
    ILabCommunityApiClient client,
    ILogger<LabCommunityConfigurationService> logger)
    : ILabCommunityConfigurationService
{
    public async Task<LabCommunityConfiguration?> GetConfiguration(CancellationToken cancellationToken = default)
    {
        try
        {
            var configuration = await client.GetConfigurationAsync(cancellationToken);

            return new LabCommunityConfiguration(
                TimeSpan.FromSeconds(configuration.CheckIn.IntervalSeconds),
                configuration.SystemStatus.Enabled,
                TimeSpan.FromSeconds(configuration.SystemStatus.IntervalSeconds),
                configuration.ReportInstrumentErrors.Enabled,
                configuration.ReportCalibrationAndControlResults.Enabled);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(
                "LabCommunity configuration call failed with status {StatusCode}: {Error}",
                ex.StatusCode, ex.Response);

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "An error occurred while calling the LabCommunity configuration API.");

            return null;
        }
    }
}