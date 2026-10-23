using System.Globalization;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure.Logging;
using Pharos.Server.Lis.HL7.Application.Interfaces;

namespace Pharos.Server.Lis.HL7.Application.Messages;

internal sealed class HL7PatientIdentificationSegment : IHL7Segment
{
    public string SegmentType => HL7SegmentType.PatientIdentification;
    public int SequenceNumber { get; set; } = 1;
    public string PatientId { get; set; } = "";
    public string PatientListId { get; set; } = "";
    public string AlternatePatientId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string MiddleName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateOnly? BirthDate { get; set; }
    public Gender Gender { get; set; }
    public bool UseLegacyValueForGender { get; set; }
    public string StreetAddress { get; set; } = "";
    public string OtherAddress { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string Country { get; set; } = "";

    public static HL7PatientIdentificationSegment Parse(string segmentString, HL7FieldDelimiters delimiters)
    {
        var parser = new HL7SegmentParser(segmentString, delimiters);

        var segment = new HL7PatientIdentificationSegment
        {
            SequenceNumber = parser.Field(PatientIdentificationField.SetId).ShortOrZero(0),
            PatientId = parser.Field(PatientIdentificationField.PatientId).Component(0),
            PatientListId = parser.Field(PatientIdentificationField.PatientIdentifierListId).Component(0),
            AlternatePatientId = parser.Field(PatientIdentificationField.AlternatePatientId).Component(0),
            LastName = parser.FieldPart(PatientIdentificationField.PatientName, 0),
            FirstName = parser.FieldPart(PatientIdentificationField.PatientName, 1),
            MiddleName = parser.FieldPart(PatientIdentificationField.PatientName, 2),
            BirthDate = parser.Field(PatientIdentificationField.BirthDate).AsDateOnly(),
            Gender = parser.FieldPart(PatientIdentificationField.Gender, 0) switch
            {
                "M" => Gender.Male,
                "F" => Gender.Female,
                _ => Gender.Undefined
            },
            StreetAddress = parser.FieldPart(PatientIdentificationField.PatientAddress, 0),
            OtherAddress = parser.FieldPart(PatientIdentificationField.PatientAddress, 1),
            City = parser.FieldPart(PatientIdentificationField.PatientAddress, 2),
            State = parser.FieldPart(PatientIdentificationField.PatientAddress, 3),
            ZipCode = parser.FieldPart(PatientIdentificationField.PatientAddress, 4),
            Country = parser.FieldPart(PatientIdentificationField.PatientAddress, 5)
        };

        return segment;
    }

    public string ToSegmentString(HL7FieldDelimiters delimiters, bool redactSensitiveData = false)
    {
        var builder = new HL7SegmentBuilder<PatientIdentificationField>(delimiters);

        builder.AddField(PatientIdentificationField.SegmentTypeId, SegmentType);
        builder.AddField(PatientIdentificationField.SetId, SequenceNumber.ToString(CultureInfo.InvariantCulture));
        builder.AddField(PatientIdentificationField.PatientId, PatientId.Redact(redactSensitiveData));
        builder.AddField(PatientIdentificationField.PatientIdentifierListId, PatientListId.Redact(redactSensitiveData));
        builder.AddField(PatientIdentificationField.AlternatePatientId, AlternatePatientId.Redact(redactSensitiveData));
        builder.AddField(PatientIdentificationField.PatientName, LastName.Redact(redactSensitiveData), FirstName.Redact(redactSensitiveData), MiddleName.Redact(redactSensitiveData));
        builder.AddField(PatientIdentificationField.BirthDate, (BirthDate?.ToString(HL7FieldParser.DateFormat, CultureInfo.InvariantCulture) ?? "").Redact(redactSensitiveData));
        builder.AddField(PatientIdentificationField.Gender, Gender switch
        {
            Gender.Male => "M",
            Gender.Female => UseLegacyValueForGender ? "W" : "F",
            _ => "U"
        });
        builder.AddField(
            PatientIdentificationField.PatientAddress,
            StreetAddress.Redact(redactSensitiveData),
            OtherAddress.Redact(redactSensitiveData),
            City.Redact(redactSensitiveData),
            State.Redact(redactSensitiveData),
            ZipCode.Redact(redactSensitiveData),
            Country.Redact(redactSensitiveData));

        return builder.ToSegmentString();
    }

    private enum PatientIdentificationField
    {
        SegmentTypeId,
        SetId,
        PatientId,
        PatientIdentifierListId,
        AlternatePatientId,
        PatientName,
        MothersMaidenName,
        BirthDate,
        Gender,
        PatientAlias,
        Race,
        PatientAddress
    }
}
