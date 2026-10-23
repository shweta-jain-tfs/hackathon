using System.Globalization;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure.Logging;

namespace Pharos.Server.Lis.Astm.Application.Messages;

internal sealed class AstmPatientRecord : IAstmRecord
{
    public char RecordType => AstmRecordType.PatientInformation;
    public int SequenceNumber { get; set; } = 1;
    public string PracticePatientId { get; set; } = "";
    public string LaboratoryPatientId { get; set; } = "";
    public string PatientNumber { get; set; } = "";
    public string PatientName1 { get; set; } = "";
    public string PatientName2 { get; set; } = "";
    public DateOnly? BirthDate { get; set; }
    public Gender Gender { get; set; }

    public string AddressLine1 { get; set; } = "";
    public string AddressLine2 { get; set; } = "";
    public string AddressLine3 { get; set; } = "";
    public string AddressLine4 { get; set; } = "";

    /// <summary>
    /// The address line #5 is interpreted as Zip code(used in LabCommunity export)
    /// </summary>
    public string AddressLine5 { get; set; } = "";

    public string CommentLine1 { get; set; } = "";
    public string CommentLine2 { get; set; } = "";
    public string CommentLine3 { get; set; } = "";
    public string CommentLine4 { get; set; } = "";
    public string CommentLine5 { get; set; } = "";

    public int? Age { get; set; }
    public string Diagnosis { get; set; } = "";
    public List<SymptomType>? Symptoms { get; set; }
    public string MedicalRecordNumber { get; set; } = "";
    public IList<string> ParsingErrors { get; set; } = new List<string>();


    public static AstmPatientRecord Parse(string recordString, AstmFieldDelimiters delimiters)
    {
        var parsingErrors = new List<string>();
        var parser = new AstmRecordParser(recordString, delimiters, parsingErrors);
        var record = new AstmPatientRecord
        {
            ParsingErrors = parsingErrors,
            PracticePatientId = parser.FieldPart(PatientInformationRecordField.PracticePatientId, 0),
            LaboratoryPatientId = parser.FieldPart(PatientInformationRecordField.LaboratoryPatientId, 0),
            PatientNumber = parser.FieldPart(PatientInformationRecordField.PatientIdNo3, 0),
            PatientName1 = parser.FieldPart(PatientInformationRecordField.PatientName, 0),
            PatientName2 = parser.FieldPart(PatientInformationRecordField.PatientName, 1),
            Gender = parser.FieldPart(PatientInformationRecordField.PatientGender, 0) switch
            {
                "M" => Gender.Male,
                "F" => Gender.Female,
                _ => Gender.Undefined
            },
            AddressLine1 = parser.FieldPart(PatientInformationRecordField.PatientAddress, 0),
            AddressLine2 = parser.FieldPart(PatientInformationRecordField.PatientAddress, 1),
            AddressLine3 = parser.FieldPart(PatientInformationRecordField.PatientAddress, 2),
            AddressLine4 = parser.FieldPart(PatientInformationRecordField.PatientAddress, 3),
            AddressLine5 = parser.FieldPart(PatientInformationRecordField.PatientAddress, 4),
            CommentLine1 = parser.FieldPart(PatientInformationRecordField.SpecialField1, 0),
            CommentLine2 = parser.FieldPart(PatientInformationRecordField.SpecialField1, 1),
            CommentLine3 = parser.FieldPart(PatientInformationRecordField.SpecialField1, 2),
            CommentLine4 = parser.FieldPart(PatientInformationRecordField.SpecialField1, 3),
            CommentLine5 = parser.FieldPart(PatientInformationRecordField.SpecialField1, 4),
            Diagnosis = parser.FieldPart(PatientInformationRecordField.PatientKnownOrSuspectedDiagnosis, 0),
            MedicalRecordNumber = parser.FieldPart(PatientInformationRecordField.PracticeFieldNo1, 0)
        };

        record.SequenceNumber = record.ParseFieldOrDefault(() => parser.Field(PatientInformationRecordField.SequenceNumber).ShortOrZero(0), nameof(SequenceNumber), record.SequenceNumber);
        record.BirthDate = record.ParseFieldOrDefault(() => parser.Field(PatientInformationRecordField.BirthDate).AsDateOnly(), nameof(BirthDate), record.BirthDate);
        record.Age = record.ParseFieldOrDefault(() => parser.Field(PatientInformationRecordField.SpecialField2).ShortOrNull(0), nameof(Age), record.Age);
        record.Symptoms = record.ParseFieldOrDefault(() => ParseSymptoms(parser), nameof(Symptoms), record.Symptoms);

        return record;
    }

