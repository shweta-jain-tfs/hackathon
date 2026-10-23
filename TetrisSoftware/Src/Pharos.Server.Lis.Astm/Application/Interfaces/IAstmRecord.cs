using Pharos.Server.Lis.Astm.Application.Messages;

namespace Pharos.Server.Lis.Astm.Application.Interfaces;

internal interface IAstmRecord
{
    char RecordType { get; }
    int SequenceNumber { get; set; }
    IList<string> ParsingErrors { get; set; }
    string ToRecordString(AstmFieldDelimiters delimiters, bool redactSensitiveData = false);
}
