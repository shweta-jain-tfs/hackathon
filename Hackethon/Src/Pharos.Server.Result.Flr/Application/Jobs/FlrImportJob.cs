using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.SystemErrors;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Localization;
using Pharos.Server.Result.Flr.Application.Commands;
using Quartz;

namespace Pharos.Server.Result.Flr.Application.Jobs;

/// <summary>
/// This job executes an FLR file import operation in the background.
/// </summary>
/// <remarks>
/// This is a one-off Quartz.NET job that is started by the user after uploading an FLR file.
/// </remarks>
[DisallowConcurrentExecution]
[PersistJobDataAfterExecution]
internal class FlrImportJob(
    ISender sender,
    ITempFileStorage tempFileStorage,
    IStreamReaderFactory streamReaderFactory,
    IJobHub jobHub,
    ILogger<FlrImportJob> logger) : IJob
{
    public const string StartedBy = nameof(StartedBy);
    public const string UploadedFileId = nameof(UploadedFileId);

    public static readonly JobKey Key = new(JobNames.FlrImport);

    public async Task Execute(IJobExecutionContext context)
    {
        using var activity = new Activity($"Job execution: {Key.Name}.");
        activity.Start();
        logger.LogInformation("Starting FLR file import");
        var jobMap = context.JobDetail.JobDataMap;
        var userId = jobMap.GetGuid(StartedBy);
        var fileName = "unknown";
        try
        {
            var fileId = jobMap.GetGuid(UploadedFileId);
            var filePath = tempFileStorage.GetFilePath(fileId);
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException($"The file with ID={fileId} does not exist in the temporary file storage.");
            }

            try
            {
                fileName = tempFileStorage.GetFileName(fileId) ?? "unknown";
                logger.LogInformation("Opening FLR file {FileName}", fileName);
                using var fileReader = streamReaderFactory.Create(filePath);

                await sender.Send(new ImportFlrFileCommand(fileName!, fileReader), context.CancellationToken);
                logger.LogInformation("FLR file import successfully completed");
#pragma warning disable CA1863
                await jobHub.JobCompleted(JobNames.FlrImport, userId, true, string.Format(Translations.FlrImportSucceded, fileName));
#pragma warning restore CA1863
            }
            finally
            {
                tempFileStorage.DeleteFile(fileId);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "FLR file import failed");
            await sender.Send(new CreatePharosErrorCommand(PharosErrorCodes.FlrImportFailed, ErrorClassification.Error, Parameters: [fileName, e.Message]));
            await jobHub.JobCompleted(JobNames.FlrImport, userId, false, Translations.FlrImportFailed);
        }
    }
}