namespace Pharos.Server.Lis.Astm.Application.Interfaces;

/// <summary>
/// Defines an algorithm for an ASTM frame checksum calculation.
/// </summary>
public interface IFrameChecksumCalculator
{
    /// <summary>
    /// Calculates the fram checksum.
    /// </summary>
    /// <param name="messageData"></param>
    /// <param name="frameNumber"></param>
    /// <param name="frameTerminator"></param>
    /// <returns>A two-byte tuple containing the frame checksum.</returns>
    /// <remarks>
    /// The checksum is encoded as 2 characters. The checksum is computed by adding
    /// the binary values of the characters (modulo 256), keeping least significant
    /// 8 bits of the result. The 8 bits can be considered as 2 groups of 4 bits which
    /// are converted to ASCII and represented in hexadecimal format.
    /// The[STX] character initializes the checksum to zero.The first character used
    /// in the checksum is the frame number.The last character used is the[ETB] or [ETX].
    ///
    /// Note! The checksum is calculated as IDM (only using the lowest byte of 2 byte characters).
    /// This can be altered using the setting: Use IDM checksum calculation.
    /// </remarks>
    (byte, byte) CalculateChecksum(byte[] messageData, int frameNumber, byte frameTerminator);
}
