namespace Pharos.Server.Lis.Astm.Application.Frames;

internal sealed record AstmFrame(int FrameNumber, byte[] MessageData, bool IntermediateFrame, IFrameChecksumCalculator ChecksumCalculator) : IAstmFrame
{
    /// <summary>
    /// The maximum size (in bytes) of a data payload in an ASTM frame.
    /// </summary>
    public const int MaxFrameDataLength = 240;

    public const byte AsciiTableDigitsOffset = 0x30;

    private const int _numberOfControlCharsInFrame = IAstmFrame.MaxFrameLength - MaxFrameDataLength;

    private byte FrameTerminator => IntermediateFrame ? AstmControlSymbols.ETB : AstmControlSymbols.ETX;

    public byte[] GetBytes()
    {
        var result = new byte[MessageData.Length + _numberOfControlCharsInFrame];

        var stream = new MemoryStream(result, true);

        stream.WriteByte(AstmControlSymbols.STX);
        stream.WriteByte((byte)(FrameNumber + AsciiTableDigitsOffset));
        stream.Write(MessageData, 0, MessageData.Length);
        stream.WriteByte(FrameTerminator);
        var (cs0, cs1) = GetChecksum();
        stream.WriteByte(cs0);
        stream.WriteByte(cs1);
        stream.WriteByte(AstmControlSymbols.CR);
        stream.WriteByte(AstmControlSymbols.LF);

        return result;
    }

    public (byte, byte) GetChecksum()
    {
        return ChecksumCalculator.CalculateChecksum(MessageData, FrameNumber, FrameTerminator);
    }
}