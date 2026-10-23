using System.Globalization;
using System.Text;

namespace Pharos.Server.Lis.Astm.Application.Frames;

public class StandardChecksumCalculator : IFrameChecksumCalculator
{
    public (byte, byte) CalculateChecksum(byte[] messageData, int frameNumber, byte frameTerminator)
    {
        var checkSum = ComputeChecksum(messageData, frameNumber, frameTerminator);
        var checksumBytes = FormatChecksumAsHex(checkSum);
        return (checksumBytes[0], checksumBytes[1]);
    }

    private static int ComputeChecksum(byte[] messageData, int frameNumber, byte frameTerminator)
    {
        var checkSum = frameNumber + AstmFrame.AsciiTableDigitsOffset;

        foreach (var dataByte in messageData)
        {
            checkSum = (checkSum + dataByte) & 0xFF;
        }

        checkSum = (checkSum + frameTerminator) & 0xFF;
        return checkSum;
    }

    private static byte[] FormatChecksumAsHex(int checkSum)
    {
        return Encoding.ASCII.GetBytes(checkSum.ToString("X2", CultureInfo.InvariantCulture));
    }
}
