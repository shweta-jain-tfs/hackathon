namespace Pharos.Server.Parameters.Application.Parsers.ValueTypeParsers;

#pragma warning disable CS8625

internal class StringParser : Parser<string>
{
    public override ParserResult<string> GetValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new ParserResult<string>(null);
        }

        value = value.Trim();
        return new ParserResult<string>(value);
    }
}