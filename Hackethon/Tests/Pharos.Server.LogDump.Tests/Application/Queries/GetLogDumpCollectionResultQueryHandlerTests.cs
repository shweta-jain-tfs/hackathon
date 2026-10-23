using Pharos.Server.Infrastructure;
using Pharos.Server.LogDump.Application.Interfaces;
using Pharos.Server.LogDump.Application.Queries.GetLogDumpCollectionResult;

namespace Pharos.Server.LogDump.Tests.Application.Queries;

public class GetLogDumpCollectionResultQueryHandlerTests : IDisposable
{
    private readonly Mock<ILogDumpResultStore> _resultStore = new();
    private readonly Mock<ITempFileStorage> _tempFileStorage = new();
    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("pharos-log-dump-result-tests-");

    [Fact]
    public async Task Handle_WhenPharosResultFileExists_ShouldReturnDownloadDetails()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var filePath = Path.Combine(_directory.FullName, "LogDump.zip");
        await File.WriteAllTextAsync(filePath, "archive", TestContext.Current.CancellationToken);
        _resultStore.SetupGet(x => x.Result).Returns(new LogDumpResult(fileId, "LogDump.zip", createdAt, LogDumpRequestSource.Pharos));
        _tempFileStorage.Setup(x => x.GetFilePath(fileId)).Returns(filePath);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new GetLogDumpCollectionResultQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.FileId.Should().Be(fileId);
        result.FileName.Should().Be("LogDump.zip");
        result.FileCreatedDateTime.Should().Be(createdAt);
    }

    [Fact]
    public async Task Handle_WhenStoredFileNoLongerExists_ShouldReturnEmptyResult()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        _resultStore.SetupGet(x => x.Result).Returns(new LogDumpResult(fileId, "LogDump.zip", DateTimeOffset.UtcNow, LogDumpRequestSource.Pharos));
        _tempFileStorage.Setup(x => x.GetFilePath(fileId)).Returns(Path.Combine(_directory.FullName, "missing.zip"));
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new GetLogDumpCollectionResultQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.FileId.Should().Be(Guid.Empty);
        result.FileName.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenResultIsNull_ShouldReturnEmptyResult()
    {
        // Arrange
        _resultStore.SetupGet(x => x.Result).Returns((LogDumpResult?)null);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new GetLogDumpCollectionResultQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.FileId.Should().Be(Guid.Empty);
        result.FileName.Should().BeEmpty();
        _tempFileStorage.Verify(x => x.GetFilePath(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenResultSourceIsNotPharos_ShouldReturnEmptyResult()
    {
        // Arrange
        _resultStore.SetupGet(x => x.Result)
            .Returns(new LogDumpResult(Guid.NewGuid(), "LogDump.zip", DateTimeOffset.UtcNow, LogDumpRequestSource.LabCommunity));
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new GetLogDumpCollectionResultQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.FileId.Should().Be(Guid.Empty);
        result.FileName.Should().BeEmpty();
        _tempFileStorage.Verify(x => x.GetFilePath(It.IsAny<Guid>()), Times.Never);
    }

    public void Dispose()
    {
        _directory.Delete(recursive: true);
    }

    private GetLogDumpCollectionResultQueryHandler CreateSut()
    {
        return new(_resultStore.Object, _tempFileStorage.Object);
    }
}
