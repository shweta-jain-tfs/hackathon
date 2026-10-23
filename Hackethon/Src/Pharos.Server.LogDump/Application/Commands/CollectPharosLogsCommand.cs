using MediatR;
using Pharos.Server.Application.LogDump;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Compression;

namespace Pharos.Server.LogDump.Application.Commands;

internal sealed record CollectPharosLogsCommand(LogFileSelection PharosFiles, DateOnly FromDate, DateOnly ToDate, Guid FolderId) : IRequest;

internal sealed class CollectPharosLogsForLogDumpCommandHandler(
    ITempFileStorage tempFileStorage,
    IZipFile zipFile,
    ILogFilePathProvider logFilePathProvider)
    : IRequestHandler<CollectPharosLogsCommand>
{
    private const string _pharosLogsArchiveName = "pharos_logs.zip";

    public async Task Handle(CollectPharosLogsCommand command, CancellationToken cancellationToken)
    {
        if (command.PharosFiles is LogFileSelection.None)
        {
            return;
        }

        var configuredLogFilePath = logFilePathProvider.GetLogFilePath();
        var logDirectory = Path.GetDirectoryName(configuredLogFilePath);

        if (string.IsNullOrWhiteSpace(logDirectory) || !Directory.Exists(logDirectory))
        {
            throw new DirectoryNotFoundException($"The Pharos logs directory '{logDirectory}' does not exist.");
        }

        await CreatePharosLogsArchive(command.FolderId, logDirectory, command.FromDate, command.ToDate, cancellationToken);
    }

    private async Task CreatePharosLogsArchive(Guid folderId, string logDirectory, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        var fileId = await tempFileStorage.CreateFile(_pharosLogsArchiveName, folderId);

        try
        {
            var filePath = tempFileStorage.GetFilePath(fileId);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new InvalidOperationException($"The file with ID={fileId} does not exist in the temporary file storage.");
            }

            await using var archiveStream = new FileStream(
               filePath,
               FileMode.Open,
               FileAccess.Write,
               FileShare.None,
               bufferSize: 1024 * 1024,
               options: FileOptions.Asynchronous | FileOptions.SequentialScan);

            await using var zip = zipFile.CreateWriter(archiveStream);

            foreach (var logFilePath in Directory.EnumerateFiles(logDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!IsFileInDateRange(logFilePath, fromDate, toDate))
                {
                    continue;
                }

                await using var logFileStream = new FileStream(
                    logFilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete,
                    bufferSize: 1024 * 1024,
                    options: FileOptions.Asynchronous | FileOptions.SequentialScan);

                await zip.AddAsync(Path.GetFileName(logFilePath), logFileStream, cancellationToken);
            }
        }
        catch
        {
            tempFileStorage.DeleteFile(fileId);
            throw;
        }
    }

    private static bool IsFileInDateRange(string filePath, DateOnly fromDate, DateOnly toDate)
    {
        var logFile = new FileInfo(filePath);

        return IsDateInRange(DateOnly.FromDateTime(logFile.CreationTime), fromDate, toDate) ||
            IsDateInRange(DateOnly.FromDateTime(logFile.LastWriteTime), fromDate, toDate);
    }

    private static bool IsDateInRange(DateOnly date, DateOnly fromDate, DateOnly toDate)
    {
        return date >= fromDate && date <= toDate;
    }
}
