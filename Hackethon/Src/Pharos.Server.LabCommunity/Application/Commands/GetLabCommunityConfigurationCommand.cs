using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Application.Interfaces.GetConfiguration;
using Pharos.Server.LabCommunity.Domain;

namespace Pharos.Server.LabCommunity.Application.Commands;

internal sealed record GetLabCommunityConfigurationCommand : IRequest;

internal sealed class GetLabCommunityConfigurationCommandHandler(
    ILabCommunityDbContext dbContext,
    ILabCommunityConfigurationService configurationService,
    ILabCommunitySchedulerService schedulerService,
    IOptionsSnapshot<LabCommunitySettings> settingsSnapshot,
    ILogger<GetLabCommunityConfigurationCommandHandler> logger) : IRequestHandler<GetLabCommunityConfigurationCommand>
{
    public async Task Handle(GetLabCommunityConfigurationCommand request, CancellationToken cancellationToken)
    {
        var settings = settingsSnapshot.Value;
        var existingEnrollment = await dbContext.GetActiveEnrollmentAsync(settings.LaboratoryCode, cancellationToken);
        if (existingEnrollment is null)
        {
            logger.LogWarning("Cannot get LabCommunity configuration. No active enrollment found.");

            return;
        }

        var pulledConfiguration = await configurationService.GetConfiguration(cancellationToken);
        if (pulledConfiguration is null)
        {
            return;
        }

        logger.LogInformation("LabCommunity configuration retrieved.");

        var persistedConfiguration = await dbContext.GetConfigurationAsync(settings.LaboratoryCode, cancellationToken);
        var previousConfiguration = CreateConfigurationSnapshot(persistedConfiguration);

        var newConfiguration = await PersistConfiguration(settings.LaboratoryCode, pulledConfiguration, persistedConfiguration, cancellationToken);

        await schedulerService.ReconfigureAfterConfigurationPull(newConfiguration, previousConfiguration, cancellationToken);
    }

    private static Configuration? CreateConfigurationSnapshot(Configuration? configuration)
    {
        if (configuration is null)
        {
            return null;
        }

        return new Configuration
        {
            LaboratoryCode = configuration.LaboratoryCode,
            CheckInIntervalSeconds = configuration.CheckInIntervalSeconds,
            SystemStatusEnabled = configuration.SystemStatusEnabled,
            SystemStatusIntervalSeconds = configuration.SystemStatusIntervalSeconds,
            ReportInstrumentErrorsEnabled = configuration.ReportInstrumentErrorsEnabled,
            ReportCalibrationAndQualityResultsEnabled = configuration.ReportCalibrationAndQualityResultsEnabled
        };
    }

    private async Task<Configuration> PersistConfiguration(
        string laboratoryCode,
        LabCommunityConfiguration configuration,
        Configuration? persistedConfiguration,
        CancellationToken cancellationToken)
    {
        if (persistedConfiguration is null)
        {
            persistedConfiguration = new Configuration { LaboratoryCode = laboratoryCode };
            dbContext.Configurations.Add(persistedConfiguration);
        }

        persistedConfiguration.CheckInIntervalSeconds = (int)configuration.CheckInInterval.TotalSeconds;
        persistedConfiguration.SystemStatusEnabled = configuration.SystemStatusEnabled;
        persistedConfiguration.SystemStatusIntervalSeconds = (int)configuration.SystemStatusInterval.TotalSeconds;
        persistedConfiguration.ReportInstrumentErrorsEnabled = configuration.ReportInstrumentErrorsEnabled;
        persistedConfiguration.ReportCalibrationAndQualityResultsEnabled = configuration.ReportCalibrationAndQualityResultsEnabled;

        await dbContext.SaveChangesAsync(cancellationToken);

        return persistedConfiguration;
    }
}