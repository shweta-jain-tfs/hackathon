using System.Globalization;
using Pharos.Server.Lis.HL7.Application.Common;

namespace Pharos.Server.Lis.HL7.Application.Messages;

internal sealed class HL7FieldParser
{
    public const string DateTimeFormat = "yyyyMMddHHmmss";
    public const string DateFormat = "yyyyMMdd";

    private readonly HL7FieldDelimiters _delimiters;
    private readonly int _fieldIndex;
    private readonly string[] _components;

    internal HL7FieldParser(string input, HL7FieldDelimiters delimiters, int fieldIndex)
        : this(input, delimiters, fieldIndex, delimiters.Component)
    {
    }

    internal HL7FieldParser(string input, HL7FieldDelimiters delimiters, int fieldIndex, char splitChar)
    {
        _components = input.Split(splitChar);
        _delimiters = delimiters;
        _fieldIndex = fieldIndex;
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

    internal HL7FieldParser SubComponents(int componentIndex)
    {
        return new HL7FieldParser(Component(componentIndex), _delimiters, _fieldIndex, _delimiters.Repeat);
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
        throw new HL7Exception($"Found illegal value: '{part}' for parameter '{_fieldIndex}'");
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
        throw new HL7Exception($"Found illegal value: '{part}' for parameter '{_fieldIndex}'");
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
        throw new HL7Exception($"Found illegal value: '{part}' for parameter '{_fieldIndex}'");
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
        throw new HL7Exception($"Found illegal value: '{part}' for parameter '{_fieldIndex}'");
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
        throw new HL7Exception($"Found illegal value: '{strDate}' for parameter '{_fieldIndex}'");
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
        throw new HL7Exception($"Found illegal value: '{strDate}' for parameter '{_fieldIndex}'");
    }

    private string DeEscapeString(string str)
    {
        return _delimiters.RemoveEscape(str).Trim();
    }
}
