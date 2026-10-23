using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure;
using Pharos.Server.Parameters.Application.Commands;
using Pharos.Server.Application.SystemErrors;
using Pharos.Server.Localization;
using Quartz;

namespace Pharos.Server.Parameters.Application.Jobs;

/// <summary>
/// This job executes the method update import in the background.
/// </summary>
/// <remarks>
/// This is a one-off Quartz.NET job that is started by the user after uploading
/// a package file.
/// </remarks>
[DisallowConcurrentExecution]
internal class ImportParametersJob(
    ISender sender,
    ITempFileStorage tempFileStorage,
    IJobHub jobHub,
    ILogger<ImportParametersJob> logger) : IJob
{
    public const string StartedBy = nameof(StartedBy);
    public const string UploadedFileId = nameof(UploadedFileId);
    // JobDataMap key for optional instrument number to associate the job with an instrument
    public const string InstrumentNumber = nameof(InstrumentNumber);

    public static readonly JobKey Key = new(JobNames.ParametersImport);

    public async Task Execute(IJobExecutionContext context)
    {
        var jobMap = context.JobDetail.JobDataMap;
        var userId = jobMap.GetGuid(StartedBy);
        var fileId = jobMap.GetGuid(UploadedFileId);
        var instrumentNumber = jobMap.ContainsKey(InstrumentNumber) ? jobMap.GetString(InstrumentNumber) : null;

        var success = false;
        string? fileName = null;

        using var activity = new Activity($"Job execution: {Key.Name}.");
        activity.Start();
        logger.LogInformation("Starting parameters import");

        try
        {
            (success, fileName) = await ExecuteImportAsync(fileId, context.CancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unexpected exception during parameters import.");
        }

        if (!success)
        {
            await ReportImportFailureAsync(fileId, fileName, instrumentNumber, context.CancellationToken);
        }

        var message = success ? Translations.ParameterImport_Success : Translations.ParameterImport_Failure;
        await jobHub.JobCompleted(JobNames.ParametersImport, userId, success, message);
    }

    private async Task<(bool Success, string? FileName)> ExecuteImportAsync(Guid fileId, CancellationToken cancellationToken)
    {
        var filePath = tempFileStorage.GetFilePath(fileId);
        if (string.IsNullOrEmpty(filePath))
        {
            logger.LogError("The file with ID={FileId} does not exist in the temporary file storage.", fileId);
            return (false, null);
        }

        if (!File.Exists(filePath))
        {
            logger.LogError("Specified method update package file does not exists. {PackageFile}", filePath);
            return (false, tempFileStorage.GetFileName(fileId));
        }

        var fileName = tempFileStorage.GetFileName(fileId);
        try
        {
            var success = await sender.Send(new ImportMethodUpdatePackageCommand(filePath, fileName), cancellationToken);
            return (success, fileName);
        }
        finally
        {
            tempFileStorage.DeleteFile(fileId);
        }
    }

    private async Task ReportImportFailureAsync(Guid fileId, string? fileName, string? instrumentNumber, CancellationToken cancellationToken)
    {
        // Send only the file name as parameter; the client translation contains the surrounding text.
        var resolvedFileName = fileName ?? (fileId != Guid.Empty ? tempFileStorage.GetFileName(fileId) : null);
        var parameters = new List<string> { resolvedFileName ?? string.Empty };

        var command = new CreatePharosErrorCommand(
            PharosErrorCodes.ParameterImportFailure,
            ErrorClassification.Error,
            InstrumentNumber: instrumentNumber,
            Parameters: parameters);

        await sender.Send(command, cancellationToken).ConfigureAwait(false);
    }
}
