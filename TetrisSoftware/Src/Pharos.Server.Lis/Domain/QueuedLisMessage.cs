using Pharos.Server.Domain.Common;

namespace Pharos.Server.Lis.Domain;

/// <summary>
/// Represents a message that is waiting to be sent to LIS.
/// Messages are dispatched based on priority and creation time.
/// </summary>
public class QueuedLisMessage : BaseEntity
{
    /// <summary>
    /// The unique identifier for the queued message.
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// The channel ID of the LIS to which the message must be sent.
    /// </summary>
    public int ChannelId { get; set; }

    /// <summary>
    /// The message priority. The Lower numeric values indicate higher priority.
    /// </summary>
    /// <seealso cref="LisMessagePriority"/>
    public int Priority { get; set; }

    /// <summary>
    /// The message creation timestamp.
    /// </summary>
    public DateTimeOffset Created { get; set; }

    /// <summary>
    /// The type of message
    /// </summary>
    public LisMessageType MessageType { get; set; }

    /// <summary>
    /// The expiration timestamp for the message
    /// </summary>
    public DateTimeOffset? DeliveryTimeout { get; set; }

    /// <summary>
    /// The message metadata as a JSON string. Contains tracking information for delivery status.
    /// </summary>
    public string? MessageMetadata { get; set; }

    /// <summary>
    /// The protocol-specific message data.
    /// </summary>
    [ProtectedData]
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string MessageData { get; set; } = default!;
}