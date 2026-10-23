using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.Localization;
using Pharos.Server.Request.Application.Commands.RequestGenerator;
using Quartz;

namespace Pharos.Server.Request.Application.Jobs;

[DisallowConcurrentExecution]
[PersistJobDataAfterExecution]
internal sealed class GenerateRequestsJob(
    ISender sender,
    IJobHub jobHub,
    ILogger<GenerateRequestsJob> _logger) : IJob
{
    public const string StartedBy = nameof(StartedBy);
    public const string RequestPayload = nameof(RequestPayload);

    public static readonly JobKey Key = new(JobNames.RequestGeneration);

    public async Task Execute(IJobExecutionContext context)
    {
        var jobMap = context.JobDetail.JobDataMap;
        var startedBy = jobMap.GetGuid(StartedBy);

        try
        {
            await sender.Send(CreateCommand(jobMap), context.CancellationToken);
            await jobHub.JobCompleted(JobNames.RequestGeneration, startedBy, true, Translations.RequestGenerator_GenerationCompletedSuccessfully);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request generation job failed.");
            await jobHub.JobCompleted(JobNames.RequestGeneration, startedBy, false, Translations.RequestGenerator_GenerationFailed);
        }
    }

    private static GenerateRequestsCommand CreateCommand(JobDataMap jobMap)
    {
        var request = jobMap[RequestPayload] as GenerateRequestsDto
            ?? throw new InvalidOperationException(Translations.RequestGenerator_RequestPayloadMissingOrInvalid);

        return new GenerateRequestsCommand(request);
    }
}

internal static class GenerateRequestsJobSchedulerFactoryExtensions
{
    public static async Task<bool> IsRequestGenerationJobInProgress(this ISchedulerFactory schedulerFactory)
    {
        var scheduler = await schedulerFactory.GetScheduler();
        return await scheduler.GetJobDetail(GenerateRequestsJob.Key) != null;
    }

    public static async Task CreateRequestGenerationJob(this ISchedulerFactory schedulerFactory, Guid startedBy, GenerateRequestsDto request)
    {
        var job = JobBuilder.Create<GenerateRequestsJob>()
            .WithIdentity(GenerateRequestsJob.Key)
            .UsingJobData(new JobDataMap
            {
                { GenerateRequestsJob.StartedBy, startedBy },
                { GenerateRequestsJob.RequestPayload, request }
            })
            .Build();

        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.AddJob(job, replace: true, storeNonDurableWhileAwaitingScheduling: true);
    }

    public static async Task TriggerRequestGenerationJob(this ISchedulerFactory schedulerFactory)
    {
        var scheduler = await schedulerFactory.GetScheduler(CancellationToken.None);
        await scheduler.TriggerJob(GenerateRequestsJob.Key);
    }
}
