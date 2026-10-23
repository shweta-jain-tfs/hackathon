using System.Globalization;

namespace Pharos.Server.Lis.Astm.Application.Messages;

internal sealed record AstmTerminatorRecord(string TerminationCode) : IAstmRecord
{
    public static AstmTerminatorRecord Default { get; } = new AstmTerminatorRecord("N");

    public char RecordType => AstmRecordType.MessageTerminator;

    public int SequenceNumber { get => 1; set => throw new NotSupportedException(); }
    public IList<string> ParsingErrors { get; set; } = new List<string>();
    public static AstmTerminatorRecord Parse(string recordString, AstmFieldDelimiters delimiters)
    {
        var parser = new AstmRecordParser(recordString, delimiters);

        var terminationCode = parser.FieldPart(TerminatorRecordField.TerminationCode, 0);
        if (terminationCode == "I" || terminationCode == "F")
        {
            return new AstmTerminatorRecord(terminationCode);
        }

        return new AstmTerminatorRecord("N");
    }

    public string ToRecordString(AstmFieldDelimiters delimiters, bool redactSensitiveData = false)
    {
        var rb = new AstmRecordBuilder<TerminatorRecordField>(delimiters);
        rb.AddField(TerminatorRecordField.RecordTypeId, RecordType.ToString());
        rb.AddField(TerminatorRecordField.SequenceNumber, SequenceNumber.ToString(CultureInfo.InvariantCulture));
        rb.AddField(TerminatorRecordField.TerminationCode, TerminationCode);
        return rb.ToRecordString();
    }

    private enum TerminatorRecordField
    {
        RecordTypeId,
        SequenceNumber,
        TerminationCode
    }
}


