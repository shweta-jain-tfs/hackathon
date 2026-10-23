using System.Text;
using Pharos.Server.Lis.HL7.Application.Common;

namespace Pharos.Server.Lis.HL7.Application.Messages;

internal sealed record HL7FieldDelimiters(char Field, char Component, char Repeat, char Escape, char SubComponent)
{
    public static HL7FieldDelimiters Default { get; } = new('|', '^', '~', '\\', '&');

    private const string _escapeFieldDelimiter = "F";
    private const string _escapeRepeatDelimiter = "R";
    private const string _escapeComponentDelimiter = "S";
    private const string _escapeEscapeDelimiter = "E";
    private const string _escapeSubComponentDelimiter = "T";

    public static HL7FieldDelimiters Parse(string headerSegment)
    {
        if (headerSegment.Length < 8 || !headerSegment.StartsWith(HL7SegmentType.MessageHeader, StringComparison.Ordinal))
        {
            throw new HL7Exception("HL7 message header segment is invalid.");
        }

        var field = headerSegment[3];
        var component = headerSegment[4];
        var repeat = headerSegment[5];
        var escape = headerSegment[6];
        var subComponent = headerSegment[7];

        if (5 != new HashSet<char> { field, component, repeat, escape, subComponent }.Count)
        {
            throw new HL7Exception("HL7 field delimiters are not unique.");
        }

        return new HL7FieldDelimiters(field, component, repeat, escape, subComponent);
    }

    public string ToMessageString()
    {
        return $"{Component}{Repeat}{Escape}{SubComponent}";
    }

    public string AddEscape(string value)
    {
        return value
               .Replace(Escape.ToString(), $"{Escape}{_escapeEscapeDelimiter}{Escape}")
               .Replace(Field.ToString(), $"{Escape}{_escapeFieldDelimiter}{Escape}")
               .Replace(Component.ToString(), $"{Escape}{_escapeComponentDelimiter}{Escape}")
               .Replace(Repeat.ToString(), $"{Escape}{_escapeRepeatDelimiter}{Escape}")
               .Replace(SubComponent.ToString(), $"{Escape}{_escapeSubComponentDelimiter}{Escape}");
    }

    public string RemoveEscape(string value)
    {
        if (!value.Contains(Escape))
        {
            return value;
        }

        var result = new StringBuilder(value.Length);

        var parts = value.Split(Escape);
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (i % 2 == 0)
            {
                result.Append(part);
            }
            else
            {
                if (parts[i] == _escapeFieldDelimiter)
                {
                    result.Append(Field);
                }
                else if (parts[i] == _escapeComponentDelimiter)
                {
                    result.Append(Component);
                }
                else if (parts[i] == _escapeRepeatDelimiter)
                {
                    result.Append(Repeat);
                }
                else if (parts[i] == _escapeEscapeDelimiter)
                {
                    result.Append(Escape);
                }
                else if (parts[i] == _escapeSubComponentDelimiter)
                {
                    result.Append(Escape);
                }
                else
                {
                    result.Append(Escape);
                    result.Append(part);
                    result.Append(Escape);
                }
            }
        }

        return result.ToString();
    }
}
