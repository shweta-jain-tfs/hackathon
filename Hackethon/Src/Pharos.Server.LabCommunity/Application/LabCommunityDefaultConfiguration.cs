namespace Pharos.Server.LabCommunity.Application;

internal sealed class LabCommunityDefaultConfiguration
{
    public const string SectionName = "LabCommunityDefaultConfiguration";

    public int CheckInIntervalSeconds { get; set; }

    public TimeSpan CheckInInterval => TimeSpan.FromSeconds(CheckInIntervalSeconds);

    public bool SystemStatusEnabled { get; set; }

    public int SystemStatusIntervalSeconds { get; set; }

    public TimeSpan SystemStatusInterval => TimeSpan.FromSeconds(SystemStatusIntervalSeconds);

    public int ReportInstrumentErrorsIntervalSeconds { get; set; }

    public TimeSpan InstrumentErrorExportInterval => TimeSpan.FromSeconds(ReportInstrumentErrorsIntervalSeconds);

    public int ReportCalibrationAndQualityResultsIntervalSeconds { get; set; }

    public TimeSpan CalibrationAndQualityResultExportInterval => TimeSpan.FromSeconds(ReportCalibrationAndQualityResultsIntervalSeconds);

    public int ExportQueueCleanupIntervalDays { get; set; } = 1;

    public TimeSpan ExportQueueCleanupInterval => TimeSpan.FromDays(ExportQueueCleanupIntervalDays);

    public int DeleteExportItemsOlderThanDays { get; set; } = 90;
}