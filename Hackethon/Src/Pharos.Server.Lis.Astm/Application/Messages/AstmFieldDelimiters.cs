using System.Text;

namespace Pharos.Server.Lis.Astm.Application.Messages;

internal sealed record AstmFieldDelimiters(char Field, char Repeat, char Component, char Escape)
{
    public static AstmFieldDelimiters Default { get; } = new AstmFieldDelimiters('|', '\\', '^', '&');

    private const string _escapeFieldDelimiter = "F";
    private const string _escapeRepeatDelimiter = "R";
    private const string _escapeComponentDelimiter = "S";
    private const string _escapeEscapeDelimiter = "E";


    public static AstmFieldDelimiters Parse(string headerRecord)
    {
        var field = headerRecord[1];
        var repeat = headerRecord[2];
        var component = headerRecord[3];
        var escape = headerRecord[4];

        if (4 != new HashSet<char> { field, repeat, component, escape }.Count)
        {
            throw new AstmException("ASTM field delimiters are not unique.");
        }
        return new AstmFieldDelimiters(field, repeat, component, escape);
    }

    public string ToMessageString()
    {
        return $"{Repeat}{Component}{Escape}";
    }

    public string AddEscape(string s)
    {
        return s
               .Replace(Escape.ToString(), $"{Escape}{_escapeEscapeDelimiter}{Escape}")
               .Replace(Field.ToString(), $"{Escape}{_escapeFieldDelimiter}{Escape}")
               .Replace(Repeat.ToString(), $"{Escape}{_escapeRepeatDelimiter}{Escape}")
               .Replace(Component.ToString(), $"{Escape}{_escapeComponentDelimiter}{Escape}")
            ;
    }

    public string RemoveEscape(string str)
    {
        if (!str.Contains(Escape))
        {
            return str;
        }

        var sb = new StringBuilder();

        var parts = str.Split(Escape);

        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (i % 2 == 0)
            {
                sb.Append(part);
            }
            else
            {
                if (parts[i] == _escapeFieldDelimiter)
                {
                    sb.Append(Field);
                }
                else if (parts[i] == _escapeComponentDelimiter)
                {
                    sb.Append(Component);
                }
                else if (parts[i] == _escapeRepeatDelimiter)
                {
                    sb.Append(Repeat);
                }
                else if (parts[i] == _escapeEscapeDelimiter)
                {
                    sb.Append(Escape);
                }
                else
                {
                    sb.Append(Escape);
                    sb.Append(part);
                    sb.Append(Escape);
                }
            }
        }
        return sb.ToString();
    }
}