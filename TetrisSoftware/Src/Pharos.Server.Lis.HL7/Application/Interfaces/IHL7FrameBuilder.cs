namespace Pharos.Server.Lis.HL7.Application.Interfaces;

/// <summary>
/// Defines an HL7 MLLP frame builder.
/// </summary>
internal interface IHL7FrameBuilder
{
    /// <summary>
    /// Wraps an HL7 message payload in an MLLP frame.
    /// </summary>
    /// <param name="messageData">The encoded HL7 message bytes.</param>
    /// <returns>The bytes to send over the transport connection.</returns>
    byte[] CreateFrameFromMessage(byte[] messageData);

    /// <summary>
    /// Extracts an HL7 message payload from an MLLP frame.
    /// </summary>
    /// <param name="frameData">The raw frame bytes received from LIS.</param>
    /// <returns>The encoded HL7 message bytes.</returns>
    byte[] CreateMessageFromFrame(IReadOnlyList<byte> frameData);
}
