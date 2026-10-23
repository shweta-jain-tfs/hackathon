using System.Globalization;
using Pharos.Server.Lis.HL7.Application.Interfaces;

namespace Pharos.Server.Lis.HL7.Application.Messages;

internal sealed record HL7SpecimenContainerSegment(
    string SampleId,
    string TrayId,
    int TrayPosition) : IHL7Segment
{
    public string SegmentType => HL7SegmentType.SpecimenContainer;
    public int SequenceNumber { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public static HL7SpecimenContainerSegment Parse(string segmentString, HL7FieldDelimiters delimiters)
    {
        var parser = new HL7SegmentParser(segmentString, delimiters);

        var segment = new HL7SpecimenContainerSegment
        (
            parser.Field(HL7SpecimenContainerSegmentField.ContainerIdentifier).Component(0),
            parser.Field(HL7SpecimenContainerSegmentField.TrayIdentifier).Component(0),
            parser.Field(HL7SpecimenContainerSegmentField.PositionInTray).IntOrZero(0)
        );

        return segment;
    }

    public string ToSegmentString(HL7FieldDelimiters delimiters, bool redactSensitiveData = false)
    {
        var builder = new HL7SegmentBuilder<HL7SpecimenContainerSegmentField>(delimiters);

        builder.AddField(HL7SpecimenContainerSegmentField.SegmentTypeId, SegmentType);
        builder.AddField(HL7SpecimenContainerSegmentField.ContainerIdentifier, SampleId);
        builder.AddField(HL7SpecimenContainerSegmentField.TrayIdentifier, TrayId);
        builder.AddField(HL7SpecimenContainerSegmentField.PositionInTray, TrayPosition != 0 ? TrayPosition.ToString(CultureInfo.InvariantCulture) : "");

        return builder.ToSegmentString();
    }

    private enum HL7SpecimenContainerSegmentField
    {
        SegmentTypeId,
        ExternalAccessionIdentifier,
        AccessionIdentifier,
        ContainerIdentifier,
        PrimaryContainerIdentifier,
        EquipmentContainerIdentifier,
        SpecimenSource,
        RegistrationDateTime,
        ContainerStatus,
        CarrierType,
        CarrierIdentifier,
        PositionInCarrier,
        TrayType,
        TrayIdentifier,
        PositionInTray
    }
}