    public string ToRecordString(AstmFieldDelimiters delimiters, bool redactSensitiveData = false)
    {
        var rb = new AstmRecordBuilder<PatientInformationRecordField>(delimiters);

        rb.AddField(PatientInformationRecordField.RecordTypeId, RecordType.ToString());
        rb.AddField(PatientInformationRecordField.SequenceNumber, SequenceNumber.ToString(CultureInfo.InvariantCulture));
        rb.AddField(PatientInformationRecordField.PracticePatientId, PracticePatientId);
        rb.AddField(PatientInformationRecordField.LaboratoryPatientId, LaboratoryPatientId);
        rb.AddField(PatientInformationRecordField.PatientIdNo3, PatientNumber);

        if (string.IsNullOrWhiteSpace(PatientName2))
        {
            rb.AddField(PatientInformationRecordField.PatientName, PatientName1.Redact(redactSensitiveData));
        }
        else
        {
            rb.AddField(PatientInformationRecordField.PatientName, PatientName1.Redact(redactSensitiveData), PatientName2.Redact(redactSensitiveData));
        }

        if (BirthDate != null)
        {
            rb.AddField(PatientInformationRecordField.BirthDate, BirthDate.Value.ToString(AstmFieldParser.DateFormat, CultureInfo.InvariantCulture).Redact(redactSensitiveData));
        }

        rb.AddField(PatientInformationRecordField.PatientGender, Gender switch
        {
            Gender.Male => "M",
            Gender.Female => "F",
            _ => "U"
        });

        rb.AddField(PatientInformationRecordField.PatientAddress, TrimEmptyAtEnd(
            AddressLine1.Redact(redactSensitiveData),
            AddressLine2.Redact(redactSensitiveData),
            AddressLine3.Redact(redactSensitiveData),
            AddressLine4.Redact(redactSensitiveData),
            AddressLine5.Redact(redactSensitiveData)
        ));

        rb.AddField(PatientInformationRecordField.SpecialField1, TrimEmptyAtEnd(
            CommentLine1.Redact(redactSensitiveData),
            CommentLine2.Redact(redactSensitiveData),
            CommentLine3.Redact(redactSensitiveData),
            CommentLine4.Redact(redactSensitiveData),
            CommentLine5.Redact(redactSensitiveData)
        ));

        rb.AddField(PatientInformationRecordField.SpecialField2, Age?.ToString(CultureInfo.InvariantCulture) ?? "");

        var diagnosis = new List<IAstmRecordField>
        {
            new StringRecordField(Diagnosis)
        };
        if (Symptoms is { Count: > 0 })
        {
            diagnosis.Add(new RepeatRecordField(Symptoms.Select(s => new StringRecordField(((short)s).ToString(CultureInfo.InvariantCulture)))));
        }
        rb.AddField(PatientInformationRecordField.PatientKnownOrSuspectedDiagnosis, diagnosis.ToArray());

        rb.AddField(PatientInformationRecordField.PracticeFieldNo1, MedicalRecordNumber.Redact(redactSensitiveData));

        return rb.ToRecordString();
    }

    private static string[] TrimEmptyAtEnd(params string[] items)
    {
        var remaining = items.ToList();
        while (remaining.Count > 0 && string.IsNullOrWhiteSpace(remaining.Last()))
        {
            remaining.RemoveAt(remaining.Count - 1);
        }
        return remaining.ToArray();
    }

    private static List<SymptomType>? ParseSymptoms(AstmRecordParser parser)
    {
        if (string.IsNullOrEmpty(parser.FieldPart(PatientInformationRecordField.PatientKnownOrSuspectedDiagnosis, 1)))
        {
            return null;
        }

        return parser.Field(PatientInformationRecordField.PatientKnownOrSuspectedDiagnosis)
            .SubComponents(1)
            .AsShortList()
            .Select(s => (SymptomType)s)
            .ToList();
    }

    private enum PatientInformationRecordField
    {
        RecordTypeId,
        SequenceNumber,
        PracticePatientId,
        LaboratoryPatientId,
        PatientIdNo3,
        PatientName,
        MothersMaidenName,
        BirthDate,
        PatientGender,
        PatientRaceEthnicOrigin,
        PatientAddress,
        ReservedField,
        PatientTelephoneNumber,
        AttendingPhysicianId,
        SpecialField1,
        SpecialField2,
        PatientHeight,
        PatientWeight,
        PatientKnownOrSuspectedDiagnosis,
        PatientActiveMedications,
        PatientsDiet,
        PracticeFieldNo1,
        PracticeFieldNo2,
        AdmissionAndDischargeDates,
        AdmissionStatus,
        Location,
        NatureOfAlternativeDiagnosticCodeAndClassifiers,
        AlternativeDiagnosticCodeAndClassification,
        PatientReligion,
        MaritalStatus,
        IsolationStatus,
        Language,
        HospitalService,
        HospitalInstitution,
        DosageCategory
    }

}
