using Microsoft.Extensions.Logging;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Domain;
using Quartz;

namespace Pharos.Server.LabCommunity.Application.Services;

internal sealed class LabCommunityJobScheduler<TJob>(
    ISchedulerFactory schedulerFactory,
    ILogger<LabCommunityJobScheduler<TJob>> logger) : ILabCommunityJobScheduler
    where TJob : ILabCommunityJob
{
    public bool ShouldReconfigureAfterConfigurationPull(Configuration? newConfiguration, Configuration? previousConfiguration)
    {
        return TJob.ShouldReconfigureAfterConfigurationPull(newConfiguration, previousConfiguration);
    }

    public TimeSpan ResolveInterval(LabCommunityDefaultConfiguration defaultConfiguration, Configuration? configuration)
    {
        var interval = TJob.ResolveInterval(defaultConfiguration, configuration);

        if (interval <= TimeSpan.Zero)
        {
            logger.LogWarning(
                "Job {JobKey} resolved a zero or negative interval. Falling back to default configuration.",
                TJob.Key.Name);

            return TJob.ResolveInterval(defaultConfiguration, configuration: null);
        }

        return interval;
    }

    public bool ShouldStop(LabCommunityDefaultConfiguration defaultConfiguration, Configuration? configuration)
    {
        return TJob.ShouldStop(defaultConfiguration, configuration);
    }

    public async Task ConfigureScheduler(TimeSpan interval, CancellationToken cancellationToken, TimeSpan initialDelay = default)
    {
        if (interval <= TimeSpan.Zero)
        {
            logger.LogWarning(
                "LabCommunity scheduler for job {JobKey} was not configured because the interval is zero or negative.",
                TJob.Key.Name);
            return;
        }

        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);

        await scheduler.DeleteJob(TJob.Key, cancellationToken);

        var job = JobBuilder.Create<TJob>()
            .WithIdentity(TJob.Key)
            .Build();

        var triggerBuilder = TriggerBuilder.Create()
            .WithIdentity(TJob.Trigger)
            .ForJob(TJob.Key);

        if (initialDelay > TimeSpan.Zero)
        {
            triggerBuilder.StartAt(DateTimeOffset.UtcNow.Add(initialDelay));
        }
        else
        {
            triggerBuilder.StartNow();
        }

        var trigger = triggerBuilder
            .WithSimpleSchedule(x => x
                .WithInterval(interval)
                .RepeatForever())
            .Build();

        await scheduler.ScheduleJob(job, trigger, cancellationToken);

        logger.LogInformation(
            "LabCommunity scheduler configured for job {JobKey}. Interval = {Interval}.",
            TJob.Key.Name,
            interval);
    }

    public async Task StopScheduler(CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);

        await scheduler.DeleteJob(TJob.Key, cancellationToken);

        logger.LogInformation(
            "LabCommunity scheduler stopped for job {JobKey}.",
            TJob.Key.Name);
    }
}
