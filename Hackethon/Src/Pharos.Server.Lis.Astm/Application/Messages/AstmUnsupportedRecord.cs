using Pharos.Server.Infrastructure.Logging;

namespace Pharos.Server.Lis.Astm.Application.Messages;

internal sealed class AstmUnsupportedRecord : IAstmRecord
{
    public AstmUnsupportedRecord(string recordString)
    {
        RecordString = recordString ?? string.Empty;
        ParsingErrors = new List<string>();
    }

    public string RecordString { get; }

    public char RecordType => !string.IsNullOrEmpty(RecordString) ? RecordString[0] : '\0';

    public int SequenceNumber { get; set; }

    public IList<string> ParsingErrors { get; set; }

    public string ToRecordString(AstmFieldDelimiters delimiters, bool redactSensitiveData = false)
    {
        return RecordString.Redact(redactSensitiveData);
    }
}
