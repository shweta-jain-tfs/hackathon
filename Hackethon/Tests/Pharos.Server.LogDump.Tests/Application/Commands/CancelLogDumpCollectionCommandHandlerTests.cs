using Pharos.Server.LogDump.Application.Commands;
using Pharos.Server.LogDump.Application.Jobs;
using Quartz;

namespace Pharos.Server.LogDump.Tests.Application.Commands;

public class CancelLogDumpCollectionCommandHandlerTests
{
    private readonly Mock<ISchedulerFactory> _schedulerFactory = new();
    private readonly Mock<IScheduler> _scheduler = new();

    public CancelLogDumpCollectionCommandHandlerTests()
    {
        _schedulerFactory.Setup(x => x.GetScheduler(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_scheduler.Object);
    }

    [Trait("ProductRequirement", "PR_484")]
    [Fact]
    public async Task Handle_WhenNoCollectionIsScheduled_ShouldDoNothing()
    {
        // Arrange
        _scheduler.Setup(x => x.GetJobDetail(CreateLogDumpJob.Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IJobDetail?)null);
        var sut = CreateSut();

        // Act
        await sut.Handle(new CancelLogDumpCollectionCommand(), TestContext.Current.CancellationToken);

        // Assert
        _scheduler.Verify(x => x.Interrupt(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Never);
        _scheduler.Verify(x => x.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_484")]
    [Fact]
    public async Task Handle_WhenRunningJobAcceptsInterrupt_ShouldNotDeleteJob()
    {
        // Arrange
        _scheduler.Setup(x => x.GetJobDetail(CreateLogDumpJob.Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJobDetail());
        _scheduler.Setup(x => x.Interrupt(CreateLogDumpJob.Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var sut = CreateSut();

        // Act
        await sut.Handle(new CancelLogDumpCollectionCommand(), TestContext.Current.CancellationToken);

        // Assert
        _scheduler.Verify(x => x.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Trait("ProductRequirement", "PR_484")]
    [Fact]
    public async Task Handle_WhenJobHasNotStarted_ShouldDeleteJob()
    {
        // Arrange
        _scheduler.Setup(x => x.GetJobDetail(CreateLogDumpJob.Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateJobDetail());
        _scheduler.Setup(x => x.Interrupt(CreateLogDumpJob.Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _scheduler.Setup(x => x.DeleteJob(CreateLogDumpJob.Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var sut = CreateSut();

        // Act
        await sut.Handle(new CancelLogDumpCollectionCommand(), TestContext.Current.CancellationToken);

        // Assert
        _scheduler.Verify(x => x.DeleteJob(CreateLogDumpJob.Key, It.IsAny<CancellationToken>()), Times.Once);
    }

    private CancelLogDumpCollectionCommandHandler CreateSut()
    {
        return new CancelLogDumpCollectionCommandHandler(_schedulerFactory.Object);
    }

    private static IJobDetail CreateJobDetail()
    {
        return JobBuilder.Create<CreateLogDumpJob>()
            .WithIdentity(CreateLogDumpJob.Key)
            .Build();
    }
}