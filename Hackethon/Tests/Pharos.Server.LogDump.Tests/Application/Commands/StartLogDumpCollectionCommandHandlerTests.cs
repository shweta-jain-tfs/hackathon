using Pharos.Server.Application;
using Pharos.Server.LogDump.Application.Commands.StartLogDump;
using Pharos.Server.LogDump.Application.Jobs;
using Quartz;

namespace Pharos.Server.LogDump.Tests.Application.Commands;

public class StartLogDumpCollectionCommandHandlerTests
{
    private readonly Mock<ISchedulerFactory> _schedulerFactory = new();
    private readonly Mock<IScheduler> _scheduler = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();

    public StartLogDumpCollectionCommandHandlerTests()
    {
        _schedulerFactory.Setup(x => x.GetScheduler(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_scheduler.Object);
    }

    [Fact]
    public async Task Handle_WhenCollectionIsInProgress_ShouldReturnFalseWithoutSchedulingAnotherJob()
    {
        // Arrange
        _scheduler.Setup(x => x.GetJobDetail(CreateLogDumpJob.Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJobDetail());
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
        _scheduler.Verify(x => x.AddJob(It.IsAny<IJobDetail>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSingleDateIsSelected_ShouldRegisterExpandedDateRangeAndTriggerJob()
    {
        // Arrange
        IJobDetail? registeredJob = null;
        _scheduler.Setup(x => x.GetJobDetail(CreateLogDumpJob.Key, It.IsAny<CancellationToken>())).ReturnsAsync((IJobDetail?)null);
        _scheduler.Setup(x => x.AddJob(It.IsAny<IJobDetail>(), false, true, It.IsAny<CancellationToken>()))
            .Callback<IJobDetail, bool, bool, CancellationToken>((job, _, _, _) => registeredJob = job)
            .Returns(Task.CompletedTask);
        _scheduler.Setup(x => x.TriggerJob(CreateLogDumpJob.Key, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var command = CreateCommand();
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
        registeredJob.Should().NotBeNull();
        registeredJob.JobDataMap.GetInt(CreateLogDumpJob.Source).Should().Be((int)command.Source);
        registeredJob.JobDataMap[CreateLogDumpJob.FromDate].Should().Be(new DateOnly(2026, 8, 6));
        registeredJob.JobDataMap[CreateLogDumpJob.ToDate].Should().Be(new DateOnly(2026, 8, 12));
        registeredJob.JobDataMap.GetInt(CreateLogDumpJob.PharosFiles).Should().Be((int)LogFileSelection.Default);
        registeredJob.JobDataMap.GetInt(CreateLogDumpJob.InstrumentFiles).Should().Be((int)LogFileSelection.None);
        _scheduler.Verify(x => x.TriggerJob(CreateLogDumpJob.Key, TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDateRangeIsSelected_ShouldRegisterRequestedDateRange()
    {
        // Arrange
        IJobDetail? registeredJob = null;
        _scheduler.Setup(x => x.GetJobDetail(CreateLogDumpJob.Key, It.IsAny<CancellationToken>())).ReturnsAsync((IJobDetail?)null);
        _scheduler.Setup(x => x.AddJob(It.IsAny<IJobDetail>(), false, true, It.IsAny<CancellationToken>()))
            .Callback<IJobDetail, bool, bool, CancellationToken>((job, _, _, _) => registeredJob = job)
            .Returns(Task.CompletedTask);
        _scheduler.Setup(x => x.TriggerJob(CreateLogDumpJob.Key, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var command = new StartLogDumpCollectionCommand(
            new LogDumpCollectionRequest(LogCollectionDateMode.DateRange, null, new DateOnly(2026, 8, 6), new DateOnly(2026, 8, 10), LogFileSelection.All, LogFileSelection.Default),
            LogDumpRequestSource.LabCommunity);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
        registeredJob!.JobDataMap[CreateLogDumpJob.FromDate].Should().Be(new DateOnly(2026, 8, 6));
        registeredJob.JobDataMap[CreateLogDumpJob.ToDate].Should().Be(new DateOnly(2026, 8, 10));
    }

    [Fact]
    public async Task Handle_WhenSchedulingFails_ShouldReturnFalse()
    {
        // Arrange
        _scheduler.Setup(x => x.GetJobDetail(CreateLogDumpJob.Key, It.IsAny<CancellationToken>())).ReturnsAsync((IJobDetail?)null);
        _scheduler.Setup(x => x.AddJob(It.IsAny<IJobDetail>(), false, true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SchedulerException("Unable to add job."));
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
        _scheduler.Verify(x => x.TriggerJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private StartLogDumpCollectionCommandHandler CreateSut()
    {
        return new(_schedulerFactory.Object, _currentUserService.Object);
    }

    private static StartLogDumpCollectionCommand CreateCommand()
    {
        return new(
        new LogDumpCollectionRequest(LogCollectionDateMode.SingleDate, new DateOnly(2026, 8, 10), null, null, LogFileSelection.Default, LogFileSelection.None),
        LogDumpRequestSource.Pharos);
    }

    private static IJobDetail CreateJobDetail()
    {
        return JobBuilder.Create<CreateLogDumpJob>().WithIdentity(CreateLogDumpJob.Key).Build();
    }
}
