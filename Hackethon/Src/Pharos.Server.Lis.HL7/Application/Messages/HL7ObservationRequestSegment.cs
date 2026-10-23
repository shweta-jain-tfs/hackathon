using System.Globalization;
using Pharos.Server.Lis.HL7.Application.Interfaces;

namespace Pharos.Server.Lis.HL7.Application.Messages;

internal enum HL7SpecimenActionCode
{
    undefined, //
    Add, // A
    New // N
}

internal enum HL7SpecimenType
{
    Serum, // SER
    QualityControl // QC3
}

internal sealed class HL7ObservationRequestSegment : IHL7Segment
{
    public string SegmentType => HL7SegmentType.ObservationRequest;
    public int SequenceNumber { get; set; } = 1;
    public string PlacerOrderNumber { get; set; } = "";
    public string UniversalServiceIdentifier { get; set; } = "";
    public DateTimeOffset? ObservationDateTime { get; set; }
    public HL7SpecimenActionCode SpecimenActionCode { get; set; }
    public HL7SpecimenType SpecimenSourceCode { get; set; }

    public static HL7ObservationRequestSegment Parse(string segmentString, HL7FieldDelimiters delimiters)
    {
        var parser = new HL7SegmentParser(segmentString, delimiters);

        var segment = new HL7ObservationRequestSegment
        {
            SequenceNumber = parser.Field(ObservationRequestSegmentField.SetId).ShortOrZero(0),
            PlacerOrderNumber = parser.Field(ObservationRequestSegmentField.PlacerOrderNumber).Component(0),
            UniversalServiceIdentifier = parser.Field(ObservationRequestSegmentField.UniversalServiceIdentifier).Component(0),
            ObservationDateTime = parser.Field(ObservationRequestSegmentField.ObservationDateTime).AsDateAndTime(),
            SpecimenActionCode = parser.FieldPart(ObservationRequestSegmentField.SpecimenActionCode, 0) switch
            {
                "A" => HL7SpecimenActionCode.Add,
                "N" => HL7SpecimenActionCode.New,
                _ => HL7SpecimenActionCode.undefined,
            },

            SpecimenSourceCode = parser.Field(ObservationRequestSegmentField.SpecimenSourceCode).Component(0) switch
            {
                "QC3" => HL7SpecimenType.QualityControl,
                _ => HL7SpecimenType.Serum
            }
        };

        return segment;
    }

    public string ToSegmentString(HL7FieldDelimiters delimiters, bool redactSensitiveData = false)
    {
        var builder = new HL7SegmentBuilder<ObservationRequestSegmentField>(delimiters);

        builder.AddField(ObservationRequestSegmentField.SegmentTypeId, SegmentType);
        builder.AddField(ObservationRequestSegmentField.SetId, SequenceNumber.ToString(CultureInfo.InvariantCulture));
        builder.AddField(ObservationRequestSegmentField.PlacerOrderNumber, PlacerOrderNumber);

        builder.AddField(ObservationRequestSegmentField.UniversalServiceIdentifier, UniversalServiceIdentifier);

        builder.AddField(ObservationRequestSegmentField.ObservationDateTime, ObservationDateTime?.ToString(HL7FieldParser.DateTimeFormat, CultureInfo.InvariantCulture) ?? "");
        builder.AddField(ObservationRequestSegmentField.SpecimenActionCode, "");
        builder.AddField(ObservationRequestSegmentField.SpecimenSourceCode, SpecimenSourceCode switch
        {
            HL7SpecimenType.QualityControl => "QC3",
            _ => "SER"
        });

        return builder.ToSegmentString();
    }

    private enum ObservationRequestSegmentField
    {
        SegmentTypeId,
        SetId,
        PlacerOrderNumber,
        FillerOrderNumber,
        UniversalServiceIdentifier,
        Priority,
        RequestedDateTime,
        ObservationDateTime,
        ObservationEndDateTime,
        CollectionVolume,
        CollectorIdentifier,
        SpecimenActionCode,
        DangerCode,
        RelevantClinicalInformation,
        SpecimenReceivedDateTime,
        SpecimenSourceCode,
        OrderingProvider,
        OrderCallbackPhoneNumber,
        PlacerField1,
        PlacerField2,
        FillerField1,
        FillerField2,
        ResultsStatusChangeDateTime,
        ChargeToPractice,
        DiagnosticServSectId,
        ResultStatus,
        ParentResult,
        QuantityTiming
    }
}