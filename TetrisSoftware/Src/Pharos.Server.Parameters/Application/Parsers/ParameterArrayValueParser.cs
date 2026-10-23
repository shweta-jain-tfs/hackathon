using Pharos.Server.Parameters.Application.Parsers.ValueTypeParsers;

namespace Pharos.Server.Parameters.Application.Parsers;

#pragma warning disable CS8604

internal class ParameterArrayValueParser<T, TValue> : ParameterArrayParser<T>
{
    private readonly ParserDefault<TValue>? _defaultValue;
    private readonly Parser<TValue> _parser;
    private readonly Action<T, TValue> _valueSetter;
    private readonly bool _handleAsString;

    public ParameterArrayValueParser(int index, bool hasToExist, Action<T, TValue> valueSetter, Parser<TValue> parser, bool handleAsString = false) : this(index, hasToExist, null, valueSetter, parser, handleAsString)
    {

    }

    public ParameterArrayValueParser(int index, bool hasToExist, ParserDefault<TValue>? parserDefaultValue, Action<T, TValue> valueSetter, Parser<TValue> parser, bool handleAsString = false) : base(index, hasToExist)
    {
        _valueSetter = valueSetter;
        _parser = parser;
        _defaultValue = parserDefaultValue;
        _handleAsString = handleAsString;
    }

    public override void Parse(T model, string[] values)
    {
        var arrayResult = GetStringValue(values, _handleAsString);
        var result = _parser.GetValue(arrayResult.Value);

        var useDefault = _defaultValue != null && string.IsNullOrEmpty(arrayResult.Value) && _defaultValue.UseDefaultWhenNullOrEmpty;
        var useStandardDefault = !result.IsSuccess && !HasToExist;
        var emptyValueNoDefaultHasToExistShouldThrow = HasToExist && string.IsNullOrEmpty(arrayResult.Value) && (_defaultValue == null || !_defaultValue.UseDefaultWhenNullOrEmpty);

        if (useDefault)
        {
            _valueSetter(model, _defaultValue!.DefaultValue);
        }
        else if (useStandardDefault)
        {
            _valueSetter(model, default);
        }
        else if (result.IsSuccess && !emptyValueNoDefaultHasToExistShouldThrow)
        {
            _valueSetter(model, result.Value);
        }
        else
        {
            var value = "";
            
            if (values.Length > 0)
            {
                value = values[0];
            }
            
            throw new InvalidDataException($"Missing Value, HasToExist is true and ParserDefault False or not specified, or fail to parse: {arrayResult.Value} {value} {model}.");
        }
    }
}

internal class ParameterValueParser<T, TValue> : ParameterArrayValueParser<T, TValue>
{
    public ParameterValueParser(bool hasToExist, Action<T, TValue> valueSetter, Parser<TValue> parser, bool handleWhole = false) : base(0, hasToExist, null, valueSetter, parser, handleWhole)
    {
    }
}

internal class ParserDefault<T>
{
    public ParserDefault(T defaultValue, bool useDefaultWhenNullOrEmpty)
    {
        DefaultValue = defaultValue;
        UseDefaultWhenNullOrEmpty = useDefaultWhenNullOrEmpty;
    }

    public T DefaultValue { get; }
    public bool UseDefaultWhenNullOrEmpty { get; }
}