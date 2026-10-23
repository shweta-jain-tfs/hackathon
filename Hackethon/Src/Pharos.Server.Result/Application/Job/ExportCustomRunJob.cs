using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Result;
using Pharos.Server.Result.Application.Interfaces;
using Quartz;

namespace Pharos.Server.Result.Application.Job;

[DisallowConcurrentExecution]
internal class ExportCustomRunJob(
    IOptionsSnapshot<ResultSettings> options,
    IResultDbContext dbContext,
    IResultExportService resultExportService,
    ILogger<ExportCustomRunJob> logger) : IJob
{
    // Use const field instead of property to avoid Quartz.NET property injection conflicts
    // Quartz tries to automatically inject JobDataMap values into properties with matching names
    public const string CustomRunId = "CustomRunId";
    public const string InstrumentNumber = "InstrumentNumber";

    public async Task Execute(IJobExecutionContext context)
    {
        var settings = options.Value;
        var folder = settings.Export.CustomRunExportPath;
        if (settings.Export.ExportOnCustomRunEnd && !string.IsNullOrWhiteSpace(folder))
        {
            var customRunId = context.MergedJobDataMap.GetString(CustomRunId);
            var instrumentNumber = context.MergedJobDataMap.GetString(InstrumentNumber);
#pragma warning disable CA1873
            logger.LogInformation("Exporting custom run: Instrument = {Instrument}, CustomRunId = {CustomRunId} to folder {Folder}.", instrumentNumber, customRunId, folder);

            var analyticalRunIds = await dbContext.AnalyticalRuns
                .Where(x => x.CmrRunId == customRunId)
                .Select(x => x.Id)
                .ToListAsync();

            if (analyticalRunIds.Count > 0)
            {
                await resultExportService.SaveResultExportStream(analyticalRunIds, folder, CancellationToken.None, customRunId);
                logger.LogInformation("Successfully exported custom run: Instrument = {Instrument}, CustomRunId = {CustomRunId} to folder {Folder}.", instrumentNumber, customRunId, folder);
            }
        }
    }

    public static readonly JobKey Key = new($"export-custom-run");
}