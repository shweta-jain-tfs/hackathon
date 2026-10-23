using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Compression;
using Pharos.Server.LogDump.Application.Commands;

namespace Pharos.Server.LogDump.Tests.Application.Commands;

public class CollectPharosLogsCommandHandlerTests
{
    private readonly Mock<ITempFileStorage> _tempFileStorage = new();
    private readonly Mock<IZipFile> _zipFile = new();
    private readonly Mock<IZipWriter> _zipWriter = new();
    private readonly Mock<ILogFilePathProvider> _logFilePathProvider = new();
    private readonly Guid _folderId = Guid.NewGuid();
    private readonly Guid _archiveFileId = Guid.NewGuid();

    public CollectPharosLogsCommandHandlerTests()
    {
        _tempFileStorage.Setup(x => x.CreateFile("pharos_logs.zip", _folderId))
            .ReturnsAsync(_archiveFileId);
        _zipFile.Setup(x => x.CreateWriter(It.IsAny<Stream>()))
            .Returns(_zipWriter.Object);
        _zipWriter.Setup(x => x.AddAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_WhenPharosFilesAreNotSelected_ShouldNotCreateArchive()
    {
        // Arrange
        var sut = CreateSut();
        var command = CreateCommand(LogFileSelection.None);

        // Act
        await sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _tempFileStorage.Verify(x => x.CreateFile(It.IsAny<string>(), It.IsAny<Guid?>()), Times.Never);
        _zipFile.Verify(x => x.CreateWriter(It.IsAny<Stream>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenConfiguredLogDirectoryDoesNotExist_ShouldNotCreateArchive()
    {
        // Arrange
        var missingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _logFilePathProvider.Setup(x => x.GetLogFilePath())
            .Returns(Path.Combine(missingDirectory, "pharos-log-.txt"));
        var sut = CreateSut();

        // Act
        var action = async () => await sut.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowAsync<DirectoryNotFoundException>();
        _tempFileStorage.Verify(x => x.CreateFile(It.IsAny<string>(), It.IsAny<Guid?>()), Times.Never);
        _zipFile.Verify(x => x.CreateWriter(It.IsAny<Stream>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenFilesAreWithinDateRange_ShouldAddOnlyMatchingFilesToArchive()
    {
        // Arrange
        var sourceDirectory = Directory.CreateTempSubdirectory("pharos-log-dump-tests-");
        var archivePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.zip");
        CreateLogFile(sourceDirectory.FullName, "included.log", new DateTime(2026, 8, 8));
        CreateLogFile(sourceDirectory.FullName, "excluded.log", new DateTime(2026, 8, 11));
        SetupLogDirectory(sourceDirectory.FullName);
        SetupArchivePath(archivePath);
        var sut = CreateSut();

        try
        {
            // Act
            await sut.Handle(CreateCommand(), TestContext.Current.CancellationToken);

            // Assert
            _tempFileStorage.Verify(x => x.CreateFile("pharos_logs.zip", _folderId), Times.Once);
            _zipWriter.Verify(x => x.AddAsync("included.log", It.IsAny<Stream>(), TestContext.Current.CancellationToken), Times.Once);
            _zipWriter.Verify(x => x.AddAsync("excluded.log", It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
            _tempFileStorage.Verify(x => x.DeleteFile(It.IsAny<Guid>()), Times.Never);
        }
        finally
        {
            DeleteDirectory(sourceDirectory);
            DeleteFile(archivePath);
        }
    }

    [Fact]
    public async Task Handle_WhenArchivePathCannotBeResolved_ShouldNotCreateZipWriter()
    {
        // Arrange
        var sourceDirectory = Directory.CreateTempSubdirectory("pharos-log-dump-tests-");
        SetupLogDirectory(sourceDirectory.FullName);
        _tempFileStorage.Setup(x => x.GetFilePath(_archiveFileId)).Returns((string?)null);
        var sut = CreateSut();

        try
        {
            // Act
            var action = async () => await sut.Handle(CreateCommand(), TestContext.Current.CancellationToken);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>();
            _zipFile.Verify(x => x.CreateWriter(It.IsAny<Stream>()), Times.Never);
            _tempFileStorage.Verify(x => x.DeleteFile(_archiveFileId), Times.Once);
        }
        finally
        {
            DeleteDirectory(sourceDirectory);
        }
    }

    [Fact]
    public async Task Handle_WhenAddingLogFileFails_ShouldDeletePartialArchiveAndRethrow()
    {
        // Arrange
        var sourceDirectory = Directory.CreateTempSubdirectory("pharos-log-dump-tests-");
        var archivePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.zip");
        CreateLogFile(sourceDirectory.FullName, "included.log", new DateTime(2026, 8, 8));
        SetupLogDirectory(sourceDirectory.FullName);
        SetupArchivePath(archivePath);
        _zipWriter.Setup(x => x.AddAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Cannot add log file."));
        var sut = CreateSut();

        try
        {
            // Act
            var action = async () => await sut.Handle(CreateCommand(), TestContext.Current.CancellationToken);

            // Assert
            await action.Should().ThrowAsync<IOException>();
            _tempFileStorage.Verify(x => x.DeleteFile(_archiveFileId), Times.Once);
        }
        finally
        {
            DeleteDirectory(sourceDirectory);
            DeleteFile(archivePath);
        }
    }

    [Fact]
    public async Task Handle_WhenCancelled_ShouldDeletePartialArchiveAndRethrow()
    {
        // Arrange
        var sourceDirectory = Directory.CreateTempSubdirectory("pharos-log-dump-tests-");
        var archivePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.zip");
        CreateLogFile(sourceDirectory.FullName, "included.log", new DateTime(2026, 8, 8));
        SetupLogDirectory(sourceDirectory.FullName);
        SetupArchivePath(archivePath);
        _zipWriter.Setup(x => x.AddAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        var sut = CreateSut();

        try
        {
            // Act
            var action = async () => await sut.Handle(CreateCommand(), TestContext.Current.CancellationToken);

            // Assert
            await action.Should().ThrowAsync<OperationCanceledException>();
            _tempFileStorage.Verify(x => x.DeleteFile(_archiveFileId), Times.Once);
        }
        finally
        {
            DeleteDirectory(sourceDirectory);
            DeleteFile(archivePath);
        }
    }

    private CollectPharosLogsForLogDumpCommandHandler CreateSut()
    {
        return new(
        _tempFileStorage.Object,
        _zipFile.Object,
        _logFilePathProvider.Object);
    }

    private CollectPharosLogsCommand CreateCommand(LogFileSelection pharosFiles = LogFileSelection.Default)
    {
        return new(
            pharosFiles,
            new DateOnly(2026, 8, 6),
            new DateOnly(2026, 8, 10),
            _folderId);
    }

    private void SetupLogDirectory(string sourceDirectory)
    {
        _logFilePathProvider.Setup(x => x.GetLogFilePath())
            .Returns(Path.Combine(sourceDirectory, "pharos-log-.txt"));
    }

    private void SetupArchivePath(string archivePath)
    {
        File.Create(archivePath).Dispose();
        _tempFileStorage.Setup(x => x.GetFilePath(_archiveFileId)).Returns(archivePath);
    }

    private static void CreateLogFile(string directory, string fileName, DateTime date)
    {
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, Path.GetFileNameWithoutExtension(fileName));
        File.SetCreationTime(path, date);
        File.SetLastWriteTime(path, date);
    }

    private static void DeleteDirectory(DirectoryInfo directory)
    {
        if (directory.Exists)
        {
            directory.Delete(recursive: true);
        }
    }

    private static void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
