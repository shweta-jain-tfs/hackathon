using Quartz;

namespace Pharos.Server.Parameters.Application.Jobs;

internal static class SchedulerFactoryExtensions
{
    public static async Task<bool> ImportParametersJobExists(this ISchedulerFactory schedulerFactory)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        return await scheduler.GetJobDetail(ImportParametersJob.Key) != null;
    }

    public static async Task CreateImportParametersJob(this ISchedulerFactory schedulerFactory, Guid startedBy, Guid packageFileId)
    {
        var job = JobBuilder.Create<ImportParametersJob>()
            .WithIdentity(ImportParametersJob.Key)
            .UsingJobData(new JobDataMap {
                { ImportParametersJob.StartedBy, startedBy },
                { ImportParametersJob.UploadedFileId, packageFileId },
            })
            .Build();
        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.AddJob(job, replace: true, storeNonDurableWhileAwaitingScheduling: true);
    }

    public static async Task TriggerImportParametersJob(this ISchedulerFactory schedulerFactory)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.TriggerJob(ImportParametersJob.Key);
    }

    public static async Task<Guid> CancelImportParametersJob(this ISchedulerFactory schedulerFactory)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        var jobDetail = await scheduler.GetJobDetail(ImportParametersJob.Key);
        if (jobDetail == null)
        {
            return Guid.Empty;
        }
        var packageFileId = jobDetail.JobDataMap.GetGuid(ImportParametersJob.UploadedFileId);

        if (!await scheduler.Interrupt(ImportParametersJob.Key))
        {
            await scheduler.DeleteJob(ImportParametersJob.Key);
        }
        return packageFileId;
    }
}