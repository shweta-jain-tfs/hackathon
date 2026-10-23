using System.Globalization;

namespace Pharos.Server.Lis.Astm.Application.Messages;

internal sealed class AstmFieldParser
{
    public const string DateTimeFormat = "yyyyMMddHHmmss";
    public const string DateFormat = "yyyyMMdd";

    private readonly AstmFieldDelimiters _delimiters;
    private readonly int _fieldIndex;
    private readonly IList<string>? _parsingErrors;
    private readonly string[] _components;

    internal AstmFieldParser(string input, AstmFieldDelimiters delimiters, int fieldIndex, IList<string>? parsingErrors = null)
        : this(input, delimiters, fieldIndex, delimiters.Component, parsingErrors)
    {
    }

    internal AstmFieldParser(string input, AstmFieldDelimiters delimiters, int fieldIndex, char splitChar, IList<string>? parsingErrors = null)
    {
        _components = input.Split(splitChar);
        _delimiters = delimiters;
        _fieldIndex = fieldIndex;
        _parsingErrors = parsingErrors;
    }

    internal string Component(int componentIndex)
    {
        return _components.Length <= componentIndex ? "" : DeEscapeString(_components[componentIndex]);
    }

    internal List<string> AsStringList()
    {
        return _components.Select(DeEscapeString).ToList();
    }

    internal List<short> AsShortList()
    {
        return AsStringList().Select(ShortOrZero).ToList();
    }

    internal AstmFieldParser SubComponents(int componentIndex)
    {
        return new AstmFieldParser(Component(componentIndex), _delimiters, _fieldIndex, _delimiters.Repeat, _parsingErrors);
    }

    internal int? IntOrNull(int componentIndex)
    {
        var part = Component(componentIndex);
        if (string.IsNullOrWhiteSpace(part))
        {
            return null;
        }

        if (int.TryParse(part, out var result))
        {
            return result;
        }
        ReportParsingError(part);
        return null;
    }

    internal int IntOrZero(int componentIndex)
    {
        var part = Component(componentIndex);
        if (string.IsNullOrWhiteSpace(part))
        {
            return 0;
        }
        if (int.TryParse(part, out var result))
        {
            return result;
        }
        ReportParsingError(part);
        return 0;
    }

    internal short? ShortOrNull(int componentIndex)
    {
        var part = Component(componentIndex);
        if (string.IsNullOrWhiteSpace(part))
        {
            return null;
        }
        if (short.TryParse(part, out var result))
        {
            return result;
        }
        ReportParsingError(part);
        return null;
    }

    internal short ShortOrZero(int componentIndex)
    {
        var part = Component(componentIndex);
        return ShortOrZero(part);
    }

    private short ShortOrZero(string part)
    {
        if (string.IsNullOrWhiteSpace(part))
        {
            return 0;
        }

        if (short.TryParse(part, out var result))
        {
            return result;
        }
        ReportParsingError(part);
        return 0;
    }

    internal DateTime? AsDateAndTime()
    {
        var strDateTime = Component(0);
        if (string.IsNullOrWhiteSpace(strDateTime))
        {
            return null;
        }

        if (strDateTime.Length == 14
            && DateTime.TryParseExact(strDateTime, DateTimeFormat, null, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }
        return AsDate();
    }

    internal DateTime? AsDate()
    {
        var strDate = Component(0);
        if (string.IsNullOrWhiteSpace(strDate))
        {
            return null;
        }

        var datePart = strDate.Length == 13 ? strDate[..8] : strDate;

        if (datePart.Length == 8
            && DateTime.TryParseExact(datePart, DateFormat, null, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }
        ReportParsingError(strDate);
        return null;
    }

    internal DateOnly? AsDateOnly()
    {
        var strDate = Component(0);
        if (string.IsNullOrWhiteSpace(strDate))
        {
            return null;
        }

        var datePart = strDate.Length == 13 ? strDate[..8] : strDate;

        if (datePart.Length == 8
            && DateOnly.TryParseExact(datePart, DateFormat, null, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }
        ReportParsingError(strDate);
        return null;
    }

    private void ReportParsingError(string value)
    {
        var message = $"Found illegal value: '{value}' for parameter '{_fieldIndex}'";
        if (_parsingErrors is null)
        {
            throw new AstmException(message);
        }

        _parsingErrors.Add(message);
    }

    private string DeEscapeString(string str)
    {
        return _delimiters.RemoveEscape(str).Trim();
    }
}