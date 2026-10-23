using Pharos.Server.Lis.Application.Interfaces;

namespace Pharos.Server.Lis.HL7.Application.Interfaces;

/// <summary>
/// Defines a "logical" connection to LIS under HL7 protocol.
/// </summary>
internal interface IHL7DataLink
{
    /// <summary>
    /// Initializes the data link using a specified transport connection.
    /// </summary>
    /// <param name="connection"></param>
    void Initialize(ILisTransportConnection connection);

    /// <summary>
    /// Asynchronously sends an HL7 message.
    /// </summary>
    /// <param name="messageData"></param>
    /// <returns>True if message was successfully sent, otherwise, false.</returns>
    Task<bool> SendMessageAsync(string messageData);

    /// <summary>
    /// Occurs after an HL7 message from LIS has been received.
    /// </summary>
    event EventHandler<IHL7Message>? MessageReceived;
}
