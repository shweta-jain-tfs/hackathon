using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Parsers;

internal abstract class ParameterPropertyParserBase<T>
{
    protected ParameterPropertyParserBase(string name, bool hasToExist)
    {
        Name = name;
        HasToExist = hasToExist;
    }

    public string Name { get; }
    public bool HasToExist { get; }
    public int LengthOfRequiredArray { get; set; }
    public abstract void Parse(ParameterFile file, ParameterProperty parameterData, T parentObject);
}

internal class PropertyObjectParser<T, TItem> : ParameterPropertyParserBase<T> where TItem : class, new()
{
    private readonly List<ParameterArrayParser<TItem>> _parsers;
    private readonly Action<T, TItem> _addToList;

    public PropertyObjectParser(string name, bool hasToExist, List<ParameterArrayParser<TItem>> parsers, Action<T, TItem> addToList) : base(name, hasToExist)
    {
        _parsers = parsers;
        _addToList = addToList;
        SetLengthOfRequiredArray(parsers);
    }

    public override void Parse(ParameterFile file, ParameterProperty parameterData, T parentObject)
    {
        var model = new TItem(); //List object

        foreach (var parser in _parsers) //loop the parsers for each splitted value, each index. 
        {
            parser.Parse(model, parameterData.Value.Split(";"));
        }

        _addToList.Invoke(parentObject, model);
    }

    private void SetLengthOfRequiredArray(List<ParameterArrayParser<TItem>> parsers)
    {
        var requiredLength = 0;

        foreach (var p in parsers)
        {
            if (p.HasToExist)
            {
                requiredLength++;
            }
            else
            {
                break;
            }
        }

        LengthOfRequiredArray = requiredLength;
    }
}

internal class PropertyArrayParser<T> : ParameterPropertyParserBase<T> where T : class
{
    private readonly List<ParameterArrayParser<T>> _parsers;

    public PropertyArrayParser(string name, bool hasToExist, List<ParameterArrayParser<T>> parsers) : base(name, hasToExist)
    {
        _parsers = parsers;
        SetLengthOfRequiredArray(parsers);
    }

    private void SetLengthOfRequiredArray(List<ParameterArrayParser<T>> parsers)
    {
        var requiredLength = 0;

        foreach (var p in parsers)
        {
            if (p.HasToExist)
            {
                requiredLength++;
            }
            else
            {
                break;
            }
        }

        LengthOfRequiredArray = requiredLength;
    }


    public override void Parse(ParameterFile file, ParameterProperty parameterData, T parentObject)
    {
        foreach (var parser in _parsers)
        {
            parser.Parse(parentObject, parameterData.Value.Split(";"));
        }
    }
}

internal class PropertyParser<T> : PropertyArrayParser<T> where T : class
{
    public PropertyParser(string name, bool hasToExist, ParameterArrayParser<T> parser) : base(name, hasToExist, new List<ParameterArrayParser<T>> { parser })
    {
    }
}