using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Request.Application.Commands.RequestGenerator;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace Pharos.Server.Request.Application.Jobs;

public class GenerateRequestsJobTests : IAsyncLifetime
{
    private static readonly Guid _startedByUserId = Guid.NewGuid();
    private readonly Mock<ISender> _sender = new();
    private readonly Mock<IJobHub> _jobHub = new();
    private readonly Mock<ILogger<GenerateRequestsJob>> _logger = new();
    private readonly TaskCompletionSource _jobCompleted = new();
    private readonly StdSchedulerFactory _schedulerFactory;
    private readonly IScheduler _scheduler;

    public GenerateRequestsJobTests()
    {
        _schedulerFactory = new StdSchedulerFactory();
        _scheduler = _schedulerFactory.GetScheduler().GetAwaiter().GetResult();
        _scheduler.JobFactory = new TestJobFactory(_sender.Object, _jobHub.Object, _logger.Object);

        _jobHub.Setup(x => x.JobCompleted(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<string>()))
            .Callback(() => _jobCompleted.TrySetResult())
            .Returns(Task.CompletedTask);
    }

    public async ValueTask InitializeAsync()
    {
        await _scheduler.Start();
    }

    public async ValueTask DisposeAsync()
    {
        await _scheduler.Shutdown(waitForJobsToComplete: true);
    }

    [Trait("ModuleRequirement", "MR_5835")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public async Task GenerateRequestsJob_WhenGenerationSucceeds_NotifiesJobHub()
    {
        var request = CreateRequest();

        _sender.Setup(x => x.Send(It.IsAny<GenerateRequestsCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _schedulerFactory.CreateRequestGenerationJob(_startedByUserId, request);

        var inProgress = await _schedulerFactory.IsRequestGenerationJobInProgress();

        await _schedulerFactory.TriggerRequestGenerationJob();
        await _jobCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        inProgress.Should().BeTrue();
        _sender.Verify(
            x => x.Send(
                It.Is<GenerateRequestsCommand>(command => command.GenerateRequestsDto == request),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _jobHub.Verify(
            x => x.JobCompleted(JobNames.RequestGeneration, _startedByUserId, true, It.Is<string>(message => message.Contains("completed successfully"))),
            Times.Once);
    }

    [Trait("ModuleRequirement", "MR_5835")]
    [Trait("ProductRequirement", "PR_1721")]
    [Fact]
    public async Task GenerateRequestsJob_WhenGenerationFails_NotifiesJobHub()
    {
        _sender.Setup(x => x.Send(It.IsAny<GenerateRequestsCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Generation failed"));

        await _schedulerFactory.CreateRequestGenerationJob(_startedByUserId, CreateRequest());
        await _schedulerFactory.TriggerRequestGenerationJob();
        await _jobCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        _jobHub.Verify(
            x => x.JobCompleted(JobNames.RequestGeneration, _startedByUserId, false, It.Is<string>(message => message.Contains("failed"))),
            Times.Once);
    }

    private static GenerateRequestsDto CreateRequest() => new()
    {
        SampleIdHeadingCharacters = "REQ",
        SampleIdNumberOfDigits = 4,
        SampleIdStartOffset = 1,
        AmountToGenerate = 2,
        PreDilutionFactor = 1,
        Tests =
        [
            new TestDetails
            {
                MethodName = "MethodA",
                TestName = "TestA",
                Replicates = 1,
                InstrumentDilutionFactor = 1
            }
        ]
    };

    private sealed class TestJobFactory(
        ISender sender,
        IJobHub jobHub,
        ILogger<GenerateRequestsJob> logger) : IJobFactory
    {
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            if (bundle.JobDetail.JobType == typeof(GenerateRequestsJob))
            {
                return new GenerateRequestsJob(sender, jobHub, logger);
            }

            throw new NotSupportedException();
        }

        public void ReturnJob(IJob job)
        {
        }
    }
}
