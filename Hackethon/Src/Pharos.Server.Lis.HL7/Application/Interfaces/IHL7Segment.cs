using Pharos.Server.Lis.HL7.Application.Messages;

namespace Pharos.Server.Lis.HL7.Application.Interfaces;

internal interface IHL7Segment
{
    string SegmentType { get; }
    int SequenceNumber { get; set; }
    string ToSegmentString(HL7FieldDelimiters delimiters, bool redactSensitiveData = false);
}
