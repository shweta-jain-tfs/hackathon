using System.Globalization;
using Pharos.Server.Lis.HL7.Application.Interfaces;

namespace Pharos.Server.Lis.HL7.Application.Messages;

internal sealed class HL7SpecimenSegment : IHL7Segment
{
    public string SegmentType => HL7SegmentType.Specimen;
    public int SequenceNumber { get; set; } = 1;
    public HL7SpecimenType SpecimenType { get; set; }
    public string ContainerType { get; set; } = "";

    public static HL7SpecimenSegment Parse(string segmentString, HL7FieldDelimiters delimiters)
    {
        var parser = new HL7SegmentParser(segmentString, delimiters);

        return new HL7SpecimenSegment
        {
            SequenceNumber = parser.Field(HL7SpecimenSegmentField.SetId).ShortOrZero(0),
            SpecimenType = parser.Field(HL7SpecimenSegmentField.SpecimenType).Component(0) switch
            {
                "QC3" => HL7SpecimenType.QualityControl,
                _ => HL7SpecimenType.Serum
            },
            ContainerType = parser.Field(HL7SpecimenSegmentField.ContainerType).Component(0)
        };
    }

    public string ToSegmentString(HL7FieldDelimiters delimiters, bool redactSensitiveData = false)
    {
        var builder = new HL7SegmentBuilder<HL7SpecimenSegmentField>(delimiters);

        builder.AddField(HL7SpecimenSegmentField.SegmentTypeId, SegmentType);
        builder.AddField(HL7SpecimenSegmentField.SetId, SequenceNumber.ToString(CultureInfo.InvariantCulture));
        builder.AddField(HL7SpecimenSegmentField.SpecimenType, SpecimenType == HL7SpecimenType.QualityControl ? "QC3" : "SER");
        builder.AddField(HL7SpecimenSegmentField.ContainerType, ContainerType);

        return builder.ToSegmentString();
    }

    private enum HL7SpecimenSegmentField
    {
        SegmentTypeId,
        SetId,
        SpecimenId,
        SpecimenParentIds,
        SpecimenType,
        SpecimenTypeModifier,
        SpecimenAdditives,
        SpecimenCollectionMethod,
        SpecimenSourceSite,
        SpecimenSourceSiteModifier,
        SpecimenCollectionSite,
        SpecimenRole,
        SpecimenCollectionAmount,
        GroupedSpecimenCount,
        SpecimenDescription,
        SpecimenHandlingCode,
        SpecimenRiskCode,
        SpecimenCollectionDateTime,
        SpecimenReceivedDateTime,
        SpecimenExpirationDateTime,
        SpecimenAvailability,
        SpecimenRejectReason,
        SpecimenQuality,
        SpecimenAppropriateness,
        SpecimenCondition,
        SpecimenCurrentQuantity,
        NumberOfSpecimenContainers,
        ContainerType
    }
}
