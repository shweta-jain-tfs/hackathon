namespace Pharos.Server.Lis.Astm.Application.Interfaces;

/// <summary>
/// Defines a "logical" connection to LIS under ASTM protocol.
/// </summary>
internal interface IAstmDataLink
{
    /// <summary>
    /// Initializes the data link using a specified transport connection.
    /// </summary>
    /// <param name="connection"></param>
    void Initialize(ILisTransportConnection connection);

    /// <summary>
    /// Asynchronously sends an ASTM message.
    /// </summary>
    /// <param name="messageData"></param>
    /// <returns>True if message was successfully sent, otherwise, false.</returns>
    Task<bool> SendMessageAsync(string messageData);

    /// <summary>
    /// Occurs after an ASTM message from LIS has been received.
    /// </summary>
    event EventHandler<IAstmMessage>? MessageReceived;
}
