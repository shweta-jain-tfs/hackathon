using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Infrastructure;
using Pharos.Server.Lis.Application.Events;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;
using Quartz;

namespace Pharos.Server.Lis.Application.Jobs;

/// <summary>
/// Job that dispatches messages from the LIS message queue for a specific channel.
/// Supports priority-based filtering via <see cref="LisDispatchMode"/> for default and periodic export scenarios.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class DispatchMessagesJob(
    IServiceScopeFactory scopeFactory,
    ILisConnectionPool connectionPool,
    IOptionsMonitor<LisSettings> lisSettings,
    IDateTime dateTime,
    ILogger<DispatchMessagesJob> logger) : IJob
{
    public const string ChannelId = nameof(ChannelId);
    public const string DispatchMode = nameof(DispatchMode);

    private const int _maxRetryAttempts = 3;
    private static readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(2);

    public static JobKey Key(int channelId)
    {
        return new($"dispatch-messages-{channelId}");
    }

    public static TriggerKey DefaultTriggerKey(int channelId)
    {
        return new($"dispatch-messages-default-{channelId}");
    }

    public static TriggerKey PeriodicTriggerKey(int channelId)
    {
        return new($"dispatch-messages-periodic-{channelId}");
    }

    public async Task Execute(IJobExecutionContext context)
    {
        int? channelId = null;
        LisDispatchMode? dispatchMode = null;

        try
        {
            var jobDataMap = context.MergedJobDataMap;

            channelId = jobDataMap.GetInt(ChannelId);
            dispatchMode = (LisDispatchMode)jobDataMap.GetInt(DispatchMode);

            await DispatchMessages(channelId.Value, dispatchMode.Value, context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute DispatchMessagesJob for channel ID {LisChannelId} with dispatch mode {Mode}", channelId, dispatchMode);
        }
    }

    private async Task DispatchMessages(int channelId, LisDispatchMode dispatchMode, CancellationToken cancellationToken)
    {
        var numberOfMessagesSent = 0;

        using var scope = scopeFactory.CreateScope();
        var messageQueue = scope.ServiceProvider.GetRequiredService<ILisMessageQueue>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        logger.LogInformation("Dispatching messages from queue for channel ID {LisChannelId} with dispatch mode {Mode}", channelId, dispatchMode);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var queueItem = await messageQueue.PeekAsync(channelId, dispatchMode).ConfigureAwait(false);
                if (queueItem == null)
                {
                    break;
                }

                if (IsMessageExpired(queueItem))
                {
                    logger.LogWarning("Message {MessageId} expired (timeout={Timeout}) for channel ID {LisChannelId}. Removing from queue.", queueItem.MessageId, queueItem.DeliveryTimeout, queueItem.ChannelId);

                    await messageQueue.DequeueAsync(queueItem.MessageId).ConfigureAwait(false);
                    await NotifyDeliveryStatusChanged(publisher, queueItem, DeliveryStatus.Timeout);
                    continue;
                }

                var useImportConnection = ShouldUseImportConnection(queueItem);
                var connection = connectionPool.LockConnection(queueItem.ChannelId, useImportConnection);
                if (connection == null)
                {
                    break;
                }

                try
                {
                    if (connection.State == LisConnectionState.Closed && !await connection.OpenAsync().ConfigureAwait(false))
                    {
                        break;
                    }

                    var success = await SendMessageWithRetry(connection, queueItem, cancellationToken);
                    if (success)
                    {
                        logger.LogInformation("Successfully sent an LIS message. Channel: {LisChannelId}, MessageId: {MessageId}", queueItem.ChannelId, queueItem.MessageId);
                        await messageQueue.DequeueAsync(queueItem.MessageId).ConfigureAwait(false);
                        await NotifyDeliveryStatusChanged(publisher, queueItem, DeliveryStatus.Success);
                        numberOfMessagesSent++;
                    }
                    else
                    {
                        logger.LogWarning("Failed to send an LIS message after {MaxAttempts} retry attempts. Channel: {LisChannelId}, MessageId: {MessageId}", _maxRetryAttempts, queueItem.ChannelId, queueItem.MessageId);
                        await messageQueue.DequeueAsync(queueItem.MessageId).ConfigureAwait(false);
                        var exception = new InvalidOperationException($"Failed to send message {queueItem.MessageId} after {_maxRetryAttempts} retry attempts");
                        await NotifyDeliveryStatusChanged(publisher, queueItem, DeliveryStatus.Error, exception);
                    }
                }
                finally
                {
                    connectionPool.UnlockConnection(connection);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during message dispatch for the LIS channel {LisChannelId}. Messages remain in the queue.", channelId);
        }
        finally
        {
            logger.LogInformation("Finished dispatching messages for the LIS channel {LisChannelId}. Sent {Count} messages.", channelId, numberOfMessagesSent);
        }
    }

    private bool IsMessageExpired(QueuedLisMessage message)
    {
        return message.DeliveryTimeout.HasValue && dateTime.Now > message.DeliveryTimeout.Value;
    }

    private bool ShouldUseImportConnection(QueuedLisMessage message)
    {
        if (message.MessageType != LisMessageType.Acknowledgement)
        {
            return false;
        }

        var channels = lisSettings.CurrentValue.LisChannels;
        if (message.ChannelId < 0 || message.ChannelId >= channels.Count)
        {
            return false;
        }

        var tcpSettings = channels[message.ChannelId].HL7Settings?.CommunicationSettings?.TcpSettings;
        return tcpSettings is { UseSeparateExportPort: true, SendAcknowledgeOnImportPort: true };
    }

    private async Task<bool> SendMessageWithRetry(ILisConnection connection, QueuedLisMessage queueItem, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= _maxRetryAttempts; attempt++)
        {
            try
            {
                logger.LogDebug("Sending an LIS message. Channel: {LisChannelId}, MessageId: {MessageId}, Attempt {Attempt} of {MaxAttempts}", queueItem.ChannelId, queueItem.MessageId, attempt, _maxRetryAttempts);
                var success = await connection.SendMessageAsync(queueItem.MessageType, queueItem.MessageData);
                if (success)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error sending message {MessageId} on attempt {Attempt}/{MaxAttempts} to channel ID {LisChannelId}", queueItem.MessageId, attempt, _maxRetryAttempts, queueItem.ChannelId);
            }

            if (attempt < _maxRetryAttempts)
            {
                await Task.Delay(_retryDelay, cancellationToken);
            }
        }

        return false;
    }

    private static async Task NotifyDeliveryStatusChanged(
        IPublisher publisher,
        QueuedLisMessage queueItem,
        DeliveryStatus status,
        Exception? exception = null)
    {
        var metadata = DeserializeMessageMetadata(queueItem.MessageMetadata);

        await publisher.Publish(new MessageDeliveryStatusChangedEvent(
            queueItem.MessageId,
            queueItem.ChannelId,
            status,
            queueItem.MessageType,
            metadata,
            exception));
    }

    private static Dictionary<string, string> DeserializeMessageMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson)
                   ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }
}
