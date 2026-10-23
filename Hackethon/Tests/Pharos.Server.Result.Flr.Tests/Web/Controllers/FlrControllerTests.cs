using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Pharos.Server.Application;
using Pharos.Server.Application.Exceptions;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Flr.Application.Jobs;
using Quartz;

namespace Pharos.Server.Result.Flr.Web.Controllers;

public class FlrControllerTests
{
    private readonly Mock<ITempFileStorage> _tempFileStorage = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<ISchedulerFactory> _schedulerFactory = new();
    private readonly Mock<IScheduler> _scheduler = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _fileId = Guid.NewGuid();

    public FlrControllerTests()
    {
        _schedulerFactory.Setup(x => x.GetScheduler(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_scheduler.Object);
        _currentUser.Setup(x => x.UserId).Returns(_userId);
    }

    [Fact]
    public async Task StartFlrImport_Completes_WhenJobNotInProgress()
    {
        // Arrange
        var fileName = "import.flr";
        var fileContent = new MemoryStream(new byte[] { 1, 2, 3 });
        var formFileMock = new Mock<IFormFile>();
        formFileMock.Setup(f => f.FileName).Returns(fileName);
        formFileMock.Setup(f => f.OpenReadStream()).Returns(fileContent);

        // Simulate no job in progress
        _scheduler.Setup(x => x.GetJobDetail(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IJobDetail?)null);

        _tempFileStorage.Setup(x => x.SaveFile(fileName, fileContent)).ReturnsAsync(_fileId);

        IJobDetail? createdJob = null;
        _scheduler.Setup(x => x.AddJob(It.IsAny<IJobDetail>(), true, true, It.IsAny<CancellationToken>()))
            .Callback<IJobDetail, bool, bool, CancellationToken>((job, _, _, _) => createdJob = job)
            .Returns(Task.CompletedTask);
        _scheduler.Setup(x => x.TriggerJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new FlrController(_tempFileStorage.Object, _currentUser.Object, _schedulerFactory.Object);

        // Act
        var act = async () => await controller.StartFlrImport(formFileMock.Object);

        // Assert
        await act.Should().NotThrowAsync();
        _tempFileStorage.Verify(x => x.SaveFile(fileName, fileContent), Times.Once);
        _scheduler.Verify(x => x.AddJob(It.IsAny<IJobDetail>(), true, true, It.IsAny<CancellationToken>()), Times.Once);
        _scheduler.Verify(x => x.TriggerJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify job parameters
        createdJob.Should().NotBeNull();
        createdJob!.Key.Should().Be(FlrImportJob.Key);
        createdJob.JobDataMap.GetGuid(FlrImportJob.StartedBy).Should().Be(_userId);
        createdJob.JobDataMap.GetGuid(FlrImportJob.UploadedFileId).Should().Be(_fileId);
    }

    [Fact]
    public async Task StartFlrImport_ThrowsNotAllowedException_IfJobInProgress()
    {
        // Arrange
        var formFileMock = new Mock<IFormFile>();
        // Simulate job in progress
        var jobDetailMock = new Mock<IJobDetail>();
        _scheduler.Setup(x => x.GetJobDetail(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobDetailMock.Object);

        var controller = new FlrController(_tempFileStorage.Object, _currentUser.Object, _schedulerFactory.Object);

        // Act
        var act = async () => await controller.StartFlrImport(formFileMock.Object);

        // Assert
        await act.Should().ThrowAsync<NotAllowedException>();
        _tempFileStorage.Verify(x => x.SaveFile(It.IsAny<string>(), It.IsAny<Stream>()), Times.Never);
        _scheduler.Verify(x => x.AddJob(It.IsAny<IJobDetail>(), true, true, It.IsAny<CancellationToken>()), Times.Never);
        _scheduler.Verify(x => x.TriggerJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartFlrImport_DeletesFile_IfJobCreationFails()
    {
        // Arrange
        var fileName = "import.flr";
        var fileContent = new MemoryStream(new byte[] { 1, 2, 3 });
        var formFileMock = new Mock<IFormFile>();
        formFileMock.Setup(f => f.FileName).Returns(fileName);
        formFileMock.Setup(f => f.OpenReadStream()).Returns(fileContent);

        // Simulate no job in progress
        _scheduler.Setup(x => x.GetJobDetail(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IJobDetail?)null);

        _tempFileStorage.Setup(x => x.SaveFile(fileName, fileContent)).ReturnsAsync(_fileId);
        _scheduler.Setup(x => x.AddJob(It.IsAny<IJobDetail>(), true, true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidDataException("fail"));

        var controller = new FlrController(_tempFileStorage.Object, _currentUser.Object, _schedulerFactory.Object);

        // Act
        var act = async () => await controller.StartFlrImport(formFileMock.Object);

        // Assert
        await act.Should().ThrowAsync<Exception>();
        _tempFileStorage.Verify(x => x.DeleteFile(_fileId), Times.Once);
        _scheduler.Verify(x => x.AddJob(It.IsAny<IJobDetail>(), true, true, It.IsAny<CancellationToken>()), Times.Once);
        _scheduler.Verify(x => x.TriggerJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}