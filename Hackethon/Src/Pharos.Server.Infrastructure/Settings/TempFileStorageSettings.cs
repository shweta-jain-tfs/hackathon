namespace Pharos.Server.Infrastructure.Settings;

internal sealed class TempFileStorageSettings
{
    public const string Section = "UploadedFilesStorage";

    public int MaxFileSize { get; set; } = 20 * 1024 * 1024; // 20 MB

    public int FileLifetimeMinutes { get; set; } = 24 * 60; // 1 day

    public int FileCleanupIntervalMinutes { get; set; } = 60; // 1 hour

    public int MaxFilesInStorage { get; set; } = 100;
}