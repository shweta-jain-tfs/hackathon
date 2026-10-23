using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Exceptions;
using Pharos.Server.Infrastructure.Compression;
using Pharos.Server.Infrastructure.Settings;
using Quartz;

namespace Pharos.Server.Infrastructure.Services;

internal class TempFileStorage(
    IOptions<TempFileStorageSettings> settings,
    ISchedulerFactory schedulerFactory,
    IDateTime dateTime,
    IZipFile zipFile,
    ILogger<TempFileStorage> logger) : BackgroundService, ITempFileStorage
{
    private record FileEntry(Guid Id, string FileName, DateTimeOffset CreatedAt, Guid? FolderId = null);
    private record FolderEntry(Guid Id, string FolderName, DateTimeOffset CreatedAt);

    private readonly Lock _folderLock = new();
    private DirectoryInfo? _tempFolder;
    private readonly ConcurrentDictionary<Guid, FileEntry> _fileEntries = new();
    private readonly ConcurrentDictionary<Guid, FolderEntry> _folderEntries = new();
    private readonly SemaphoreSlim _schedulerLock = new(1, 1);
    private IScheduler? _scheduler;

    public async Task<Guid> SaveFile(string fileName, Stream content)
    {
        if (_tempFolder == null)
        {
            throw new InvalidOperationException("The temporary file upload folder was not initialized properly.");
        }

        var maxSize = settings.Value.MaxFileSize;
        if (content.Length > maxSize)
        {
            throw new NotAllowedException($"The file exceeds the maximum allowed size of {maxSize / 1024.0 / 1024}");
        }

        var fileId = Guid.NewGuid();
        await using var fileStream = File.Create(GetFilePathInternal(fileId), 1024 * 1024, FileOptions.RandomAccess); // 00000000-0000-0000-0000-000000000000
        await content.CopyToAsync(fileStream);
        _fileEntries[fileId] = new FileEntry(fileId, fileName, dateTime.Now);

        await ScheduleCleanupJob();

        return fileId;
    }

    public async Task<Guid> CreateFile(string fileName, Guid? folderId = null)
    {
        if (_tempFolder == null)
        {
            throw new InvalidOperationException("The temporary file upload folder was not initialized properly.");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));
        }

        if (folderId.HasValue && !_folderEntries.ContainsKey(folderId.Value))
        {
            throw new DirectoryNotFoundException($"Temporary folder '{folderId.Value:D}' was not found.");
        }

        var fileId = Guid.NewGuid();
        var filePath = GetFilePathInternal(fileId, folderId);
        await using (File.Create(filePath))
        {
        }

        _fileEntries[fileId] = new FileEntry(fileId, fileName, dateTime.Now, folderId);

        await ScheduleCleanupJob();

        return fileId;
    }

    public async Task<Guid> CreateArchiveFromFolderAsync(Guid folderId, string archiveName, CancellationToken cancellationToken)
    {
        if (_tempFolder == null)
        {
            throw new InvalidOperationException("The temporary file upload folder was not initialized properly.");
        }

        if (string.IsNullOrWhiteSpace(archiveName))
        {
            throw new ArgumentException("Archive name cannot be empty.", nameof(archiveName));
        }

        if (!_folderEntries.TryGetValue(folderId, out _))
        {
            throw new DirectoryNotFoundException($"Temporary folder '{folderId:D}' was not found.");
        }

        var folder = GetFolderPathInternal(folderId);
        if (!Directory.Exists(folder))
        {
            throw new DirectoryNotFoundException($"Temporary folder '{folderId:D}' was not found.");
        }

        var archiveId = Guid.NewGuid();
        var archivePath = GetFilePathInternal(archiveId);

        try
        {
            await using var archiveStream = File.Create(archivePath, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using var zipWriter = zipFile.CreateWriter(archiveStream);

            foreach (var fileEntry in _fileEntries.Values.Where(x => x.FolderId == folderId))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var filePath = GetFilePathInternal(fileEntry.Id, fileEntry.FolderId);

                await using var source = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete,
                    bufferSize: 1024 * 1024,
                    options: FileOptions.Asynchronous | FileOptions.SequentialScan);

                await zipWriter.AddAsync(fileEntry.FileName, source, cancellationToken);
            }

            _fileEntries[archiveId] = new FileEntry(archiveId, archiveName, dateTime.Now);

            await ScheduleCleanupJob();

            return archiveId;
        }
        catch
        {
            File.Delete(archivePath);
            throw;
        }
    }

    public string? GetFileName(Guid fileId)
    {
        return _fileEntries.TryGetValue(fileId, out var entry) ? entry.FileName : null;
    }

    public string? GetFilePath(Guid fileId)
    {
        if (_fileEntries.TryGetValue(fileId, out var entry))
        {
            return GetFilePathInternal(fileId, entry.FolderId);
        }

        return null;
    }

    public void DeleteFile(Guid fileId)
    {
        if (_fileEntries.TryGetValue(fileId, out var entry))
        {
            try
            {
                lock (_folderLock)
                {
                    File.Delete(GetFilePathInternal(fileId, entry.FolderId));
                }
                _fileEntries.TryRemove(fileId, out _);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to delete the temporary file {FileName}.", entry.FileName);
            }
        }
    }

    public Guid CreateFolder(string folderName)
    {
        if (_tempFolder == null)
        {
            throw new InvalidOperationException("The temporary file upload folder was not initialized properly.");
        }

        if (string.IsNullOrWhiteSpace(folderName))
        {
            throw new ArgumentException("Folder name cannot be empty.", nameof(folderName));
        }

        var folderId = Guid.NewGuid();
        lock (_folderLock)
        {
            Directory.CreateDirectory(GetFolderPathInternal(folderId));
        }

        _folderEntries[folderId] = new FolderEntry(folderId, folderName, dateTime.Now);

        return folderId;
    }

    public void DeleteFolder(Guid folderId)
    {
        if (_folderEntries.TryGetValue(folderId, out var entry))
        {
            try
            {
                lock (_folderLock)
                {
                    Directory.Delete(GetFolderPathInternal(folderId), recursive: true);
                }
                _folderEntries.TryRemove(folderId, out _);
            }
            catch (DirectoryNotFoundException)
            {
                _folderEntries.TryRemove(folderId, out _);
                logger.LogDebug("Temporary folder {FolderName} was already deleted.", entry.FolderName);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to delete the temporary folder {FolderName}.", entry.FolderName);
            }
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _tempFolder = Directory.CreateTempSubdirectory();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create the temporary file upload folder. Uploading files is not possible.");
        }

        if (settings.Value.FileCleanupIntervalMinutes <= 0)
        {
            logger.LogWarning($"The interval for the temp file cleanup job is not specified. The job is not created. Add {TempFileStorageSettings.Section}:{nameof(TempFileStorageSettings.FileCleanupIntervalMinutes)} configuration setting.");
        }

        if (settings.Value.FileLifetimeMinutes <= 0)
        {
            logger.LogWarning($"The lifetime for the temporary files is not specified. Cleanup job is not created. Add {TempFileStorageSettings.Section}:{nameof(TempFileStorageSettings.FileLifetimeMinutes)} configuration setting.");
        }
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        var scheduler = _scheduler;
        if (scheduler is { IsShutdown: false })
        {
            try
            {
                await scheduler.UnscheduleJob(CleanupTempFilesJob.Trigger, cancellationToken);
                // Interrupt the job to avoid file locks when deleting the folder below.
                await scheduler.Interrupt(CleanupTempFilesJob.Key, cancellationToken);
            }
            catch (ObjectDisposedException e)
            {
                logger.LogDebug(e, "Scheduler was already disposed during shutdown.");
            }
            catch (SchedulerException e)
            {
                logger.LogWarning(e, "Failed to unschedule temp file cleanup trigger.");
            }
        }

        try
        {
            lock (_folderLock)
            {
                _tempFolder?.Delete(true);
            }
        }
        catch (DirectoryNotFoundException)
        {
            logger.LogDebug("Temporary file folder was already deleted before StopAsync could remove it.");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to delete the temporary file upload folder.");
        }
        finally
        {
            _tempFolder = null;
            _fileEntries.Clear();
            _folderEntries.Clear();
        }
    }

    private async Task ScheduleCleanupJob()
    {
        // schedule cleanup job first successful upload
        await _schedulerLock.WaitAsync();
        try
        {
            var scheduler = _scheduler ??= await schedulerFactory.GetScheduler(CancellationToken.None);
            var job = await scheduler.GetJobDetail(CleanupTempFilesJob.Key);
            if (job == null)
            {
                var trigger = TriggerBuilder.Create()
                    .WithIdentity(CleanupTempFilesJob.Trigger)
                    .WithSimpleSchedule(b => b.WithIntervalInMinutes(settings.Value.FileCleanupIntervalMinutes))
                    .StartNow()
                    .Build();

                job = JobBuilder.Create<CleanupTempFilesJob>()
                    .WithIdentity(CleanupTempFilesJob.Key)
                    .Build();

                await scheduler.ScheduleJob(job, trigger);
            }
        }
        finally
        {
            _schedulerLock.Release();
        }
    }

    private string GetFilePathInternal(Guid fileId, Guid? folderId)
    {
        if (folderId.HasValue)
        {
            return Path.Combine(GetFolderPathInternal(folderId.Value), fileId.ToString("D"));
        }

        return GetFilePathInternal(fileId);
    }

    private string GetFilePathInternal(Guid fileId)
    {
        return Path.Combine(_tempFolder!.FullName, fileId.ToString("D"));
    }

    private string GetFolderPathInternal(Guid folderId)
    {
        return Path.Combine(_tempFolder!.FullName, folderId.ToString("D"));
    }

    public override void Dispose()
    {
        base.Dispose();
        _schedulerLock.Dispose();
    }

    private class CleanupTempFilesJob(
        ITempFileStorage tempFileStorage,
        IOptions<TempFileStorageSettings> settings,
        IDateTime dateTime) : IJob
    {
        public static readonly JobKey Key = new("cleanup-temp-files");
        public static readonly TriggerKey Trigger = new("cleanup-temp-files-interval");

        public Task Execute(IJobExecutionContext context)
        {
            var fileEntries = ((TempFileStorage)tempFileStorage)._fileEntries;

            var maxLifetime = TimeSpan.FromMinutes(settings.Value.FileLifetimeMinutes);
            var deleteTime = dateTime.Now - maxLifetime;
            foreach (var entry in fileEntries.Values)
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (entry.CreatedAt <= deleteTime)
                {
                    tempFileStorage.DeleteFile(entry.Id);
                }
            }
            return Task.CompletedTask;
        }
    }
}