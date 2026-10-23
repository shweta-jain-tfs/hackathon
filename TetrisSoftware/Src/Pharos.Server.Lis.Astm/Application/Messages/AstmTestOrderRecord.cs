using System.Globalization;
using Pharos.Server.Infrastructure.Logging;

namespace Pharos.Server.Lis.Astm.Application.Messages;

internal enum AstmTestAction
{
    Undefined,
    Cancel,
    QualityControl,
    NewTest,
    ReTest,
    AddTest
}

internal enum AstmReportType
{
    Undefined, // 
    Order, // "O"
    FinalResult, // "F"
    ResultPending, // "I"
    Error // "X"
}

internal sealed class AstmTestOrderRecord : IAstmRecord
{
    public char RecordType => AstmRecordType.TestOrder;
    public int SequenceNumber { get; set; } = 1;
    public string SpecimenId { get; set; } = "";
    public string TubeType { get; set; } = "";
    public string RackId { get; set; } = "";
    public int RackPosition { get; set; }
    public string? TrayId { get; set; }
    public string InstrumentSpecimenId { get; set; } = "";
    public List<AstmTestId> UniversalTestIds { get; set; } = new();
    public string Priority { get; set; } = "";
    public DateTimeOffset? RequestedDateAndTime { get; set; }
    public DateTimeOffset? SpecimenCollectionDateAndTime { get; set; }
    public int? CollectionVolume { get; set; }
    public string CollectorId { get; set; } = "";
    public AstmTestAction TestAction { get; set; }
    public string DangerCode { get; set; } = "";
    public int Dilution { get; set; }
    public DateTimeOffset? SpecimenReceivedDateAndTime { get; set; }
    public string SpecimenDescriptor { get; set; } = "";
    public string RequestorId { get; set; } = "";
    public string RequestorName { get; set; } = "";
    public string RequestorShortcutCode { get; set; } = "";
    public string RequestorPhone { get; set; } = "";
    public string RequestorAddressLine1 { get; set; } = "";
    public string RequestorAddressLine2 { get; set; } = "";
    public string RequestorAddressLine3 { get; set; } = "";
    public string RequestorAddressLine4 { get; set; } = "";
    public string RequestorAddressLine5 { get; set; } = "";
    public string RequestorComment { get; set; } = "";
    public int RequestorReflexTesting { get; set; }
    public string UserFieldNo2 { get; set; } = "";
    public string LaboratoryFieldNo1 { get; set; } = "";
    public string LaboratoryFieldNo2 { get; set; } = "";
    public DateTimeOffset? ResultsReportedOrLastModifiedDateAndTime { get; set; }
    public string InstrumentChargeToComputerSystem { get; set; } = "";
    public string InstrumentSectionId { get; set; } = "";
    public AstmReportType ReportType { get; set; }
    public string ReservedField { get; set; } = "";
    public string HospitalCode { get; set; } = "";
    public string HospitalName { get; set; } = "";
    public string SectionCode { get; set; } = "";
    public string SectionName { get; set; } = "";
    public string WardCode { get; set; } = "";
    public string WardName { get; set; } = "";
    public string NoscomialInfectionFlag { get; set; } = "";
    public string SpecimenService { get; set; } = "";
    public string SpecimenInstitution { get; set; } = "";
    public IList<string> ParsingErrors { get; set; } = new List<string>();

