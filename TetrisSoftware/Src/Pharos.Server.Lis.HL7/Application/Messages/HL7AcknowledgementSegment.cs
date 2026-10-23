using Pharos.Server.Lis.HL7.Application.Interfaces;

namespace Pharos.Server.Lis.HL7.Application.Messages;

public enum HL7AcknowledgementCode
{
    MessageAccept, // AA 
    MessageError, // AE 
    MessageReject // AR
}

internal sealed record HL7AcknowledgementSegment(
    HL7AcknowledgementCode AcknowledgementCode,
    string MessageControlId) : IHL7Segment
{
    public string SegmentType => HL7SegmentType.MessageAcknowledgement;

    public int SequenceNumber { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public static HL7AcknowledgementSegment Parse(string segmentString, HL7FieldDelimiters delimiters)
    {
        var parser = new HL7SegmentParser(segmentString, delimiters);

        return new HL7AcknowledgementSegment
        (
            parser.Field(AcknowledgementSegmentField.AcknowledgementCode).Component(0) switch
            {
                "AA" => HL7AcknowledgementCode.MessageAccept,
                "AE" => HL7AcknowledgementCode.MessageError,
                "AR" => HL7AcknowledgementCode.MessageReject,
                _ => HL7AcknowledgementCode.MessageAccept
            },
            parser.Field(AcknowledgementSegmentField.MessageControlId).Component(0)
        );
    }

    public string ToSegmentString(HL7FieldDelimiters delimiters, bool redactSensitiveData = false)
    {
        var builder = new HL7SegmentBuilder<AcknowledgementSegmentField>(delimiters);

        builder.AddField(AcknowledgementSegmentField.SegmentTypeId, SegmentType);
        builder.AddField(AcknowledgementSegmentField.AcknowledgementCode, AcknowledgementCode switch
        {
            HL7AcknowledgementCode.MessageAccept => "AA",
            HL7AcknowledgementCode.MessageError => "AE",
            HL7AcknowledgementCode.MessageReject => "AR",
            _ => ""
        });
        builder.AddField(AcknowledgementSegmentField.MessageControlId, MessageControlId);

        return builder.ToSegmentString();
    }

    private enum AcknowledgementSegmentField
    {
        SegmentTypeId,
        AcknowledgementCode,
        MessageControlId
    }
}
