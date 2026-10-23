using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.LogDump;
using Pharos.Server.Infrastructure;
using Pharos.Server.Localization;
using Pharos.Server.LogDump.Application.Commands;
using Pharos.Server.LogDump.Application.Interfaces;
using Pharos.Server.LogDump.Application.Services;
using Quartz;

namespace Pharos.Server.LogDump.Application.Jobs;

[DisallowConcurrentExecution]
internal sealed class CreateLogDumpJob(
    ISender sender,
    ITempFileStorage tempFileStorage,
    LogDumpResultStore logDumpResultStore,
    IDateTime dateTime,
    IPublisher publisher,
    IJobHub jobHub,
    ILogger<CreateLogDumpJob> logger) : IJob
{
    public const string StartedBy = nameof(StartedBy);
    public const string Source = nameof(Source);
    public const string FromDate = nameof(FromDate);
    public const string ToDate = nameof(ToDate);
    public const string PharosFiles = nameof(PharosFiles);
    public const string InstrumentFiles = nameof(InstrumentFiles);

    private const string _logDumpFileName = "LogDump.zip";
    private const string _stagingFolderName = "LogDumpStaging";

    public static readonly JobKey Key = new(JobNames.LogDump);

    public async Task Execute(IJobExecutionContext context)
    {
        using var activity = new Activity($"Job execution: {Key.Name}.");
        activity.Start();

        var jobData = context.MergedJobDataMap;
        var startedBy = jobData.GetGuid(StartedBy);
        var source = (LogDumpRequestSource)jobData.GetInt(Source);

        logger.LogInformation("Starting log dump collection for {Source}.", jobData.GetInt(Source));

        Guid? stagingFolderId = null;

        try
        {
            var fromDate = (DateOnly)jobData[FromDate];
            var toDate = (DateOnly)jobData[ToDate];
            var pharosFiles = (LogFileSelection)jobData.GetInt(PharosFiles);

            stagingFolderId = tempFileStorage.CreateFolder(_stagingFolderName);

            //TODO: Collect instrument logs and other system logs
            await sender.Send(new CollectPharosLogsCommand(pharosFiles, fromDate, toDate, stagingFolderId.Value), context.CancellationToken);

            var fileId = await tempFileStorage.CreateArchiveFromFolderAsync(stagingFolderId.Value, _logDumpFileName, context.CancellationToken);

            SaveLogDumpResult(fileId, source);

            await publisher.Publish(new LogDumpCreatedEvent(fileId, source), context.CancellationToken);
            await NotifyClient(source, startedBy, true, Translations.LogDumpCollectionSucceeded);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Log dump collection was cancelled.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Log dump collection failed.");
            await NotifyClient(source, startedBy, false, Translations.LogDumpCollectionFailed);
        }
        finally
        {
            if (stagingFolderId != null)
            {
                tempFileStorage.DeleteFolder(stagingFolderId.Value);
            }
        }
    }

    private void SaveLogDumpResult(Guid fileId, LogDumpRequestSource source)
    {
        var previousResult = logDumpResultStore.Result;
        if (previousResult != null)
        {
            var previousFilePath = tempFileStorage.GetFilePath(previousResult.FileId);
            if (!string.IsNullOrEmpty(previousFilePath) && File.Exists(previousFilePath))
            {
                tempFileStorage.DeleteFile(previousResult.FileId);
            }
        }

        logDumpResultStore.Result = new LogDumpResult(fileId, _logDumpFileName, dateTime.Now, source);
    }

    private async Task NotifyClient(LogDumpRequestSource source, Guid startedBy, bool success, string message)
    {
        if (source == LogDumpRequestSource.LabCommunity)
        {
            return;
        }

        await jobHub.JobCompleted(JobNames.LogDump, startedBy, success, message);
    }
}
