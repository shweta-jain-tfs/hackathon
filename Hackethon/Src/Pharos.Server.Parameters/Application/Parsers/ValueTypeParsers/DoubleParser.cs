using System.Globalization;

namespace Pharos.Server.Parameters.Application.Parsers.ValueTypeParsers;

internal class DoubleParser : Parser<double>
{
    private readonly int _maxDigits;

    public DoubleParser(int maxDigits)
    {
        _maxDigits = maxDigits;
    }

    public override ParserResult<double> GetValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return new ParserResult<double>(default);
        }

        // CultureInfo.InvariantCulture uses . as a decimal separator, and , as a thousands separator.
        // We assume we don't have any thousand separators in the input string.
        value = value.Replace(',', '.');

        if (double.TryParse(value, CultureInfo.InvariantCulture, out var result))
        {
            return new ParserResult<double>(Math.Round(result, _maxDigits));
        }

        return new ParserResult<double>();
    }
}