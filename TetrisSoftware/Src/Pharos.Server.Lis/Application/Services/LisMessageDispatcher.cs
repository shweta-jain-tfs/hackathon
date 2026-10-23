using System.Collections.Concurrent;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Infrastructure;
using Pharos.Server.Infrastructure.Logging;
using Pharos.Server.Lis.Application.Commands;
using Pharos.Server.Lis.Application.Events;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Application.Jobs;
using Pharos.Server.Lis.Domain;
using Quartz;

namespace Pharos.Server.Lis.Application.Services;

internal sealed class LisMessageDispatcher(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILisConnectionPool connectionPool,
    ILisRequestPolling lisRequestPolling,
    IOptionsMonitor<LisSettings> lisSettings,
    ISchedulerFactory schedulerFactory,
    IDateTime dateTime,
    ILogger<LisMessageDispatcher> logger) : IHostedService, IDisposable, ILisMessageDispatcher
{
    private bool _redactSensitiveData = true;
    private IScheduler? _scheduler;
    private bool _stopping;
    private readonly ConcurrentDictionary<int, ChannelJobInfo> _channelJobs = new();

    private sealed record ChannelJobInfo(JobKey JobKey, TriggerKey DefaultTriggerKey, TriggerKey? PeriodicTriggerKey);

    /// <summary>
    /// Testing infrastructure can set this property to true to suspend message dispatching.
    /// </summary>
    internal bool SuspendDispatching { get; set; }

    public void Dispose()
    {
        connectionPool.ConnectionAdded -= OnConnectionAddedToPool;
        connectionPool.ConnectionRemoved -= OnConnectionRemovedFromPool;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _redactSensitiveData = configuration.IsRedactionEnabled();

        logger.LogInformation("Starting the LIS module.");

        connectionPool.ConnectionAdded += OnConnectionAddedToPool;
        connectionPool.ConnectionRemoved += OnConnectionRemovedFromPool;

        _scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        await InitializeChannelJobs(cancellationToken);
        await UpdateResetLisStatisticsJob(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _stopping = true;
        logger.LogInformation("Stopping the LIS module.");

        lisRequestPolling.StopAll();

        if (_scheduler != null)
        {
            await _scheduler.Shutdown(waitForJobsToComplete: true, cancellationToken);
        }

        _channelJobs.Clear();
    }

    public async Task<Guid> PostMessage(
        int channelId,
        LisMessagePriority priority,
        LisMessageType messageType,
        Dictionary<string, string> metadata,
        ILisMessage message,
        TimeSpan? deliveryTimeout = null)
    {
        logger.LogDebug("Posting an LIS message for delivery. Channel: {LisChannelId}, MessageType: {MessageType}", channelId, messageType);

        try
        {
            using var scope = scopeFactory.CreateScope();
            var messageQueue = scope.ServiceProvider.GetRequiredService<ILisMessageQueue>();

            var queuedMessage = new QueuedLisMessage
            {
                MessageId = Guid.NewGuid(),
                ChannelId = channelId,
                Priority = (int)priority,
                Created = dateTime.Now,
                MessageType = messageType,
                MessageMetadata = JsonSerializer.Serialize(metadata),
                DeliveryTimeout = deliveryTimeout.HasValue ? dateTime.Now.Add(deliveryTimeout.Value) : null,
                MessageData = message.GetMessageString(),
            };

            var messageId = await messageQueue.EnqueueAsync(queuedMessage);

            // We log the message data with sensitive information redacted to avoid logging sensitive patient data.
            logger.LogInformation(
                "Put an LIS message to the queue for delivery. Channel: {LisChannelId}, MessageId: {MessageId}, MessageData: {MessageData}",
                channelId,
                messageId,
                message.GetMessageString(_redactSensitiveData));

            await NotifyDeliveryStatusChanged(messageId, channelId, DeliveryStatus.Queued, messageType, metadata);
            await TriggerChannelJob(channelId);

            return messageId;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to post an LIS message for delivery. Channel: {LisChannelId}, MessageType: {MessageType}", channelId, messageType);
            throw;
        }
    }

    private async Task InitializeChannelJobs(CancellationToken cancellationToken)
    {
        var lisChannels = lisSettings.CurrentValue.LisChannels;
        for (var channelId = 0; channelId < lisChannels.Count; channelId++)
        {
            var channel = lisChannels[channelId];
            if (channel.IsEnabled)
            {
                await CreateChannelJob(channelId, channel, cancellationToken);
            }
        }
    }

    private async Task CreateChannelJob(int channelId, LisChannel channel, CancellationToken cancellationToken)
    {
        if (_scheduler == null)
        {
            return;
        }

        var jobKey = DispatchMessagesJob.Key(channelId);
        var job = JobBuilder.Create<DispatchMessagesJob>()
            .WithIdentity(jobKey)
            .UsingJobData(DispatchMessagesJob.ChannelId, channelId)
            .Build();

        var (hasPeriodicExport, intervalInMinutes) = GetPeriodicExportSettings(channel);

        var defaultTriggerKey = DispatchMessagesJob.DefaultTriggerKey(channelId);
        var defaultTrigger = CreateDefaultTrigger(defaultTriggerKey, jobKey, hasPeriodicExport);
        await _scheduler.ScheduleJob(job, defaultTrigger, cancellationToken);

        TriggerKey? periodicTriggerKey = null;
        if (hasPeriodicExport)
        {
            periodicTriggerKey = DispatchMessagesJob.PeriodicTriggerKey(channelId);
            var periodicTrigger = CreatePeriodicTrigger(periodicTriggerKey, jobKey, intervalInMinutes);
            await _scheduler.ScheduleJob(periodicTrigger, cancellationToken);
        }

        _channelJobs[channelId] = new ChannelJobInfo(jobKey, defaultTriggerKey, periodicTriggerKey);
    }

    private async Task TriggerChannelJob(int channelId)
    {
        if (_stopping || SuspendDispatching || _scheduler == null || lisSettings.CurrentValue.LisChannels.Count == 0)
        {
            return;
        }

        try
        {
            var channel = lisSettings.CurrentValue.LisChannels[channelId];
            var (hasPeriodicExport, _) = GetPeriodicExportSettings(channel);
            var defaultDispatchMode = hasPeriodicExport ? LisDispatchMode.ExcludeLow : LisDispatchMode.All;

            var jobDataMap = new JobDataMap
            {
                { DispatchMessagesJob.DispatchMode, (int)defaultDispatchMode }
            };

            await _scheduler.TriggerJob(DispatchMessagesJob.Key(channelId), jobDataMap, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to trigger dispatch job for channel ID {LisChannelId}.", channelId);
        }
    }

    private async Task OnSettingsChanged(LisSettings settings, CancellationToken cancellationToken)
    {
        if (_stopping || _scheduler == null)
        {
            return;
        }

        try
        {
            var currentChannelIds = Enumerable.Range(0, settings.LisChannels.Count).ToHashSet();
            var existingChannelIds = _channelJobs.Keys.ToHashSet();

            var deletedChannels = existingChannelIds.Except(currentChannelIds);
            foreach (var channelId in deletedChannels)
            {
                await RemoveChannelJob(channelId, cancellationToken);
            }

            for (var channelId = 0; channelId < settings.LisChannels.Count; channelId++)
            {
                var channel = settings.LisChannels[channelId];
                var jobExists = existingChannelIds.Contains(channelId);

                if (channel.IsEnabled && jobExists)
                {
                    await UpdateChannelJob(channelId, channel, cancellationToken);
                }
                else if (channel.IsEnabled && !jobExists)
                {
                    await CreateChannelJob(channelId, channel, cancellationToken);
                }
                else if (!channel.IsEnabled && jobExists)
                {
                    await RemoveChannelJob(channelId, cancellationToken);
                }
            }
            await UpdateResetLisStatisticsJob(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update channel jobs after LIS settings change.");
        }
    }

    private async Task UpdateChannelJob(int channelId, LisChannel channel, CancellationToken cancellationToken)
    {
        if (_scheduler == null || !_channelJobs.TryGetValue(channelId, out var jobInfo))
        {
            return;
        }

        var (hasPeriodicExport, intervalInMinutes) = GetPeriodicExportSettings(channel);

        var defaultTrigger = CreateDefaultTrigger(jobInfo.DefaultTriggerKey, jobInfo.JobKey, hasPeriodicExport);
        await _scheduler.RescheduleJob(jobInfo.DefaultTriggerKey, defaultTrigger, cancellationToken);

        if (hasPeriodicExport)
        {
            var periodicTriggerKey = jobInfo.PeriodicTriggerKey ?? DispatchMessagesJob.PeriodicTriggerKey(channelId);
            var periodicTrigger = CreatePeriodicTrigger(periodicTriggerKey, jobInfo.JobKey, intervalInMinutes);

            if (jobInfo.PeriodicTriggerKey != null)
            {
                await _scheduler.RescheduleJob(periodicTriggerKey, periodicTrigger, cancellationToken);
            }
            else
            {
                await _scheduler.ScheduleJob(periodicTrigger, cancellationToken);
                _channelJobs[channelId] = jobInfo with { PeriodicTriggerKey = periodicTriggerKey };
            }
        }
        else if (jobInfo.PeriodicTriggerKey != null)
        {
            await _scheduler.UnscheduleJob(jobInfo.PeriodicTriggerKey, cancellationToken);
            _channelJobs[channelId] = jobInfo with { PeriodicTriggerKey = null };
        }
    }

    private async Task RemoveChannelJob(int channelId, CancellationToken cancellationToken)
    {
        if (_scheduler == null || !_channelJobs.TryRemove(channelId, out var jobInfo))
        {
            return;
        }

        try
        {
            await _scheduler.DeleteJob(jobInfo.JobKey, cancellationToken);

            using var scope = scopeFactory.CreateScope();
            var messageQueue = scope.ServiceProvider.GetRequiredService<ILisMessageQueue>();
            var deletedCount = await messageQueue.DeleteMessagesByChannelAsync(channelId);

            if (deletedCount > 0)
            {
                logger.LogWarning("Removed dispatch job for channel ID {LisChannelId} and deleted {Count} queued messages", channelId, deletedCount);
                // TODO: Publish an event for deleted messages if notification is needed
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove dispatch job for channel ID {LisChannelId}.", channelId);
        }
    }

    private async Task UpdateResetLisStatisticsJob(CancellationToken cancellationToken)
    {
        if (_scheduler == null)
        {
            return;
        }
        var isMidnightResetEnabled = lisSettings.CurrentValue.LisChannels.Any(x => x.Other.ResetStatisticsAtMidnight);
        var isMidnightResetJobExists = await _scheduler.CheckExists(ResetLisStatisticsJob.JobKey, cancellationToken);

        if (!isMidnightResetEnabled && isMidnightResetJobExists)
        {
            await _scheduler.DeleteJob(ResetLisStatisticsJob.JobKey, cancellationToken);
        }
        else if (isMidnightResetEnabled && !isMidnightResetJobExists)
        {
            var job = JobBuilder.Create<ResetLisStatisticsJob>()
                .WithIdentity(ResetLisStatisticsJob.JobKey)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity(ResetLisStatisticsJob.MidnightTriggerKey)
                .ForJob(ResetLisStatisticsJob.JobKey)
                .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(0, 0).InTimeZone(TimeZoneInfo.Local))
                .Build();

            await _scheduler.ScheduleJob(job, trigger, cancellationToken);
        }
        else
        {
            logger.LogInformation("Midnight statistics reset job is already in the expected state. Enabled: {IsEnabled}, Exists: {JobExists}", isMidnightResetEnabled, isMidnightResetJobExists);
        }
    }

    private static (bool HasPeriodicExport, int IntervalInMinutes) GetPeriodicExportSettings(LisChannel channel)
    {
        var exportSettings = channel.ExportSettings;
        var hasPeriodicExport = exportSettings.PeriodicExport && exportSettings.Interval > 0;
        return (hasPeriodicExport, exportSettings.Interval);
    }

    private static ITrigger CreateDefaultTrigger(TriggerKey triggerKey, JobKey jobKey, bool hasPeriodicExport)
    {
        var dispatchMode = hasPeriodicExport ? LisDispatchMode.ExcludeLow : LisDispatchMode.All;

        return TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .ForJob(jobKey)
            .UsingJobData(DispatchMessagesJob.DispatchMode, (int)dispatchMode)
            .StartNow()
            .WithSimpleSchedule(x => x
                .WithInterval(TimeSpan.FromMinutes(1))
                .RepeatForever())
            .Build();
    }

    private ITrigger CreatePeriodicTrigger(TriggerKey triggerKey, JobKey jobKey, int intervalInMinutes)
    {
        var startAt = dateTime.Now.AddMinutes(intervalInMinutes);

        return TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .ForJob(jobKey)
            .UsingJobData(DispatchMessagesJob.DispatchMode, (int)LisDispatchMode.OnlyLow)
            .StartAt(startAt)
            .WithSimpleSchedule(x => x
                .WithInterval(TimeSpan.FromMinutes(intervalInMinutes))
                .RepeatForever())
            .Build();
    }

    private async Task NotifyDeliveryStatusChanged(
        Guid messageId,
        int channelId,
        DeliveryStatus status,
        LisMessageType messageType,
        Dictionary<string, string> metadata,
        Exception? exception = null)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.Publish(new MessageDeliveryStatusChangedEvent(
                messageId,
                channelId,
                status,
                messageType,
                metadata,
                exception));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish delivery status change for message {MessageId}, Status {Status}", messageId, status);
        }
    }

    private void OnConnectionAddedToPool(object? sender, ILisConnection connection)
    {
        connection.TestOrderReceived += OnTestOrderReceived;

        lisRequestPolling.Start(connection.LisChannelId);
    }

    private void OnConnectionRemovedFromPool(object? sender, ILisConnection connection)
    {
        connection.TestOrderReceived -= OnTestOrderReceived;

        if (lisRequestPolling.IsPollingForChannel(connection.LisChannelId))
        {
            lisRequestPolling.Stop(connection.LisChannelId);
        }
    }

    private void OnTestOrderReceived(object? sender, TestOrderReceivedEventArgs e)
    {
        var channelId = ((ILisConnection)sender!).LisChannelId;
        logger.LogInformation("LIS message received. Channel: {LisChannelId}, MessageData: {MessageData}", channelId, e.ReceivedMessage.GetMessageString(_redactSensitiveData));

        try
        {
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
            var IsTestOrdersProcessed = mediator.Send(new ProcessLisTestOrdersCommand
            {
                LisChannelId = channelId,
                Patients = e.Patients
            }).GetAwaiter().GetResult();

            mediator.Send(new SendMessageAcknowledgementToLisCommand(channelId, e.Metadata, IsTestOrdersProcessed)).GetAwaiter().GetResult();
        }
        catch (Exception error)
        {
            logger.LogError(error, "Failed to process test orders received from LIS for channel ID {LisConnectionId}.", channelId);
        }
        
    }

    internal sealed class LisSettingsChangedEventHandler(
        ILisMessageDispatcher messageDispatcher,
        ILogger<LisSettingsChangedEventHandler> logger) : INotificationHandler<SettingsChangedEvent<LisSettings>>
    {
        public async Task Handle(SettingsChangedEvent<LisSettings> notification, CancellationToken cancellationToken)
        {
            var dispatcher = (LisMessageDispatcher)messageDispatcher;
            logger.LogInformation("Updating LIS message dispatcher jobs with new settings");

            await dispatcher.OnSettingsChanged(notification.NewValue, cancellationToken);
        }
    }
}