namespace Pharos.Server.Lis.Astm.Application.Interfaces;

/// <summary>
/// Defines a ASTM frame builder.
/// </summary>
internal interface IAstmFrameBuilder
{
    /// <summary>
    /// Splits a message into a collection of frames according to ASTM protocol specification.
    /// </summary>
    /// <param name="messageData"></param>
    /// <returns>A collection of <see cref="IAstmFrame"/> objects.</returns>
    IEnumerable<IAstmFrame> CreateFramesFromMessage(byte[] messageData);

    /// <summary>
    /// Creates a frame from raw data received from LIS.
    /// </summary>
    /// <param name="input">The raw data received from LIS.</param>
    /// <param name="expectedFrameNumber">The expected number of the frame (0-7).</param>
    /// <returns>An object of the <see cref="IAstmFrame"/> type.</returns>
    /// <exception cref="AstmException">
    /// The input cannot be parsed.
    /// </exception>
    IAstmFrame CreateFrameFromBytes(byte[] input, int expectedFrameNumber);
}
