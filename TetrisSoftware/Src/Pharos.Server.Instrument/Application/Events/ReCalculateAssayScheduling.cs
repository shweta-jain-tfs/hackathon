using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Jobs;
using Pharos.Server.Instrument.Domain;
using Quartz;
using Quartz.Impl.Matchers;

namespace Pharos.Server.Instrument.Application.Events;

internal record ReCalculateAssayScheduling : INotification;

internal class ReCalculateAssaySchedulingHandler : INotificationHandler<ReCalculateAssayScheduling>
{
    private readonly IInstrumentDbContext _dbContext;
    private readonly ISchedulerFactory _schedulerFactory;

    public ReCalculateAssaySchedulingHandler(IInstrumentDbContext dbContext, ISchedulerFactory schedulerFactory)
    {
        _dbContext = dbContext;
        _schedulerFactory = schedulerFactory;
    }

    public async Task Handle(ReCalculateAssayScheduling notification, CancellationToken cancellationToken)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        // Delete all existing jobs related to assay scheduling
        await DeleteAssaySchedulingJobs(scheduler, cancellationToken);

        // Fetch all assay schedulers from the database
        var assaySchedulers = await _dbContext.AssaySchedulers
            .Include(x => x.SchedulerDays)
            .ThenInclude(x => x.ScheduledMethods)
            .Include(x => x.Instrument)
            .ToListAsync(cancellationToken);

        // Auto-heal: clear expired PausedUntilDate so DB stays consistent even if Quartz jobs were lost on restart.
        var now = DateTimeOffset.Now;
        foreach (var assayScheduler in assaySchedulers)
        {
            if (!assayScheduler.IsActive && assayScheduler.PausedUntilDate.HasValue && assayScheduler.PausedUntilDate.Value <= now)
            {
                assayScheduler.IsActive = true;
                assayScheduler.PausedUntilDate = null;
            }
        }

        if (_dbContext.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Recalculate and reschedule jobs
        foreach (var assayScheduler in assaySchedulers)
        {
            if (assayScheduler.IsActive)
            {
                //set up schedule for each day
                await SetupJobForDays(assayScheduler.SchedulerDays, assayScheduler.Instrument.SerialNumber, assayScheduler.InstrumentId, scheduler, cancellationToken);
            }

            // If the scheduler is paused, we want to set up a job to unpause it later
            if (assayScheduler is { IsActive: false, PausedUntilDate: not null })
            {
                await CreateUnPauseJob(assayScheduler.Instrument.SerialNumber, assayScheduler.InstrumentId, assayScheduler.PausedUntilDate.Value, scheduler, cancellationToken);
            }
        }
    }

    private static async Task SetupJobForDays(ICollection<AssaySchedulerDay> assaySchedulerDays, string instrumentNumber, int instrumentId, IScheduler scheduler, CancellationToken cancellationToken)
    {
        foreach (var assaySchedulerDay in assaySchedulerDays)
        {
            if (assaySchedulerDay.StartTime.HasValue)
            {
                await CreateStartJob(instrumentNumber, instrumentId, assaySchedulerDay.Id, assaySchedulerDay.StartTime.Value, assaySchedulerDay.Day, scheduler, cancellationToken);
            }

            if (assaySchedulerDay.EndTime.HasValue)
            {
                await CreateEndJob(instrumentNumber, assaySchedulerDay.EndTime.Value, assaySchedulerDay.Day, scheduler, cancellationToken);
            }
        }
    }

    private static async Task CreateStartJob(string instrumentNumber, int instrumentId, int schedulerDayId, TimeOnly time, DayOfWeek day, IScheduler scheduler, CancellationToken cancellationToken)
    {
        var job = JobBuilder.Create<StartAssayJob>()
            .WithIdentity(StartAssayJob.Key(instrumentNumber, day))
            .UsingJobData(StartAssayJob.InstrumentId, instrumentId)
            .UsingJobData(StartAssayJob.InstrumentNumber, instrumentNumber)
            .UsingJobData(StartAssayJob.SchedulerDayId, schedulerDayId)
            .Build();
        var trigger = TriggerBuilder.Create()
            .WithSchedule(CronScheduleBuilder.WeeklyOnDayAndHourAndMinute(day, time.Hour, time.Minute))
            .ForJob(job)
            .Build();
        await scheduler.ScheduleJob(job, trigger, cancellationToken);
    }

    private static async Task CreateEndJob(string instrumentNumber, TimeOnly time, DayOfWeek day, IScheduler scheduler, CancellationToken cancellationToken)
    {
        var job = JobBuilder.Create<EndAssayJob>()
            .WithIdentity(EndAssayJob.Key(instrumentNumber, day))
            .UsingJobData(EndAssayJob.InstrumentNumber, instrumentNumber)
            .Build();
        var trigger = TriggerBuilder.Create()
            .WithSchedule(CronScheduleBuilder.WeeklyOnDayAndHourAndMinute(day, time.Hour, time.Minute))
            .ForJob(job)
            .Build();
        await scheduler.ScheduleJob(job, trigger, cancellationToken);
    }

    private static async Task CreateUnPauseJob(string instrumentNumber, int instrumentId, DateTimeOffset pauseUntil, IScheduler scheduler, CancellationToken cancellationToken)
    {
        var job = JobBuilder.Create<UnPauseInstrumentSchedulingJob>()
            .WithIdentity(UnPauseInstrumentSchedulingJob.Key(instrumentNumber))
            .UsingJobData(UnPauseInstrumentSchedulingJob.InstrumentId, instrumentId)
            .Build();
        var trigger = TriggerBuilder.Create()
            .StartAt(pauseUntil)
            .ForJob(job)
            .Build();
        await scheduler.ScheduleJob(job, trigger, cancellationToken);
    }

    private static async Task DeleteAssaySchedulingJobs(IScheduler scheduler, CancellationToken cancellationToken)
    {
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(AssaySchedulingGroup.Key), cancellationToken);
        await scheduler.DeleteJobs(jobKeys, cancellationToken);
    }
}

internal static class AssaySchedulingGroup
{
    public static string Key = "assay-scheduling";
}