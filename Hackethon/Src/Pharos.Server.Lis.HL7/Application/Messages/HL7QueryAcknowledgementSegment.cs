using Pharos.Server.Lis.HL7.Application.Interfaces;

namespace Pharos.Server.Lis.HL7.Application.Messages;

internal enum HL7QueryResponseStatus
{
    DataFound, //OK
    NoDataFound, //NF
    ApplicationError, //AE
    ApplicationReject //AR
}

internal sealed class HL7QueryAcknowledgementSegment : IHL7Segment
{
    public string SegmentType => HL7SegmentType.QueryAcknowledgement;

    public int SequenceNumber { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public string QueryTag { get; set; } = "";

    public HL7QueryResponseStatus QueryResponseStatus { get; set; }

    public string MessageQueryNameIdentifier { get; set; } = HL7MessageType.Zos;

    public string MessageQueryNameText { get; set; } = "Lab Order Specimen Query";

    public int HitCount { get; set; }

    public static HL7QueryAcknowledgementSegment Parse(string segmentString, HL7FieldDelimiters delimiters)
    {
        var parser = new HL7SegmentParser(segmentString, delimiters);

        return new HL7QueryAcknowledgementSegment
        {
            QueryTag = parser.Field(QueryAcknowledgementSegmentField.QueryTag).Component(0),
            QueryResponseStatus = parser.Field(QueryAcknowledgementSegmentField.QueryResponseStatus).Component(0) switch
            {
                "NF" => HL7QueryResponseStatus.NoDataFound,
                "AE" => HL7QueryResponseStatus.ApplicationError,
                "AR" => HL7QueryResponseStatus.ApplicationReject,
                _ => HL7QueryResponseStatus.DataFound
            },
            MessageQueryNameIdentifier = parser.Field(QueryAcknowledgementSegmentField.MessageQueryName).Component(0),
            MessageQueryNameText = parser.Field(QueryAcknowledgementSegmentField.MessageQueryName).Component(1),
            HitCount = parser.Field(QueryAcknowledgementSegmentField.HitCount).IntOrZero(0)
        };
    }

    public string ToSegmentString(HL7FieldDelimiters delimiters, bool redactSensitiveData = false)
    {
        var builder = new HL7SegmentBuilder<QueryAcknowledgementSegmentField>(delimiters);

        builder.AddField(QueryAcknowledgementSegmentField.SegmentTypeId, SegmentType);
        builder.AddField(QueryAcknowledgementSegmentField.QueryTag, QueryTag);
        builder.AddField(QueryAcknowledgementSegmentField.QueryResponseStatus, QueryResponseStatus switch
        {
            HL7QueryResponseStatus.NoDataFound => "NF",
            HL7QueryResponseStatus.ApplicationError => "AE",
            HL7QueryResponseStatus.ApplicationReject => "AR",
            _ => "OK"
        });
        builder.AddField(QueryAcknowledgementSegmentField.MessageQueryName, MessageQueryNameIdentifier, MessageQueryNameText);
        builder.AddField(QueryAcknowledgementSegmentField.HitCount, HitCount.ToString());

        return builder.ToSegmentString();
    }

    private enum QueryAcknowledgementSegmentField
    {
        SegmentTypeId,
        QueryTag,
        QueryResponseStatus,
        MessageQueryName,
        HitCount
    }
}
