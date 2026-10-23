using System.Globalization;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Lis.HL7.Application.Interfaces;

namespace Pharos.Server.Lis.HL7.Application.Messages;

internal enum HL7ObservationResultStatus
{
    Final, // F
    Pending, // I
    Canceled, // X
    FinalWithManualTests // M
}

internal sealed class HL7AdditionalTestInformationSegment : IHL7Segment
{
    public string SegmentType => HL7SegmentType.AdditionalTestInformation;
    public int SequenceNumber { get; set; } = 1;
    public string ObservationId { get; set; } = "";
    public string ObservationText { get; set; } = "";
    public string ObservationCodingSystem { get; set; } = "";
    public string ObservationValue { get; set; } = "";
    public string Units { get; set; } = "";
    public HL7ObservationResultStatus ResultStatus { get; set; }
    public DateTimeOffset? ObservationDateTime { get; set; }
    public string SampleId { get; set; } = "";
    public List<SymptomType>? Symptoms { get; set; }

    public static HL7AdditionalTestInformationSegment Parse(string segmentString, HL7FieldDelimiters delimiters)
    {
        var parser = new HL7SegmentParser(segmentString, delimiters);

        List<SymptomType>? symptoms = null;
        var sampleId = "";
        var observationId = parser.FieldPart(AdditionalTestInformationField.ObservationId, 0);
        if (observationId == "SY")
        {
            symptoms = parser.Field(AdditionalTestInformationField.ObservationValue).AsShortList().Select(s => (SymptomType)s).ToList();
        }
        else
        {
            sampleId = observationId;
        }

        var segment = new HL7AdditionalTestInformationSegment
        {
            SequenceNumber = parser.Field(AdditionalTestInformationField.SetId).ShortOrZero(0),
            ObservationId = observationId,
            ObservationText = parser.FieldPart(AdditionalTestInformationField.ObservationId, 1),
            ObservationCodingSystem = parser.FieldPart(AdditionalTestInformationField.ObservationId, 2),
            ObservationValue = parser.Field(AdditionalTestInformationField.ObservationValue).Component(0),
            Symptoms = symptoms,
            SampleId = sampleId
        };

        return segment;
    }

    public string ToSegmentString(HL7FieldDelimiters delimiters, bool redactSensitiveData = false)
    {
        var builder = new HL7SegmentBuilder<AdditionalTestInformationField>(delimiters);

        builder.AddField(AdditionalTestInformationField.SegmentTypeId, SegmentType);
        builder.AddField(AdditionalTestInformationField.SetId, SequenceNumber.ToString(CultureInfo.InvariantCulture));
        builder.AddField(AdditionalTestInformationField.ValueType, "ST");
        builder.AddField(AdditionalTestInformationField.ObservationId, ObservationId, ObservationText, ObservationCodingSystem);
        builder.AddField(AdditionalTestInformationField.ObservationValue, ObservationValue);
        builder.AddField(AdditionalTestInformationField.Units, Units);
        builder.AddField(AdditionalTestInformationField.ObservationResultStatus, MapResultStatus(ResultStatus));
        builder.AddField(AdditionalTestInformationField.ObservationDateTime, ObservationDateTime?.ToString(HL7FieldParser.DateTimeFormat, CultureInfo.InvariantCulture) ?? "");

        return builder.ToSegmentString();
    }

    private static string MapResultStatus(HL7ObservationResultStatus resultStatus)
    {
        return resultStatus switch
        {
            HL7ObservationResultStatus.Final => "F",
            HL7ObservationResultStatus.Pending => "I",
            HL7ObservationResultStatus.Canceled => "X",
            HL7ObservationResultStatus.FinalWithManualTests => "M",
            _ => ""
        };
    }

    private enum AdditionalTestInformationField
    {
        SegmentTypeId,
        SetId,
        ValueType,
        ObservationId,
        ObservationSubId,
        ObservationValue,
        Units,
        ReferencesRange,
        AbnormalFlags,
        Probability,
        NatureOfAbnormalTest,
        ObservationResultStatus,
        LastObservedNormalValuesDate,
        UserDefinedAccessChecks,
        ObservationDateTime
    }
}
