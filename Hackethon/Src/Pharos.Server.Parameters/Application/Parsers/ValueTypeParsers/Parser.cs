namespace Pharos.Server.Parameters.Application.Parsers.ValueTypeParsers;

internal abstract class Parser<T>
{
    public abstract ParserResult<T> GetValue(string value);
}

internal class ParserResult<T>
{
    private ParserResult(T? value, bool isSuccess)
    {
        Value = value;
        IsSuccess = isSuccess;
    }

    public ParserResult() : this(default, false)
    {
    }

    public ParserResult(T value) : this(value, true)
    {
    }

    public T? Value { get; }
    public bool IsSuccess { get; }
}