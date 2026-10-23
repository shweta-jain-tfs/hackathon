namespace Pharos.Server.Parameters.Application.Parsers.ValueTypeParsers;

internal enum BoolStrategy
{
    ZeroAsFalseOneAsTrue,
    ZeroAsFalseRestAsTrue
}

internal class BoolParser(BoolStrategy boolStrategy) : Parser<bool>
{
    public override ParserResult<bool> GetValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return new ParserResult<bool>(false);
        }
        var number = int.Parse(value);

        if (number == 0)
        {
            return new ParserResult<bool>(false);
        }

        if (number == 1)
        {
            return new ParserResult<bool>(true);
        }

        if (boolStrategy == BoolStrategy.ZeroAsFalseOneAsTrue)
        {
            throw new FormatException($"{number} is not a valid boolean value for this field.");
        }

        return new ParserResult<bool>(true);
    }
}