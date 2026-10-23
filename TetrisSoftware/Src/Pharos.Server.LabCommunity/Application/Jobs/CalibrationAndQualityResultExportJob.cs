using MediatR;
using Pharos.Server.LabCommunity.Application.Commands;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Domain;
using Quartz;

namespace Pharos.Server.LabCommunity.Application.Jobs;

[DisallowConcurrentExecution]
internal sealed class CalibrationAndQualityResultExportJob(ISender sender) : ILabCommunityJob
{
    private static readonly ExportItemType[] _exportItemTypes =
    [
        ExportItemType.CurveControlResult,
        ExportItemType.QualityControlResult,
        ExportItemType.BlankResult
    ];

    public static JobKey Key => new("labcommunity-calibration-and-quality-result-export");

    public static TriggerKey Trigger => new("labcommunity-calibration-and-quality-result-export-recurring-trigger");

    public static bool ShouldReconfigureAfterConfigurationPull(Configuration? newConfiguration, Configuration? previousConfiguration)
    {
        return newConfiguration is not null
               && (previousConfiguration is null
                   || previousConfiguration.ReportCalibrationAndQualityResultsEnabled != newConfiguration.ReportCalibrationAndQualityResultsEnabled);
    }

    public static bool ShouldStop(LabCommunityDefaultConfiguration defaultConfiguration, Configuration? configuration)
    {
        return configuration?.ReportCalibrationAndQualityResultsEnabled != true;
    }

    public static TimeSpan ResolveInterval(LabCommunityDefaultConfiguration defaultConfiguration, Configuration? configuration)
    {
        return defaultConfiguration.CalibrationAndQualityResultExportInterval;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        foreach (var exportItemType in _exportItemTypes)
        {
            await sender.Send(new SendExportItemsToLabCommunityCommand(exportItemType), context.CancellationToken);
        }
    }
}