    public static AstmTestOrderRecord Parse(string recordString, AstmFieldDelimiters delimiters)
    {
        var parsingErrors = new List<string>();
        var parser = new AstmRecordParser(recordString, delimiters, parsingErrors);
        var record = new AstmTestOrderRecord
        {
            ParsingErrors = parsingErrors,
            SpecimenId = parser.FieldPart(TestOrderRecordField.SpecimenId, 0),
            TubeType = !string.IsNullOrEmpty(parser.FieldPart(TestOrderRecordField.SpecimenId, 1)) ? parser.FieldPart(TestOrderRecordField.SpecimenId, 1) : "N",
            RackId = parser.FieldPart(TestOrderRecordField.SpecimenId, 2),
            InstrumentSpecimenId = parser.FieldPart(TestOrderRecordField.InstrumentSpecimenId, 0),
            Priority = parser.FieldPart(TestOrderRecordField.Priority, 0),
            CollectorId = parser.FieldPart(TestOrderRecordField.CollectorId, 0),
            TestAction = parser.FieldPart(TestOrderRecordField.ActionCode, 0) switch
            {
                "C" => AstmTestAction.Cancel,
                "N" => AstmTestAction.NewTest,
                "A" => AstmTestAction.AddTest,
                "R" => AstmTestAction.ReTest,
                "Q" => AstmTestAction.QualityControl,
                _ => AstmTestAction.Undefined
            },
            DangerCode = parser.FieldPart(TestOrderRecordField.DangerCode, 0),
            SpecimenDescriptor = parser.FieldPart(TestOrderRecordField.SpecimenDescriptor, 0),
            RequestorId = parser.FieldPart(TestOrderRecordField.OrderingPhysician, 0),
            RequestorName = parser.FieldPart(TestOrderRecordField.OrderingPhysician, 1),
            RequestorShortcutCode = parser.FieldPart(TestOrderRecordField.OrderingPhysician, 2),
            RequestorPhone = parser.FieldPart(TestOrderRecordField.PhysiciansTelephoneNumber, 0),
            RequestorAddressLine1 = parser.FieldPart(TestOrderRecordField.UserFieldNo1, 0),
            RequestorAddressLine2 = parser.FieldPart(TestOrderRecordField.UserFieldNo1, 1),
            RequestorAddressLine3 = parser.FieldPart(TestOrderRecordField.UserFieldNo1, 2),
            RequestorAddressLine4 = parser.FieldPart(TestOrderRecordField.UserFieldNo1, 3),
            RequestorAddressLine5 = parser.FieldPart(TestOrderRecordField.UserFieldNo1, 4),
            RequestorComment = parser.FieldPart(TestOrderRecordField.UserFieldNo1, 5),
            UserFieldNo2 = parser.FieldPart(TestOrderRecordField.UserFieldNo2, 0),
            LaboratoryFieldNo1 = parser.FieldPart(TestOrderRecordField.LaboratoryFieldNo1, 0),
            LaboratoryFieldNo2 = parser.FieldPart(TestOrderRecordField.LaboratoryFieldNo2, 0),
            InstrumentChargeToComputerSystem = parser.FieldPart(TestOrderRecordField.InstrumentChargeToComputerSystem, 0),
            InstrumentSectionId = parser.FieldPart(TestOrderRecordField.InstrumentSectionId, 0),
            ReportType = parser.Field(TestOrderRecordField.ReportTypes).SubComponents(0).Component(0) switch
            {
                "O" => AstmReportType.Order,
                _ => AstmReportType.Undefined
            },
            ReservedField = parser.FieldPart(TestOrderRecordField.ReservedField, 0),
            HospitalCode = parser.FieldPart(TestOrderRecordField.LocationOrWardOfSpecimenCollection, 0),
            HospitalName = parser.FieldPart(TestOrderRecordField.LocationOrWardOfSpecimenCollection, 1),
            SectionCode = parser.FieldPart(TestOrderRecordField.LocationOrWardOfSpecimenCollection, 2),
            SectionName = parser.FieldPart(TestOrderRecordField.LocationOrWardOfSpecimenCollection, 3),
            WardCode = parser.FieldPart(TestOrderRecordField.LocationOrWardOfSpecimenCollection, 4),
            WardName = parser.FieldPart(TestOrderRecordField.LocationOrWardOfSpecimenCollection, 5),
            NoscomialInfectionFlag = parser.FieldPart(TestOrderRecordField.NosocomialInfectionFlag, 0),
            SpecimenService = parser.FieldPart(TestOrderRecordField.SpecimenService, 0),
            SpecimenInstitution = parser.FieldPart(TestOrderRecordField.SpecimenInstitution, 0)
        };

        record.SequenceNumber = record.ParseFieldOrDefault(() => parser.Field(TestOrderRecordField.SequenceNumber).ShortOrZero(0), nameof(SequenceNumber), record.SequenceNumber);
        record.RackPosition = record.ParseFieldOrDefault(() => parser.Field(TestOrderRecordField.SpecimenId).ShortOrZero(3), nameof(RackPosition), record.RackPosition);
        record.UniversalTestIds = record.ParseFieldOrDefault(
            () => parser.FieldListOfLists(TestOrderRecordField.UniversalTestIds)
                .Select(f => new AstmTestId(f.Component(3), f.Component(4), f.IntOrZero(5), f.ShortOrZero(6), f.Component(7), f.ShortOrZero(8)))
                .ToList(),
            nameof(UniversalTestIds),
            record.UniversalTestIds);
        record.RequestedDateAndTime = record.ParseFieldOrDefault(() => parser.Field(TestOrderRecordField.RequestedDateAndTime).AsDateAndTime(), nameof(RequestedDateAndTime), record.RequestedDateAndTime);
        record.SpecimenCollectionDateAndTime = record.ParseFieldOrDefault(() => parser.Field(TestOrderRecordField.SpecimenCollectionDateAndTime).AsDateAndTime(), nameof(SpecimenCollectionDateAndTime), record.SpecimenCollectionDateAndTime);
        record.CollectionVolume = record.ParseFieldOrDefault(() => parser.Field(TestOrderRecordField.CollectionVolume).IntOrNull(0), nameof(CollectionVolume), record.CollectionVolume);
        record.Dilution = record.ParseFieldOrDefault(() => parser.Field(TestOrderRecordField.RelevantClinicalInformation).IntOrZero(0), nameof(Dilution), record.Dilution);
        record.SpecimenReceivedDateAndTime = record.ParseFieldOrDefault(() => parser.Field(TestOrderRecordField.SpecimenReceivedDateAndTime).AsDateAndTime(), nameof(SpecimenReceivedDateAndTime), record.SpecimenReceivedDateAndTime);
        record.RequestorReflexTesting = record.ParseFieldOrDefault(() => parser.Field(TestOrderRecordField.UserFieldNo1).ShortOrNull(6) ?? 1, nameof(RequestorReflexTesting), record.RequestorReflexTesting);
        record.ResultsReportedOrLastModifiedDateAndTime = record.ParseFieldOrDefault(() => parser.Field(TestOrderRecordField.ResultsReportedOrLastModifiedDateAndTime).AsDateAndTime(), nameof(ResultsReportedOrLastModifiedDateAndTime), record.ResultsReportedOrLastModifiedDateAndTime);

        return record;
    }

