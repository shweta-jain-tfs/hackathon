namespace Pharos.Server.Lis.Astm.Application.Interfaces;

/// <summary>
/// Defines an ASTM-protocol data frame.
/// </summary>
public interface IAstmFrame
{
    /// <summary>
    /// The maximum size (in bytes) of an ASTM frame.
    /// </summary>
    public const int MaxFrameLength = 247;

    /// <summary>
    /// The frame sequence number
    /// </summary>
    int FrameNumber { get; }

    /// <summary>
    /// Gets the frame payload.
    /// </summary>
    byte[] MessageData { get; }

    /// <summary>
    /// Gets a sequence of bytes that represents a properly formatted ASTM data frame
    /// which is ready to send.
    /// </summary>
    /// <returns>A byte array containing the formatted ASTM frame.</returns>
    byte[] GetBytes();
}