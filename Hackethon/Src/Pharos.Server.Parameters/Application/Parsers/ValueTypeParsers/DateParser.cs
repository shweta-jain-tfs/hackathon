using System.Globalization;

namespace Pharos.Server.Parameters.Application.Parsers.ValueTypeParsers;

internal class DateParser : Parser<DateTime?>
{
    public override ParserResult<DateTime?> GetValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return new ParserResult<DateTime?>(default);
        }

        value = AddYear(value);

        if (DateTime.TryParseExact(value, "yyyyMMdd:HHmmss", null, DateTimeStyles.None, out var result))
        {
            return new ParserResult<DateTime?>(result);
        }

        return new ParserResult<DateTime?>();
    }

    private static string AddYear(string value)
    {
        //format is yyMMdd:HHmmss
        if (value.Length == 13)
        {
            if (int.TryParse(value.AsSpan(0, 2), out var year))
            {
                // YY >= 80 year 1980 - 1999
                if (year >= 80)
                {
                    value = value.Insert(0, "19");
                }

                // YY < 80 year 2000 - 2079
                if (year < 80)
                {
                    value = value.Insert(0, "20");
                }
            }
        }

        //format is yyyyMMdd:HHmmss
        return value;
    }
}