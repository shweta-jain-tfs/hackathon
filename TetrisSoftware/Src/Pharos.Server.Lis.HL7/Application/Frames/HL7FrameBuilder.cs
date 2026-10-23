using Pharos.Server.Lis.HL7.Application.Common;
using Pharos.Server.Lis.HL7.Application.Interfaces;

namespace Pharos.Server.Lis.HL7.Application.Frames;

internal sealed class HL7FrameBuilder : IHL7FrameBuilder
{
    private const int _numberOfControlCharsInFrame = 3;

    public byte[] CreateFrameFromMessage(byte[] messageData)
    {
        var frame = new byte[messageData.Length + _numberOfControlCharsInFrame];

        var stream = new MemoryStream(frame, true);
        stream.WriteByte(HL7ControlSymbols.StartBlock);
        stream.Write(messageData, 0, messageData.Length);
        stream.WriteByte(HL7ControlSymbols.EndBlock);
        stream.WriteByte(HL7ControlSymbols.CarriageReturn);

        return frame;
    }

    public byte[] CreateMessageFromFrame(IReadOnlyList<byte> frameData)
    {
        ValidateFrame(frameData);

        var messageData = new byte[frameData.Count - _numberOfControlCharsInFrame];
        for (var i = 0; i < messageData.Length; i++)
        {
            messageData[i] = frameData[i + 1];
        }

        return messageData;
    }

    private static void ValidateFrame(IReadOnlyList<byte> frameData)
    {
        var frameLength = frameData.Count;

        if (frameLength < _numberOfControlCharsInFrame)
        {
            throw new HL7Exception("HL7 frame is incomplete.");
        }
        if (frameData[0] != HL7ControlSymbols.StartBlock)
        {
            throw new HL7Exception("HL7 frame does not start with the start block character.");
        }

        var ebByte = frameData[frameLength - 2];
        if (ebByte != HL7ControlSymbols.EndBlock)
        {
            throw new HL7Exception("HL7 frame does not end with the end block character.");
        }

        var crByte = frameData[frameLength - 1];
        if (crByte != HL7ControlSymbols.CarriageReturn)
        {
            throw new HL7Exception("HL7 frame does not end with a carriage return after the end block character.");
        }
    }
}
