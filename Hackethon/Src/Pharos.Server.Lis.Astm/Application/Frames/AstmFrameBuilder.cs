namespace Pharos.Server.Lis.Astm.Application.Frames;

internal sealed class AstmFrameBuilder : IAstmFrameBuilder
{
    private const int _maxFrameSequenceNumber = 8;
    private readonly bool _useOnlyEtx;
    private readonly bool _etxOnlySplitLargeRecords;
    private readonly bool _etxOnlyNormalFrameBlockSize;
    private readonly IFrameChecksumCalculator _checksumCalculator;

    public AstmFrameBuilder(bool useOnlyEtx, bool etxOnlySplitLargeRecords, bool etxOnlyNormalFrameBlockSize, IFrameChecksumCalculator checksumCalculator)
    {
        _useOnlyEtx = useOnlyEtx;
        _etxOnlySplitLargeRecords = etxOnlySplitLargeRecords;
        _etxOnlyNormalFrameBlockSize = etxOnlyNormalFrameBlockSize;
        _checksumCalculator = checksumCalculator;
    }

    public IAstmFrame CreateFrameFromBytes(byte[] input, int expectedFrameNumber)
    {
        var stream = new MemoryStream(input, false);

        ValidateFrameStartSymbol(stream.ReadByte());
        ValidateFrameNumber(stream.ReadByte(), expectedFrameNumber);

        var data = new List<byte>();
        var currentByte = stream.ReadByte();

        while (!AstmControlSymbols.IsFrameTerminator(currentByte))
        {
            if (currentByte == -1)
            {
                throw new AstmException("ASTM frame terminator not found");
            }

            ValidateFrameDataByte(currentByte, data.Count);
            data.Add((byte)currentByte);
            currentByte = stream.ReadByte();
        }

        var result = new AstmFrame(expectedFrameNumber, data.ToArray(), currentByte == AstmControlSymbols.ETB, _checksumCalculator);

        var (calc0, calc1) = result.GetChecksum();
        var cs0 = stream.ReadByte();
        var cs1 = stream.ReadByte();

        if (calc0 != cs0 || calc1 != cs1)
        {
            throw new AstmException($"ASTM frame checksum mismatch. FrameNumber={expectedFrameNumber}, FrameTerminator=0x{currentByte:X2}, CalculatedChecksum=0x{calc0:X2}0x{calc1:X2}, ReceivedChecksum=0x{cs0:X2}0x{cs1:X2}");
        }
        var crByte = stream.ReadByte();
        var lfByte = stream.ReadByte();
        ValidateFrameEnding(crByte, lfByte);
        return result;
    }

    public IEnumerable<IAstmFrame> CreateFramesFromMessage(byte[] messageData)
    {
        var frameNumber = 1;
        var bytesSent = 0;

        while (bytesSent < messageData.Length)
        {
            var bytesRemaining = messageData.Length - bytesSent;
            var bytesToRead = CalculateFrameDataLength(messageData, bytesSent, bytesRemaining);

            var frameData = new byte[bytesToRead];
            Array.Copy(messageData, bytesSent, frameData, 0, bytesToRead);

            var isIntermediateFrame = ShouldUseIntermediateFrame(bytesSent + bytesToRead, messageData.Length);

            yield return new AstmFrame((byte)frameNumber, frameData, isIntermediateFrame, _checksumCalculator);

            frameNumber = (frameNumber + 1) % _maxFrameSequenceNumber;
            bytesSent += bytesToRead;
        }
    }

    private static void ValidateFrameStartSymbol(int startByte)
    {
        if (startByte != AstmControlSymbols.STX)
        {
            throw new AstmException("Frame does not start with STX symbol.");
        }
    }

    private static void ValidateFrameNumber(int frameNumberByte, int expectedFrameNumber)
    {
        var actualFrameNumber = frameNumberByte - AstmFrame.AsciiTableDigitsOffset;

        if (expectedFrameNumber != actualFrameNumber)
        {
            throw new AstmException("Unexpected frame number");
        }
    }

    private void ValidateFrameDataByte(int currentByte, int currentDataLength)
    {
        if (currentDataLength >= AstmFrame.MaxFrameDataLength && EnsureMaxFrameLengthLimit())
        {
            throw new AstmException("Unexpected symbol in the message data.");
        }

        if (AstmControlSymbols.IsControlSymbol((byte)currentByte))
        {
            throw new AstmException("The message contains illegal characters.");
        }
    }

    private static void ValidateFrameEnding(int crByte, int lfByte)
    {
        if (crByte != AstmControlSymbols.CR || lfByte != AstmControlSymbols.LF)
        {
            throw new AstmException($"Unexpected frame ending. CR=0x{crByte:X2}, LF=0x{lfByte:X2}");
        }
    }

    private int CalculateFrameDataLength(byte[] messageData, int currentPosition, int bytesRemaining)
    {
        var maxLength = Math.Min(bytesRemaining, AstmFrame.MaxFrameDataLength);

        if (IsEtxOnlyModeWithNoNormalFrameBlockSize())
        {
            return CalculateLengthForEtxOnlyMode(messageData, currentPosition, bytesRemaining);
        }

        return maxLength;
    }

    private int CalculateLengthForEtxOnlyMode(byte[] messageData, int currentPosition, int bytesRemaining)
    {
        var nextCrIndex = Array.IndexOf(messageData, AstmControlSymbols.CR, currentPosition + 1);
        var distanceToNextCr = nextCrIndex < 0 ? bytesRemaining : (nextCrIndex - currentPosition + 1);

        if (EnsureMaxFrameLengthLimit())
        {
            return Math.Min(distanceToNextCr, AstmFrame.MaxFrameDataLength);
        }

        return distanceToNextCr;
    }

    private bool EnsureMaxFrameLengthLimit()
    {
        // With SettingCommunicationETXOnlySplitLargeRecords set to false there might be frames larger than 
        // what spec allows, but we follow how Prime works.
        return !IsEtxOnlyModeWithNoNormalFrameBlockSize() || _etxOnlySplitLargeRecords;
    }

    private bool IsEtxOnlyModeWithNoNormalFrameBlockSize()
    {
        return _useOnlyEtx && !_etxOnlyNormalFrameBlockSize;
    }

    private bool ShouldUseIntermediateFrame(int bytesSent, int totalLength)
    {
        return bytesSent < totalLength && !_useOnlyEtx;
    }
}
