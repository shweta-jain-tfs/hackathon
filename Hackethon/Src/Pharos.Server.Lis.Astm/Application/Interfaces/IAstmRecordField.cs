using Pharos.Server.Lis.Astm.Application.Messages;

namespace Pharos.Server.Lis.Astm.Application.Interfaces;

internal interface IAstmRecordField
{
    string ToString(AstmFieldDelimiters delimiters);
}
