using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Application.Interfaces;

/// <summary>
/// Represents an abstraction of a persistent message queue.
/// </summary>
internal interface ILisMessageQueue
{
    /// <summary>
    /// Puts a message in the persistent queue.
    /// </summary>
    /// <param name="message">The message object.</param>
    /// <returns>The ID of the message in the queue.</returns>
    Task<Guid> EnqueueAsync(QueuedLisMessage message);

    /// <summary>
    /// Removes a message from the persistent queue.
    /// </summary>
    /// <param name="queuedMessageId">The ID of the queued message.</param>
    /// <returns>Task object.</returns>
    Task DequeueAsync(Guid queuedMessageId);

    /// <summary>
    /// Returns the item at the beginning of the queue without removing it, filtered by dispatch mode.
    /// </summary>
    /// <param name="channelId">The LIS channel ID.</param>
    /// <param name="dispatchMode">The dispatch mode to apply.</param>
    /// <returns>The item at the beginning of the queue or null if the queue is empty.</returns>
    Task<QueuedLisMessage?> PeekAsync(int channelId, LisDispatchMode dispatchMode);

    /// <summary>
    /// Deletes all messages for a specific channel.
    /// </summary>
    /// <param name="channelId">The LIS channel ID.</param>
    /// <returns>The number of messages deleted.</returns>
    Task<int> DeleteMessagesByChannelAsync(int channelId);
}