    public string ToRecordString(AstmFieldDelimiters delimiters, bool redactSensitiveData = false)
    {
        var rb = new AstmRecordBuilder<TestOrderRecordField>(delimiters);

        rb.AddField(TestOrderRecordField.RecordTypeId, RecordType.ToString());
        rb.AddField(TestOrderRecordField.SequenceNumber, SequenceNumber.ToString(CultureInfo.InvariantCulture));

        var specimenIdParts = new List<string>
                              {
                                  SpecimenId,
                                  TubeType,
                                  RackId,
                                  RackPosition != 0 ? RackPosition.ToString(CultureInfo.InvariantCulture) : ""
                              };

        if (TrayId != null)
        {
            specimenIdParts.Add(TrayId);
        }

        rb.AddField(TestOrderRecordField.SpecimenId, specimenIdParts.ToArray());
        rb.AddField(TestOrderRecordField.InstrumentSpecimenId, InstrumentSpecimenId);

        rb.AddField(TestOrderRecordField.UniversalTestIds, new RepeatRecordField(UniversalTestIds.Select(id => new ComponentArrayField(
                                                                                                      new string[] {
                                                                                                                              "", "",  "",
                                                                                                                              id.TestName,
                                                                                                                              id.LisMethodId,
                                                                                                                              id.InstrumentDilution.ToString(CultureInfo.InvariantCulture)
                                                                                                                   }))));

        rb.AddField(TestOrderRecordField.RequestedDateAndTime, RequestedDateAndTime?.ToString(AstmFieldParser.DateTimeFormat, CultureInfo.InvariantCulture) ?? "");
        rb.AddField(TestOrderRecordField.SpecimenCollectionDateAndTime, SpecimenCollectionDateAndTime?.ToString(AstmFieldParser.DateTimeFormat, CultureInfo.InvariantCulture) ?? "");

        rb.AddField(TestOrderRecordField.ActionCode, TestAction == AstmTestAction.QualityControl ? "Q" : "N");
        rb.AddField(TestOrderRecordField.RelevantClinicalInformation, Dilution.ToString(CultureInfo.InvariantCulture));
        rb.AddField(TestOrderRecordField.OrderingPhysician, RequestorId, RequestorName.Redact(redactSensitiveData), RequestorShortcutCode);

        rb.AddField(TestOrderRecordField.PhysiciansTelephoneNumber, RequestorPhone.Redact(redactSensitiveData));
        rb.AddField(
            TestOrderRecordField.UserFieldNo1,
            RequestorAddressLine1.Redact(redactSensitiveData),
            RequestorAddressLine2.Redact(redactSensitiveData),
            RequestorAddressLine3.Redact(redactSensitiveData),
            RequestorAddressLine4.Redact(redactSensitiveData),
            RequestorAddressLine5.Redact(redactSensitiveData),
            RequestorComment.Redact(redactSensitiveData),
            RequestorReflexTesting == 1 ? "1" : "0"
        );

        rb.AddField(TestOrderRecordField.LaboratoryFieldNo1, LaboratoryFieldNo1);

        rb.AddField(TestOrderRecordField.ResultsReportedOrLastModifiedDateAndTime, ResultsReportedOrLastModifiedDateAndTime?.ToString(AstmFieldParser.DateTimeFormat, CultureInfo.InvariantCulture) ?? "");

        rb.AddField(TestOrderRecordField.InstrumentSectionId, InstrumentSectionId);
        rb.AddField(TestOrderRecordField.ReportTypes, ReportType switch
        {
            AstmReportType.FinalResult => "F",
            AstmReportType.ResultPending => "I",
            AstmReportType.Error => "X",
            _ => "O"
        });

        rb.AddField(TestOrderRecordField.LocationOrWardOfSpecimenCollection, HospitalCode, HospitalName.Redact(redactSensitiveData), SectionCode, SectionName.Redact(redactSensitiveData), WardCode, WardName.Redact(redactSensitiveData));

        return rb.ToRecordString();
    }

    private enum TestOrderRecordField
    {
        RecordTypeId,
        SequenceNumber,
        SpecimenId,
        InstrumentSpecimenId,
        UniversalTestIds,
        Priority,
        RequestedDateAndTime,
        SpecimenCollectionDateAndTime,
        CollectionEndTime,
        CollectionVolume,
        CollectorId,
        ActionCode,
        DangerCode,
        RelevantClinicalInformation,
        SpecimenReceivedDateAndTime,
        SpecimenDescriptor,
        OrderingPhysician,
        PhysiciansTelephoneNumber,
        UserFieldNo1,
        UserFieldNo2,
        LaboratoryFieldNo1,
        LaboratoryFieldNo2,
        ResultsReportedOrLastModifiedDateAndTime,
        InstrumentChargeToComputerSystem,
        InstrumentSectionId,
        ReportTypes,
        ReservedField,
        LocationOrWardOfSpecimenCollection,
        NosocomialInfectionFlag,
        SpecimenService,
        SpecimenInstitution
    }
}
