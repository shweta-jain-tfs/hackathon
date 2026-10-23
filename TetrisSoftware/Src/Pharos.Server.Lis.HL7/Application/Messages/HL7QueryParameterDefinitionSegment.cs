using Pharos.Server.Lis.HL7.Application.Interfaces;

namespace Pharos.Server.Lis.HL7.Application.Messages;

internal sealed record HL7QueryParameterDefinitionSegment(
    string QueryTag,
    string? SampleId) : IHL7Segment
{
    public string SegmentType => HL7SegmentType.QueryParameterDefinition;

    public int SequenceNumber { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public string MessageQueryName { get; init; } = "ZOS";

    public string MessageQueryDescription { get; init; } = "Lab Order Specimen Query";

    public string UserParameters => string.IsNullOrWhiteSpace(SampleId) ? "ALL" : SampleId;

    public static HL7QueryParameterDefinitionSegment Parse(string segmentString, HL7FieldDelimiters delimiters)
    {
        var parser = new HL7SegmentParser(segmentString, delimiters);
        var messageQueryName = parser.Field(QueryParameterDefinitionSegmentField.MessageQueryName);
        var queryTag = parser.Field(QueryParameterDefinitionSegmentField.QueryTag).Component(0);
        var userParameters = parser.Field(QueryParameterDefinitionSegmentField.UserParameters).Component(0);

        return new HL7QueryParameterDefinitionSegment(queryTag, userParameters)
        {
            MessageQueryName = messageQueryName.Component(0),
            MessageQueryDescription = messageQueryName.Component(1)
        };
    }

    public string ToSegmentString(HL7FieldDelimiters delimiters, bool redactSensitiveData = false)
    {
        var builder = new HL7SegmentBuilder<QueryParameterDefinitionSegmentField>(delimiters);

        builder.AddField(QueryParameterDefinitionSegmentField.SegmentTypeId, SegmentType);
        builder.AddField(
            QueryParameterDefinitionSegmentField.MessageQueryName,
            MessageQueryName,
            MessageQueryDescription);
        builder.AddField(QueryParameterDefinitionSegmentField.QueryTag, QueryTag);
        builder.AddField(QueryParameterDefinitionSegmentField.UserParameters, UserParameters);

        return builder.ToSegmentString();
    }

    private enum QueryParameterDefinitionSegmentField
    {
        SegmentTypeId,
        MessageQueryName,
        QueryTag,
        UserParameters
    }
}
