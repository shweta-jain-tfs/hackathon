using Pharos.Server.Lis.HL7.Application.Interfaces;

namespace Pharos.Server.Lis.HL7.Application.Messages;

internal sealed record HL7ResponseControlParameterSegment() : IHL7Segment
{
    public string SegmentType => HL7SegmentType.ResponseControlParameter;

    public int SequenceNumber { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public string ToSegmentString(HL7FieldDelimiters delimiters, bool redactSensitiveData = false)
    {
        var builder = new HL7SegmentBuilder<ResponseControlParameterSegmentField>(delimiters);

        builder.AddField(ResponseControlParameterSegmentField.SegmentTypeId, SegmentType);
        builder.AddField(ResponseControlParameterSegmentField.QueryPriority, "I");
        builder.AddField(ResponseControlParameterSegmentField.ResponseModality, "R");

        return builder.ToSegmentString();
    }

    private enum ResponseControlParameterSegmentField
    {
        SegmentTypeId,
        QueryPriority,
        QuantityLimitedRequest,
        ResponseModality,
        ExecutionAndDeliveryTime,
        ModifyIndicator,
        SortByField,
        SegmentGroupInclusion
    }
}
