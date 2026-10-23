using Pharos.Server.Parameters.Application.Parsers.ValueTypeParsers;

namespace Pharos.Server.Parameters.Application.Parsers;

internal abstract class ParameterArrayParser<T>
{
    protected int Index { get; }
    public bool HasToExist { get; }

    protected ParameterArrayParser(int index, bool hasToExist)
    {
        Index = index;
        HasToExist = hasToExist;
    }

    protected ParserResult<string> GetStringValue(string[] values, bool handelAsString)
    {
        if (Index > values.Length - 1) //check if index is larger than given array 
        {
            return new ParserResult<string>();
        }

        if (handelAsString)
        {
            return new ParserResult<string>(string.Join(";", values));
        }
        var value = values[Index];
        return new ParserResult<string>(value);
    }

    public abstract void Parse(T model, string[] values);
}