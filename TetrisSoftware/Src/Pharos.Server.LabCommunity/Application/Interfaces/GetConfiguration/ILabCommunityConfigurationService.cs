namespace Pharos.Server.LabCommunity.Application.Interfaces.GetConfiguration;

internal interface ILabCommunityConfigurationService
{
    Task<LabCommunityConfiguration?> GetConfiguration(CancellationToken cancellationToken = default);
}

internal sealed record LabCommunityConfiguration(
    TimeSpan CheckInInterval,
    bool SystemStatusEnabled,
    TimeSpan SystemStatusInterval,
    bool ReportInstrumentErrorsEnabled,
    bool ReportCalibrationAndQualityResultsEnabled);