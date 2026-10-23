using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Request.Application.Commands.TestOrderScheduling;
using Pharos.Server.Request.Application.Interfaces;
using Quartz;

namespace Pharos.Server.Request.Application.Jobs;

[DisallowConcurrentExecution]
internal sealed class TrySendAllNotStartedTestsToInstrumentsJob : IJob
{
    private readonly IRequestDbContext _dbContext;
    private readonly ILogger<TrySendAllNotStartedTestsToInstrumentsJob> _logger;
    private readonly ISender _sender;

    public static readonly JobKey Key = new("try-send-all-not-started-tests-to-instruments");
    public static readonly TriggerKey DelayedTrigger = new("try-send-all-not-started-tests-to-instruments-delayed-trigger");
    public static readonly TriggerKey RecurringTrigger = new("try-send-all-not-started-tests-to-instruments-recurring-trigger");

    public TrySendAllNotStartedTestsToInstrumentsJob(
        IRequestDbContext dbContext,
        ISender sender,
        ILogger<TrySendAllNotStartedTestsToInstrumentsJob> logger)
    {
        _sender = sender;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;

        var requestIds = await _dbContext.TestOrders // Get IDs of all requests with not started test orders.
                .Where(x => x.State == TestOrderState.NotStarted)
                .Select(x => x.RequestId)
                .Distinct()
                .ToHashSetAsync(cancellationToken)
                .ConfigureAwait(false);

        if (requestIds.Count == 0)
        {
            return;
        }

        // Iterate through requests, get not started test orders, dispatch them, and update statuses.
        foreach (var requestId in requestIds)
        {
            var request = await _dbContext.Requests
                .Where(x => x.Id == requestId)
                .Include(x => x.TestOrders)
                .ThenInclude(x => x.ReflexDefinitions)
                .Include(x => x.TestOrders)
                .ThenInclude(x => x.TestOrderCustomParameters)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (request is null)
            {
                continue;
            }

            try
            {
                await _sender.Send(new SyncTestOrdersWithInstrumentCommand(request, PublishRequestUpdatedEvent: true), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _dbContext.ClearChanges();
                _logger.LogError(e, "Failed to send test orders to the instrument for request ID={RequestId}, Sample ID={SampleId}. ", request.Id, request.SampleId);
            }
        }
    }
}

internal static class SchedulerFactoryExtensions
{
    private static readonly TimeSpan _interval = TimeSpan.FromMinutes(15);

    public static async Task ConfigureTrySendAllNotStartedTestsToInstrumentsJob(this ISchedulerFactory schedulerFactory, CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);

        await scheduler.DeleteJob(TrySendAllNotStartedTestsToInstrumentsJob.Key, cancellationToken);

        var job = JobBuilder.Create<TrySendAllNotStartedTestsToInstrumentsJob>()
            .WithIdentity(TrySendAllNotStartedTestsToInstrumentsJob.Key)
            .Build();

        var trigger = CreateRecurringTrigger(DateTimeOffset.UtcNow);

        await scheduler.ScheduleJob(job, trigger, cancellationToken);
    }

    public static async Task TriggerTrySendAllNotStartedTestsToInstrumentsJobImmediately(this ISchedulerFactory schedulerFactory, CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetOrConfigureTrySendAllNotStartedTestsToInstrumentsScheduler(cancellationToken);
        await RescheduleMainTrigger(scheduler, cancellationToken);
        await scheduler.TriggerJob(TrySendAllNotStartedTestsToInstrumentsJob.Key, cancellationToken);
    }

    public static async Task TriggerTrySendAllNotStartedTestsToInstrumentsJobWithDelay(this ISchedulerFactory schedulerFactory, CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetOrConfigureTrySendAllNotStartedTestsToInstrumentsScheduler(cancellationToken);
        await RescheduleMainTrigger(scheduler, cancellationToken);

        if (await scheduler.CheckExists(TrySendAllNotStartedTestsToInstrumentsJob.DelayedTrigger, cancellationToken))
        {
            return;
        }

        var delayedStart = DateTimeOffset.UtcNow.AddSeconds(10);
        var delayedTrigger = CreateDelayedTrigger(delayedStart);

        await scheduler.ScheduleJob(delayedTrigger, cancellationToken);
    }

    private static async Task<IScheduler> GetOrConfigureTrySendAllNotStartedTestsToInstrumentsScheduler(this ISchedulerFactory schedulerFactory, CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);

        if (await scheduler.CheckExists(TrySendAllNotStartedTestsToInstrumentsJob.Key, cancellationToken))
        {
            return scheduler;
        }

        await schedulerFactory.ConfigureTrySendAllNotStartedTestsToInstrumentsJob(cancellationToken);
        return await schedulerFactory.GetScheduler(cancellationToken);
    }

    private static async Task RescheduleMainTrigger(IScheduler scheduler, CancellationToken cancellationToken)
    {
        var nextAutomaticStart = DateTimeOffset.UtcNow.Add(_interval);
        await scheduler.RescheduleJob(TrySendAllNotStartedTestsToInstrumentsJob.RecurringTrigger, CreateRecurringTrigger(nextAutomaticStart), cancellationToken);
    }

    private static ITrigger CreateDelayedTrigger(DateTimeOffset startAt)
    {
        return Quartz.TriggerBuilder.Create()
            .WithIdentity(TrySendAllNotStartedTestsToInstrumentsJob.DelayedTrigger)
            .ForJob(TrySendAllNotStartedTestsToInstrumentsJob.Key)
            .StartAt(startAt)
            .Build();
    }

    private static ITrigger CreateRecurringTrigger(DateTimeOffset startAt)
    {
        return Quartz.TriggerBuilder.Create()
            .WithIdentity(TrySendAllNotStartedTestsToInstrumentsJob.RecurringTrigger)
            .ForJob(TrySendAllNotStartedTestsToInstrumentsJob.Key)
            .StartAt(startAt)
            .WithSimpleSchedule(x => x.WithInterval(_interval).RepeatForever())
            .Build();
    }
}
