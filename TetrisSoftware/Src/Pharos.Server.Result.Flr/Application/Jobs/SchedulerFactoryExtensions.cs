using Quartz;

namespace Pharos.Server.Result.Flr.Application.Jobs;

internal static class SchedulerFactoryExtensions
{
    public static async Task<bool> IsFlrImportJobInProgress(this ISchedulerFactory schedulerFactory)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        return await scheduler.GetJobDetail(FlrImportJob.Key) != null;
    }

    public static async Task CreateFlrImportJob(this ISchedulerFactory schedulerFactory, Guid startedBy, Guid flrFileId)
    {
        var job = JobBuilder.Create<FlrImportJob>()
            .WithIdentity(FlrImportJob.Key)
            .UsingJobData(new JobDataMap {
                { FlrImportJob.StartedBy, startedBy },
                { FlrImportJob.UploadedFileId, flrFileId },
            })
            .Build();
        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.AddJob(job, replace: true, storeNonDurableWhileAwaitingScheduling: true);
    }

    public static async Task TriggerFlrImportJob(this ISchedulerFactory schedulerFactory)
    {
        var scheduler = await schedulerFactory.GetScheduler(CancellationToken.None);
        await scheduler.TriggerJob(FlrImportJob.Key);
    }
}