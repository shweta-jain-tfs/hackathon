using System.Drawing;
using System.Text.RegularExpressions;

namespace Pharos.Server.Parameters.Application.Parsers.ValueTypeParsers;

internal class ColorParser : Parser<Color?>
{
#pragma warning disable
    private static readonly Regex _hexColorRegex = new("^#([0-9A-Fa-f]{6})$", RegexOptions.Compiled);

    public override ParserResult<Color?> GetValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new ParserResult<Color?>(null);
        }

        if (!_hexColorRegex.IsMatch(value))
        {
            throw new FormatException($"The value '{value}' is not a valid color.");
        }
        var color = ColorTranslator.FromHtml(value);
        return new ParserResult<Color?>(color);
    }
}