using System.Text;
using Pharos.Server.Lis.HL7.Application.Interfaces;

namespace Pharos.Server.Lis.HL7.Application.Messages;

internal sealed class HL7Message : IHL7Message
{
    public HL7HeaderSegment? Header { get; set; }

    public IList<IHL7Segment> Segments { get; } = new List<IHL7Segment>();

    public string GetMessageString(bool redactSensitiveData = false)
    {
        var messageText = new StringBuilder();
        var delimiters = Header?.Delimiters ?? HL7FieldDelimiters.Default;

        if (Header is not null)
        {
            messageText.Append(Header);
            messageText.Append('\r');
        }

        foreach (var segment in Segments)
        {
            messageText.Append(segment.ToSegmentString(delimiters, redactSensitiveData));
            messageText.Append('\r');
        }

        return messageText.ToString();
    }
}
