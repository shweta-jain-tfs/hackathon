using MediatR;
using Pharos.Server.Jobs;
using Quartz;

namespace Pharos.Server.Commands;

internal record SetupAutomaticDeleteJobCommand : IRequest;

internal class SetupAutomaticDeleteJobCommandHandler : IRequestHandler<SetupAutomaticDeleteJobCommand>
{
    private readonly ISchedulerFactory _schedulerFactory;

    public SetupAutomaticDeleteJobCommandHandler(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory;
    }

    public async Task Handle(SetupAutomaticDeleteJobCommand request, CancellationToken cancellationToken)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        // Remove existing delete job if it exists
        await DeleteJob(scheduler, cancellationToken);

        // Create a new delete job
        await CreateDeleteJob(scheduler, cancellationToken);
    }

    private static async Task DeleteJob(IScheduler scheduler, CancellationToken cancellationToken)
    {
        await scheduler.DeleteJob(AutomaticDeleteJob.Key(), cancellationToken);
    }

    private static async Task CreateDeleteJob(IScheduler scheduler, CancellationToken cancellationToken)
    {
        var job = JobBuilder.Create<AutomaticDeleteJob>()
            .WithIdentity(AutomaticDeleteJob.Key())
            .Build();
        var trigger = TriggerBuilder.Create()
            .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(1, 0))
            .ForJob(job)
            .Build();
        await scheduler.ScheduleJob(job, trigger, cancellationToken);
    }
}