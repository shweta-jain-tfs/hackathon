namespace Pharos.Server.Parameters.Application.Parsers.ValueTypeParsers;

internal class NullableIntParser : Parser<int?>
{
    public override ParserResult<int?> GetValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return new ParserResult<int?>(default);
        }

        var result = int.Parse(value);

        return new ParserResult<int?>(result);
    }
}