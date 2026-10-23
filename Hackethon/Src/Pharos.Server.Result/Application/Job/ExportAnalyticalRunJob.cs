using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Result;
using Pharos.Server.Result.Application.Interfaces;
using Quartz;

namespace Pharos.Server.Result.Application.Job;

[DisallowConcurrentExecution]
internal class ExportAnalyticalRunJob(IOptionsSnapshot<ResultSettings> options, IResultExportService resultExportService, ILogger<ExportAnalyticalRunJob> logger) : IJob
{
    // Use const field instead of property to avoid Quartz.NET property injection conflicts
    // Quartz tries to automatically inject JobDataMap values into properties with matching names
    public const string AnalyticalRunId = "AnalyticalRunId";
    public const string AnalyticalRunDbId = "AnalyticalRunDbId";
    public const string InstrumentNumber = "InstrumentNumber";
    public const string MethodName = "MethodName";

    public async Task Execute(IJobExecutionContext context)
    {
        var settings = options.Value;
        var folder = settings.Export.AnalyticalRunExportPath;
        if (settings.Export.ExportOnAnalyticalRunEnd && !string.IsNullOrWhiteSpace(folder))
        {
            var analyticalRunId = context.MergedJobDataMap.GetString(AnalyticalRunId);
            var instrumentNumber = context.MergedJobDataMap.GetString(InstrumentNumber);
            var methodName = context.MergedJobDataMap.GetString(MethodName);
#pragma warning disable CA1873
            logger.LogInformation("Exporting analytical run: Instrument = {Instrument}, MethodName = '{MethodName}', AnalyticalRunId = {AnalyticalRunId} to folder {Folder}.",
                instrumentNumber, methodName, analyticalRunId, folder);

            var analyticalRunDbId = context.MergedJobDataMap.GetIntValue(AnalyticalRunDbId);
            await resultExportService.SaveResultExportStream([analyticalRunDbId], folder, CancellationToken.None);
            logger.LogInformation("Successfully exported analytical run: Instrument = {Instrument}, MethodName = '{MethodName}', AnalyticalRunId = {AnalyticalRunId} to folder {Folder}.",
                instrumentNumber, methodName, analyticalRunId, folder);
        }
    }

    public static readonly JobKey Key = new($"export-analytical-run");
}