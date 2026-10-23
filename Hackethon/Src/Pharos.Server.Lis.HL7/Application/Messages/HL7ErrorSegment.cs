using Pharos.Server.Lis.HL7.Application.Interfaces;

namespace Pharos.Server.Lis.HL7.Application.Messages;

internal sealed record HL7ErrorSegment(string ErrorMessage) : IHL7Segment
{
    public string SegmentType => HL7SegmentType.Error;

    public int SequenceNumber { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public static HL7ErrorSegment Parse(string segmentString, HL7FieldDelimiters delimiters)
    {
        var parser = new HL7SegmentParser(segmentString, delimiters);

        return new HL7ErrorSegment
        (
            parser.Field(ErrorSegmentField.ErrorMessage).Component(0)
        );
    }

    public string ToSegmentString(HL7FieldDelimiters delimiters, bool redactSensitiveData = false)
    {
        var builder = new HL7SegmentBuilder<ErrorSegmentField>(delimiters);

        builder.AddField(ErrorSegmentField.SegmentTypeId, SegmentType);
        builder.AddField(ErrorSegmentField.ErrorMessage, ErrorMessage);

        return builder.ToSegmentString();
    }

    private enum ErrorSegmentField
    {
        SegmentTypeId,
        ErrorMessage
    }
}
