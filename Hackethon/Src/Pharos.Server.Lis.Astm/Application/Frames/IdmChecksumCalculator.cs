using System.Globalization;
using System.Text;

namespace Pharos.Server.Lis.Astm.Application.Frames;

public class IdmChecksumCalculator : IFrameChecksumCalculator
{
    private readonly Encoding _encoding;

    public IdmChecksumCalculator(Encoding encoding)
    {
        _encoding = encoding;
    }

    public (byte, byte) CalculateChecksum(byte[] messageData, int frameNumber, byte frameTerminator)
    {
        // code taken from Prime, since framenumber and termination character is added before 
        // deconstructing back to characters it is not the same characters as in the original string 
        // that is considered when a encoding like unicode is used. And this do not follow the Astm specification.

        var frame = new List<byte>(capacity: messageData.Length + 2)
        {
            (byte)(frameNumber + AstmFrame.AsciiTableDigitsOffset)
        };
        frame.AddRange(messageData);
        frame.Add(frameTerminator);

        var checkSum = ComputeChecksum(frame.ToArray());
        var checksumBytes = FormatChecksumAsHex(checkSum);

        return (checksumBytes[0], checksumBytes[1]);
    }

    private int ComputeChecksum(byte[] frameData)
    {
        var characters = _encoding.GetString(frameData);
        var checksum = 0;

        foreach (var character in characters)
        {
            var bytes = _encoding.GetBytes([character]);
            // IDM method: use only the lowest byte (last byte for multi-byte encodings)
            var byteValue = bytes.Length > 1 ? bytes[1] : bytes[0];
            checksum = (checksum + byteValue) & 0xFF;
        }

        return checksum;
    }
    private static byte[] FormatChecksumAsHex(int checkSum)
    {
        return Encoding.ASCII.GetBytes(checkSum.ToString("X2", CultureInfo.InvariantCulture));
    }
}