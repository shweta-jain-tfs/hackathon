using System.Globalization;
using Pharos.Server.Lis.HL7.Application.Common;
using Pharos.Server.Lis.HL7.Application.Interfaces;

namespace Pharos.Server.Lis.HL7.Application.Messages;

internal enum HL7ProcessingId
{
    Debugging,
    Production,
    Testing
}

internal sealed record HL7HeaderSegment(
    HL7FieldDelimiters Delimiters,
    string SendingApplication,
    DateTimeOffset? DateAndTimeOfMessage,
    string MessageType1,
    string MessageType2,
    string MessageControlId,
    HL7ProcessingId ProcessingId) : IHL7Segment
{
    private const string _hl7Version = "2.5";

    public string SegmentType => HL7SegmentType.MessageHeader;

    public int SequenceNumber { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public static HL7HeaderSegment Parse(string segmentString)
    {
        if (segmentString.Length < 8 || HL7SegmentParser.GetRecordType(segmentString) != HL7SegmentType.MessageHeader)
        {
            throw new HL7Exception("The segment is not an HL7 message header.");
        }

        var delimiters = HL7FieldDelimiters.Parse(segmentString);
        var parser = new HL7SegmentParser(segmentString, delimiters);

        var segment = new HL7HeaderSegment
        (
            delimiters,
            parser.Field(HeaderSegmentField.SendingApplication).Component(0),
            parser.Field(HeaderSegmentField.DateAndTimeOfMessage).AsDateAndTime(),
            parser.FieldPart(HeaderSegmentField.MessageType, 0),
            parser.FieldPart(HeaderSegmentField.MessageType, 1),
            parser.Field(HeaderSegmentField.MessageControlId).Component(0),
            parser.Field(HeaderSegmentField.ProcessingId).Component(0) switch
            {
                "P" => HL7ProcessingId.Production,
                "T" => HL7ProcessingId.Testing,
                _ => HL7ProcessingId.Debugging
            }
        );

        return segment;
    }

    public string ToSegmentString(HL7FieldDelimiters delimiters, bool redactSensitiveData = false)
    {
        return ToString();
    }

    public override string ToString()
    {
        var builder = new HL7SegmentBuilder<HeaderSegmentField>(Delimiters);

        builder.AddField(HeaderSegmentField.SegmentTypeId, SegmentType);
        builder.AddField(HeaderSegmentField.EncodingCharacters, new DelimiterSegmentField());
        builder.AddField(HeaderSegmentField.SendingApplication, SendingApplication);
        builder.AddField(HeaderSegmentField.DateAndTimeOfMessage, DateAndTimeOfMessage?.ToString(HL7FieldParser.DateTimeFormat, CultureInfo.InvariantCulture) ?? "");
        builder.AddField(HeaderSegmentField.MessageType, MessageType1, MessageType2);
        builder.AddField(HeaderSegmentField.MessageControlId, MessageControlId);
        builder.AddField(HeaderSegmentField.ProcessingId, ProcessingId switch
        {
            HL7ProcessingId.Production => "P",
            HL7ProcessingId.Testing => "T",
            _ => "D"
        });
        builder.AddField(HeaderSegmentField.VersionId, _hl7Version);
        builder.AddField(HeaderSegmentField.AcceptAcknowledgementType, "AL");

        return builder.ToSegmentString();
    }

    private enum HeaderSegmentField
    {
        SegmentTypeId,
        EncodingCharacters,
        SendingApplication,
        SendingFacility,
        ReceivingApplication,
        ReceivingFacility,
        DateAndTimeOfMessage,
        Security,
        MessageType,
        MessageControlId,
        ProcessingId,
        VersionId,
        SequenceNumber,
        ContinuationPointer,
        AcceptAcknowledgementType,
        ApplicationAcknowledgmentType,
        CountryCode
    }
}
