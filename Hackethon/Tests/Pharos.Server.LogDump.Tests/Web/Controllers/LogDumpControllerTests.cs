using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pharos.Server.Application;
using Pharos.Server.Infrastructure;
using Pharos.Server.LogDump.Application.Commands;
using Pharos.Server.LogDump.Application.Jobs;
using Pharos.Server.LogDump.Application.Queries.GetLogDumpCollectionResult;
using Pharos.Server.LogDump.Web.Controllers;
using Quartz;

namespace Pharos.Server.LogDump.Tests.Web.Controllers;

public class LogDumpControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<ISchedulerFactory> _schedulerFactory = new();
    private readonly Mock<IScheduler> _scheduler = new();
    private readonly Mock<ITempFileStorage> _tempFileStorage = new();

    [Trait("ProductRequirement", "PR_484")]
    [Fact]
    public async Task IsLogDumpCollectionInProgress_WhenJobExists_ReturnsTrue()
    {
        var job = JobBuilder.Create<CreateLogDumpJob>()
            .WithIdentity(CreateLogDumpJob.Key)
            .Build();

        _schedulerFactory
            .Setup(x => x.GetScheduler(TestContext.Current.CancellationToken))
            .ReturnsAsync(_scheduler.Object);
        _scheduler
            .Setup(x => x.GetJobDetail(CreateLogDumpJob.Key, TestContext.Current.CancellationToken))
            .ReturnsAsync(job);

        var sut = CreateSut();

        var result = await sut.IsLogDumpCollectionInProgress(TestContext.Current.CancellationToken);

        result.Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_484")]
    [Fact]
    public async Task IsLogDumpCollectionInProgress_WhenJobDoesNotExist_ReturnsFalse()
    {
        _schedulerFactory
            .Setup(x => x.GetScheduler(TestContext.Current.CancellationToken))
            .ReturnsAsync(_scheduler.Object);
        _scheduler
            .Setup(x => x.GetJobDetail(CreateLogDumpJob.Key, TestContext.Current.CancellationToken))
            .ReturnsAsync((IJobDetail?)null);

        var sut = CreateSut();

        var result = await sut.IsLogDumpCollectionInProgress(TestContext.Current.CancellationToken);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetLogDumpCollectionResult_ShouldDispatchResultQuery()
    {
        // Arrange
        var expected = new LogDumpCollectionResultDto { FileId = Guid.NewGuid(), FileName = "LogDump.zip" };
        _sender.Setup(x => x.Send(It.IsAny<GetLogDumpCollectionResultQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var sut = CreateSut();

        // Act
        var result = await sut.GetLogDumpCollectionResult();

        // Assert
        result.Should().BeSameAs(expected);
        _sender.Verify(x => x.Send(It.IsAny<GetLogDumpCollectionResultQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartLogDumpCollection_ShouldDispatchPharosCommandForCurrentUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new LogDumpCollectionRequest(
            LogCollectionDateMode.SingleDate,
            new DateOnly(2026, 8, 10),
            null,
            null,
            LogFileSelection.Default,
            LogFileSelection.None);
        _currentUser.Setup(x => x.UserId).Returns(userId);
        _sender.Setup(x => x.Send(It.IsAny<StartLogDumpCollectionCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();

        // Act
        var result = await sut.StartLogDumpCollection(request, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
        _sender.Verify(x => x.Send(
            It.Is<StartLogDumpCollectionCommand>(command =>
                command.Request == request &&
                command.Source == LogDumpRequestSource.Pharos),
            TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task CancelLogDumpCollection_ShouldDispatchCancelCommand()
    {
        // Arrange
        _sender.Setup(x => x.Send(It.IsAny<CancelLogDumpCollectionCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        // Act
        await sut.CancelLogDumpCollection(TestContext.Current.CancellationToken);

        // Assert
        _sender.Verify(x => x.Send(It.IsAny<CancelLogDumpCollectionCommand>(), TestContext.Current.CancellationToken), Times.Once);
    }

    [Trait("ProductRequirement", "PR_484")]
    [Fact]
    public void ExportLogDump_WhenFileExists_ReturnsPhysicalFileResult()
    {
        var fileId = Guid.NewGuid();
        const string filePath = @"C:\temp\log-dump-file";
        const string fileName = "LogDump.zip";

        _tempFileStorage.Setup(x => x.GetFilePath(fileId)).Returns(filePath);
        _tempFileStorage.Setup(x => x.GetFileName(fileId)).Returns(fileName);

        var sut = CreateSut();

        var result = sut.ExportLogDump(fileId);

        var fileResult = result.Should().BeOfType<PhysicalFileResult>().Subject;
        fileResult.FileName.Should().Be(filePath);
        fileResult.ContentType.Should().Be("application/zip");
        fileResult.FileDownloadName.Should().Be(fileName);
        fileResult.EnableRangeProcessing.Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_484")]
    [Fact]
    public void ExportLogDump_WhenFileDoesNotExist_ReturnsNotFound()
    {
        var fileId = Guid.NewGuid();
        _tempFileStorage.Setup(x => x.GetFilePath(fileId)).Returns((string?)null);

        var sut = CreateSut();

        var result = sut.ExportLogDump(fileId);

        result.Should().BeOfType<NotFoundResult>();
    }

    private LogDumpController CreateSut()
    {
        return new LogDumpController(
            _sender.Object,
            _schedulerFactory.Object,
            _tempFileStorage.Object);
    }
}