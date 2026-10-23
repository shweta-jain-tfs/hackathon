using Pharos.Server.Application.Lis;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Application.Interfaces;

/// <summary>
/// Defines the role of a LIS connection.
/// </summary>
[Flags]
public enum LisConnectionRole
{
    /// <summary>
    /// The connection handles inbound messages (test orders).
    /// </summary>
    Import = 1,

    /// <summary>
    /// The connection handles outbound messages (test results, queries).
    /// </summary>
    Export = 2,

    /// <summary>
    /// The connection handles both inbound and outbound messages.
    /// Used when no dedicated export port is configured.
    /// </summary>
    Both = Import | Export
}

/// <summary>
/// An abstraction of LIS connection. Connection is used to transmit and receive messages
/// under a LIS-protocol.
/// </summary>
/// <remarks>
/// Note to implementors: do not use this interface for low-level connections like TCP.
/// Transport specifics must remain hidden from the rest of the application.
/// </remarks>
public interface ILisConnection
{
    /// <summary>
    /// Gets the current state of the connection.
    /// </summary>
    LisConnectionState State { get; }

    int LisChannelId { get; }

    LisConnectionRole Role { get; }

    Task<bool> OpenAsync();

    void Close();

    /// <summary>
    /// Asynchronously sends a message to the open connection.
    /// </summary>
    /// <param name="messageType">The type of message.</param>
    /// <param name="messageData">The message data to send.</param>
    /// <returns>True if message was successfully sent, otherwise, false.</returns>
    Task<bool> SendMessageAsync(LisMessageType messageType, string messageData);

    /// <summary>
    /// Occurs when the state of the LIS connection has been changed.
    /// </summary>
    event EventHandler<LisConnectionStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Occurs after a message from LIS has been received.
    /// </summary>
    event EventHandler<TestOrderReceivedEventArgs>? TestOrderReceived;
}

public class TestOrderReceivedEventArgs
{
    public TestOrderReceivedEventArgs(ILisMessage receivedMessage)
    {
        ReceivedMessage = receivedMessage;
    }

    /// <summary>
    /// The list of patients with samples and test orders. See remarks for more information.
    /// </summary>
    /// <remarks>
    /// Every <see cref="LisPatient"/> object must represent a unique (for this message) patient.
    /// Every patient must have be linked with, at least, one sample in the <see cref="LisSample"/> collection.
    /// Anonymous patients must not be added here.
    /// </remarks>
    public IList<LisPatient> Patients { get; } = [];

    public ILisMessage ReceivedMessage { get; }

    /// <summary>
    /// Gets the metadata associated with the received message. This can include information such as message control ID and other relevant details.
    /// </summary>
    public Dictionary<string, string> Metadata { get; } = new Dictionary<string, string>();
}
