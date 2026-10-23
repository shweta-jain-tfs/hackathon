using MediatR;
using Pharos.Server.Application.Lis;
using Pharos.Server.Lis.Application.Commands.LisStatisticsCommands;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Application.Events;

/// <summary>
/// Represents the delivery status of a message sent to LIS.
/// </summary>
internal enum DeliveryStatus
{
    /// <summary>
    /// Message is queued for delivery.
    /// </summary>
    Queued,

    /// <summary>
    /// Message was successfully delivered.
    /// </summary>
    Success,

    /// <summary>
    /// Message expired before it could be sent to LIS.
    /// </summary>
    Timeout,

    /// <summary>
    /// Message delivery encountered an error.
    /// </summary>
    Error
}

/// <summary>
/// Event raised when a LIS message delivery status changes.
/// </summary>
/// <param name="MessageId">Unique identifier of the message.</param>
/// <param name="ChannelId">The LIS channel ID where the message was sent.</param>
/// <param name="Status">The current delivery status.</param>
/// <param name="MessageType">The type of LIS message.</param>
/// <param name="Metadata">Additional metadata associated with the message.</param>
/// <param name="Exception">Exception details if the delivery failed.</param>
internal sealed record MessageDeliveryStatusChangedEvent(
    Guid MessageId,
    int ChannelId,
    DeliveryStatus Status,
    LisMessageType MessageType,
    Dictionary<string, string> Metadata,
    Exception? Exception = null) : INotification;

internal sealed class MessageDeliveryStatusChangedHandler(IPublisher publisher, ISender sender) : INotificationHandler<MessageDeliveryStatusChangedEvent>
{
    public async Task Handle(MessageDeliveryStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.Status == DeliveryStatus.Success)
        {
            await HandleSuccessfulDelivery(notification, cancellationToken);
            await SaveLisChannelStatisticAsync(notification, cancellationToken);
        }
    }

    private async Task SaveLisChannelStatisticAsync(MessageDeliveryStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.MessageType == LisMessageType.QualityControlTestResult)
        {
            return;
        }

        var sampleIds = notification.Metadata.TryGetValue(LisMessageMetadataKeys.SampleIds, out var sampleIdsStr)
            ? sampleIdsStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
            : [];

        int? testsCount = null;
        var lisStatisticsCategory = LisStatisticsCategory.Queries;
        if (notification.MessageType == LisMessageType.SampleTestResult)
        {
            testsCount = notification.Metadata.TryGetValue(LisMessageMetadataKeys.TestOrderIds, out var testOrderIdsStr)
                ? testOrderIdsStr.Split(',', StringSplitOptions.RemoveEmptyEntries).Length
                : 0;
            lisStatisticsCategory = LisStatisticsCategory.Exported;
        }

        await sender.Send(new SaveLisStatisticsCommand(
            notification.ChannelId,
            lisStatisticsCategory,
            sampleIds.Length,
            testsCount,
            sampleIds.LastOrDefault()), cancellationToken);
    }

    private async Task HandleSuccessfulDelivery(MessageDeliveryStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.MessageType == LisMessageType.SampleTestResult)
        {
            await HandleTestResultSuccess(notification, cancellationToken);
        }
        else if (notification.MessageType == LisMessageType.QualityControlTestResult)
        {
            await HandleQualityControlResultSuccess(notification, cancellationToken);
        }
    }

    private async Task HandleTestResultSuccess(MessageDeliveryStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.Metadata.TryGetValue(LisMessageMetadataKeys.TestOrderIds, out var testOrderIdsStr) && !string.IsNullOrEmpty(testOrderIdsStr))
        {
            var testOrderIds = testOrderIdsStr
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(Guid.Parse)
                .ToList();

            if (testOrderIds.Count == 0)
            {
                return;
            }

            await publisher.Publish(new SampleTestResultsSentToLisEvent(testOrderIds, notification.ChannelId), cancellationToken);
        }
    }

    private async Task HandleQualityControlResultSuccess(MessageDeliveryStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.Metadata.TryGetValue(LisMessageMetadataKeys.QcTestResultId, out var qcResultIdStr)
                && int.TryParse(qcResultIdStr, out var qcResultId))
        {
            await publisher.Publish(new QualityControlTestResultSentToLisEvent(qcResultId), cancellationToken);
        }
    }
}
