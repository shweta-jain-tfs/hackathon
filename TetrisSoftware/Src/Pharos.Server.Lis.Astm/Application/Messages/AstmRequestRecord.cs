using System.Globalization;

namespace Pharos.Server.Lis.Astm.Application.Messages;

internal sealed class AstmRequestRecord : IAstmRecord
{
    public char RecordType => AstmRecordType.RequestInformation;

    public int SequenceNumber { get; set; }

    public string? SpecimenId { get; set; }
    public IList<string> ParsingErrors { get; set; } = new List<string>();

    internal static AstmRequestRecord Parse(string recordString, AstmFieldDelimiters delimiters)
    {
        var record = new AstmRequestRecord();
        var parser = new AstmRecordParser(recordString, delimiters, record.ParsingErrors);

        record.SequenceNumber = record.ParseFieldOrDefault(() => parser.Field(MessageRequestRecordField.SequenceNumber).ShortOrZero(0), nameof(SequenceNumber), record.SequenceNumber);

        if (parser.FieldPart(MessageRequestRecordField.StartingRangeIdNumber, 0) != "INV")
        {
            throw new AstmException("Only information requests are supported");
        }

        return record;
    }

    public string ToRecordString(AstmFieldDelimiters delimiters, bool redactSensitiveData = false)
    {
        var rb = new AstmRecordBuilder<MessageRequestRecordField>(delimiters);
        rb.AddField(MessageRequestRecordField.RecordTypeId, RecordType.ToString());
        rb.AddField(MessageRequestRecordField.SequenceNumber, SequenceNumber.ToString(CultureInfo.InvariantCulture));
        rb.AddField(MessageRequestRecordField.StartingRangeIdNumber, "", string.IsNullOrWhiteSpace(SpecimenId) ? "ALL" : SpecimenId);
        rb.AddField(MessageRequestRecordField.RequestInformationStatusCodes, "O");
        return rb.ToRecordString();
    }

    private enum MessageRequestRecordField
    {
        RecordTypeId,
        SequenceNumber,
        StartingRangeIdNumber,
        EndingRangeIdNumber,
        UniversalTestId,
        NatureOfRequestTimeLimits,
        BeginningRequestResultsDateAndTime,
        EndingRequestResultsDateAndTime,
        RequestingPhysicianName,
        RequestingPhysicianTelephoneNumber,
        UserFieldNo1,
        UserFieldNo2,
        RequestInformationStatusCodes
    }
}
