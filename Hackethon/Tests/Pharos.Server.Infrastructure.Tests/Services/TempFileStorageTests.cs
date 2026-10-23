using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Pharos.Server.Application.Exceptions;
using Pharos.Server.Infrastructure.Compression;
using Pharos.Server.Infrastructure.Services;
using Pharos.Server.Infrastructure.Settings;
using Quartz;

namespace Pharos.Server.Infrastructure.Tests.Services;

public class TempFileStorageTests : IAsyncLifetime
{
    private readonly Mock<IOptions<TempFileStorageSettings>> _settings = new();
    private readonly Mock<ISchedulerFactory> _schedulerFactory = new();
    private readonly Mock<IScheduler> _scheduler = new();
    private readonly Mock<IDateTime> _dateTime = new();
    private readonly Mock<IZipFile> _zipFile = new();
    private readonly Mock<IZipWriter> _zipWriter = new();
    private readonly Mock<ILogger<TempFileStorage>> _logger = new();

    private readonly TempFileStorage _storage;

    private readonly TempFileStorageSettings _testSettings = new()
    {
        MaxFileSize = 1024, // 1 kB
        FileLifetimeMinutes = 60,
        FileCleanupIntervalMinutes = 60,
        MaxFilesInStorage = 100
    };

    public TempFileStorageTests()
    {
        _settings.Setup(x => x.Value).Returns(_testSettings);
        _schedulerFactory.Setup(x => x.GetScheduler(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_scheduler.Object);
        _dateTime.Setup(x => x.Now).Returns(DateTimeOffset.UtcNow);
        _zipFile.Setup(x => x.CreateWriter(It.IsAny<Stream>()))
            .Returns(_zipWriter.Object);
        _zipWriter.Setup(x => x.AddAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storage = new TempFileStorage(_settings.Object, _schedulerFactory.Object, _dateTime.Object, _zipFile.Object, _logger.Object);
    }

    public async ValueTask InitializeAsync()
    {
        await _storage.StartAsync(TestContext.Current.CancellationToken);
        // Wait for the background service to be fully started
        await _storage.ExecuteTask!.WaitAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _storage.StopAsync(TestContext.Current.CancellationToken);
        _storage.Dispose();
    }

    [Fact]
    public async Task SaveFile_And_GetFileName_And_GetFilePath_Works()
    {
        // Arrange
        var fileName = "test.txt";
        var fileContent = "Hello, world!";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

        // Act
        var fileId = await _storage.SaveFile(fileName, stream);

        // Assert
        fileId.Should().NotBe(Guid.Empty);
        _storage.GetFileName(fileId).Should().Be(fileName);
        var filePath = _storage.GetFilePath(fileId);
        filePath.Should().NotBeNullOrEmpty();
        File.Exists(filePath!).Should().BeTrue();
        File.ReadAllText(filePath!).Should().Be(fileContent);
    }

    [Fact]
    public async Task CreateFile_WhenFolderIdIsNull_ShouldCreateEmptyTrackedFile()
    {
        // Arrange
        const string fileName = "created.txt";

        // Act
        var fileId = await _storage.CreateFile(fileName);

        // Assert
        fileId.Should().NotBe(Guid.Empty);
        _storage.GetFileName(fileId).Should().Be(fileName);
        var filePath = _storage.GetFilePath(fileId);
        filePath.Should().NotBeNullOrEmpty();
        File.Exists(filePath!).Should().BeTrue();
        new FileInfo(filePath!).Length.Should().Be(0);
    }

    [Fact]
    public async Task CreateFile_WhenFolderIdIsProvided_ShouldCreateFileInsideFolder()
    {
        // Arrange
        const string fileName = "folder-file.txt";
        var folderId = _storage.CreateFolder("folder");

        // Act
        var fileId = await _storage.CreateFile(fileName, folderId);

        // Assert
        _storage.GetFileName(fileId).Should().Be(fileName);
        var filePath = _storage.GetFilePath(fileId);
        filePath.Should().NotBeNullOrEmpty();
        File.Exists(filePath!).Should().BeTrue();
        Directory.GetParent(filePath!)!.Name.Should().Be(folderId.ToString("D"));
    }

    [Fact]
    public async Task CreateFolder_ShouldCreateTrackedFolder()
    {
        // Act
        var folderId = _storage.CreateFolder("folder");
        var fileId = await _storage.CreateFile("file.txt", folderId);

        // Assert
        var folderPath = Directory.GetParent(_storage.GetFilePath(fileId)!)!.FullName;
        Directory.Exists(folderPath).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteFolder_ShouldDeleteFolderAndContents()
    {
        // Arrange
        var folderId = _storage.CreateFolder("folder");
        var fileId = await _storage.CreateFile("content.txt", folderId);
        var filePath = _storage.GetFilePath(fileId)!;
        await File.WriteAllTextAsync(filePath, "content", TestContext.Current.CancellationToken);
        var folderPath = Directory.GetParent(filePath)!.FullName;

        // Act
        _storage.DeleteFolder(folderId);

        // Assert
        Directory.Exists(folderPath).Should().BeFalse();
    }

    [Fact]
    public async Task CreateArchiveFromFolderAsync_ShouldCreateArchiveFromTrackedFolderFiles()
    {
        // Arrange
        var folderId = _storage.CreateFolder("folder");
        var firstFileId = await _storage.CreateFile("first.log", folderId);
        var secondFileId = await _storage.CreateFile("second.log", folderId);
        await File.WriteAllTextAsync(_storage.GetFilePath(firstFileId)!, "first", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(_storage.GetFilePath(secondFileId)!, "second", TestContext.Current.CancellationToken);

        // Act
        var archiveId = await _storage.CreateArchiveFromFolderAsync(folderId, "archive.zip", TestContext.Current.CancellationToken);

        // Assert
        archiveId.Should().NotBe(Guid.Empty);
        _storage.GetFileName(archiveId).Should().Be("archive.zip");
        File.Exists(_storage.GetFilePath(archiveId)!).Should().BeTrue();
        _zipFile.Verify(x => x.CreateWriter(It.IsAny<Stream>()), Times.Once);
        _zipWriter.Verify(x => x.AddAsync("first.log", It.IsAny<Stream>(), TestContext.Current.CancellationToken), Times.Once);
        _zipWriter.Verify(x => x.AddAsync("second.log", It.IsAny<Stream>(), TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateArchiveFromFolderAsync_WhenZipWriterFails_ShouldDeletePartialArchiveAndRethrow()
    {
        // Arrange
        var folderId = _storage.CreateFolder("folder");
        var fileId = await _storage.CreateFile("file.log", folderId);
        await File.WriteAllTextAsync(_storage.GetFilePath(fileId)!, "content", TestContext.Current.CancellationToken);
        var tempStorageRoot = Directory.GetParent(Directory.GetParent(_storage.GetFilePath(fileId)!)!.FullName)!.FullName;
        var rootFilesBefore = Directory.GetFiles(tempStorageRoot);
        _zipWriter.Setup(x => x.AddAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("zip failed"));

        // Act
        var act = async () => await _storage.CreateArchiveFromFolderAsync(folderId, "archive.zip", TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<IOException>();
        _storage.GetFilePath(fileId).Should().NotBeNull();
        _storage.GetFileName(fileId).Should().Be("file.log");
        Directory.GetFiles(tempStorageRoot).Should().BeEquivalentTo(rootFilesBefore);
    }

    [Fact]
    public async Task SaveFile_Throws_When_TooLarge()
    {
        // Arrange
        var fileName = "big.txt";
        var bigContent = new byte[_testSettings.MaxFileSize + 1];
        using var stream = new MemoryStream(bigContent);

        // Act & Assert
        // ReSharper disable once AccessToDisposedClosure
        var act = async () => await _storage.SaveFile(fileName, stream);
        await act.Should().ThrowAsync<NotAllowedException>();
    }

    [Fact]
    public void GetFileName_ReturnsNull_IfNotFound()
    {
        var randomId = Guid.NewGuid();
        _storage.GetFileName(randomId).Should().BeNull();
    }

    [Fact]
    public void GetFilePath_ReturnsNull_IfNotFound()
    {
        var randomId = Guid.NewGuid();
        _storage.GetFilePath(randomId).Should().BeNull();
    }

    [Fact]
    public async Task DeleteFile_RemovesFile()
    {
        // Arrange
        var fileName = "delete.txt";
        var fileContent = "delete me";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        var fileId = await _storage.SaveFile(fileName, stream);
        var filePath = _storage.GetFilePath(fileId);
        File.Exists(filePath!).Should().BeTrue();

        // Act
        _storage.DeleteFile(fileId);

        // Assert
        File.Exists(filePath!).Should().BeFalse();
    }

    [Fact]
    public void DeleteFile_DoesNothing_IfNotFound()
    {
        var randomId = Guid.NewGuid();
        // Should not throw
        var act = () => _storage.DeleteFile(randomId);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task SaveFile_Throws_IfNotInitialized()
    {
        // Arrange
        var fileName = "not_init.txt";
        var fileContent = "fail";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        using var storage = new TempFileStorage(_settings.Object, _schedulerFactory.Object, _dateTime.Object, _zipFile.Object, _logger.Object);

        // Do not call StartAsync/ExecuteAsync

        // Act & Assert
        // ReSharper disable once AccessToDisposedClosure
        var act = async () => await storage.SaveFile(fileName, stream);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}