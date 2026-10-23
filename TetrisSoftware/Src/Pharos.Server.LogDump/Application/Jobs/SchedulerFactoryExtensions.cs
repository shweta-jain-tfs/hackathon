using Pharos.Server.Application.LogDump;
using Quartz;

namespace Pharos.Server.LogDump.Application.Jobs;

internal static class SchedulerFactoryExtensions
{
    public static async Task<bool> IsLogDumpJobInProgress(this ISchedulerFactory schedulerFactory, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        return await scheduler.GetJobDetail(CreateLogDumpJob.Key, cancellationToken) != null;
    }

    public static async Task RegisterLogDumpJob(
        this ISchedulerFactory schedulerFactory,
        Guid startedBy,
        LogDumpRequestSource source,
        DateOnly fromDate,
        DateOnly toDate,
        LogFileSelection pharosFiles,
        LogFileSelection instrumentFiles,
        CancellationToken cancellationToken)
    {
        var job = JobBuilder.Create<CreateLogDumpJob>()
            .WithIdentity(CreateLogDumpJob.Key)
            .UsingJobData(new JobDataMap {
                { CreateLogDumpJob.StartedBy, startedBy },
                { CreateLogDumpJob.Source, (int)source },
                { CreateLogDumpJob.FromDate, fromDate },
                { CreateLogDumpJob.ToDate, toDate },
                { CreateLogDumpJob.PharosFiles, (int)pharosFiles },
                { CreateLogDumpJob.InstrumentFiles, (int)instrumentFiles },
                })
            .Build();

        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        await scheduler.AddJob(job, replace: false, storeNonDurableWhileAwaitingScheduling: true, cancellationToken);
    }

    public static async Task TriggerLogDumpJob(this ISchedulerFactory schedulerFactory, CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        await scheduler.TriggerJob(CreateLogDumpJob.Key, cancellationToken);
    }

    public static async Task CancelLogDumpJob(this ISchedulerFactory schedulerFactory, CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        var job = await scheduler.GetJobDetail(CreateLogDumpJob.Key, cancellationToken);
        if (job == null)
        {
            return;
        }

        var interruptRequested = await scheduler.Interrupt(CreateLogDumpJob.Key, cancellationToken);
        if (!interruptRequested)
        {
            await scheduler.DeleteJob(CreateLogDumpJob.Key, cancellationToken);
        }
    }
}
