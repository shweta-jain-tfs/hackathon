using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.Infrastructure;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Application.Interfaces.CheckIn;

namespace Pharos.Server.LabCommunity.Application.Services;

internal sealed class LabCommunityConnectionStatusService(
    ILabCommunityDbContext dbContext,
    IOptionsSnapshot<LabCommunitySettings> settings,
    IOptions<LabCommunityDefaultConfiguration> defaultConfiguration,
    ILabCommunityCheckInState checkInState,
    IDateTime dateTime) : ILabCommunityConnectionStatusService
{
    public async Task<bool> IsConnected(CancellationToken cancellationToken = default)
    {
        if (!settings.Value.Enable)
        {
            return false;
        }

        var enrollment = await dbContext.GetActiveEnrollmentAsync(settings.Value.LaboratoryCode, cancellationToken);
        if (enrollment is null || !enrollment.ValidCredentials)
        {
            return false;
        }

        var configuration = await dbContext.GetConfigurationAsync(settings.Value.LaboratoryCode, cancellationToken);
        var checkInInterval = configuration is null
            ? defaultConfiguration.Value.CheckInInterval
            : TimeSpan.FromSeconds(configuration.CheckInIntervalSeconds);

        return checkInState.HasRecentSuccessfulCheckIn(dateTime.UtcNow, checkInInterval);
    }
}
