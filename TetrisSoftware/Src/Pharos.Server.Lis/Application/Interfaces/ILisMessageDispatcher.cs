using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Application.Interfaces;

/// <summary>
/// Defines a dispatcher for queuing and asynchronously dispatching LIS messages.
/// Manages per-channel dispatch jobs and publishes delivery status change events via MediatR.
/// </summary>
internal interface ILisMessageDispatcher
{
    /// <summary>
    /// Puts a LIS message to the queue and triggers the asynchronous dispatch operation.
    /// </summary>
    /// <param name="channelId">The LIS channel ID. Higher priority messages are dispatched first.</param>
    /// <param name="priority">The message priority.</param>
    /// <param name="messageType">The message type.</param>
    /// <param name="metadata">The message metadata used to track delivery status.</param>
    /// <param name="message">The protocol-specific LIS message.</param>
    /// <param name="deliveryTimeout">Optional timeout duration after which the message expires. Default is null (no expiration).</param>
    /// <returns>The unique identifier of the queued message.</returns>
    Task<Guid> PostMessage(
        int channelId,
        LisMessagePriority priority,
        LisMessageType messageType,
        Dictionary<string, string> metadata,
        ILisMessage message,
        TimeSpan? deliveryTimeout = null);
}