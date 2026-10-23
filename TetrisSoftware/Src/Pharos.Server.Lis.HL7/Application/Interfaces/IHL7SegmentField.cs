using Pharos.Server.Lis.HL7.Application.Messages;

namespace Pharos.Server.Lis.HL7.Application.Interfaces;

internal interface IHL7SegmentField
{
    string ToString(HL7FieldDelimiters delimiters);
}
