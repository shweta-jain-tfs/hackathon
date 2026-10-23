using System.Globalization;
using Pharos.Server.Lis.HL7.Application.Interfaces;

namespace Pharos.Server.Lis.HL7.Application.Messages;

internal sealed class HL7SegmentBuilder<T> where T : struct, Enum
{
    private readonly Dictionary<int, string> _fieldValues = new();
    private readonly HL7FieldDelimiters _delimiters;

    public HL7SegmentBuilder(HL7FieldDelimiters delimiters)
    {
        _delimiters = delimiters;
    }

    public void AddField(T fieldId, params string[] value)
    {
        // remove empty trailing components first
        var components = new List<IHL7SegmentField>();
        var hasNonEmptyValue = false;
        for (var i = value.Length - 1; i >= 0; i--)
        {
            hasNonEmptyValue |= !string.IsNullOrWhiteSpace(value[i]);
            if (hasNonEmptyValue)
            {
                components.Add(new StringSegmentField(value[i]));
            }
        }
        components.Reverse();
        AddField(fieldId, components.ToArray());
    }

    public void AddField(T fieldId, params IHL7SegmentField[] value)
    {
        var escaped = value.Select(v => v.ToString(_delimiters)).ToArray();
        AddFieldWithoutEscaping(fieldId, escaped);
    }

    private void AddFieldWithoutEscaping(T fieldId, params string[] value)
    {
        _fieldValues.Add(EnumToInt(fieldId), string.Join(_delimiters.Component, value));
    }

    public string ToSegmentString()
    {
        var fieldCount = _fieldValues.Where(e => !string.IsNullOrWhiteSpace(e.Value)).Max(e => e.Key) + 1;
        return string.Join(_delimiters.Field, Enumerable.Range(0, fieldCount).Select(FieldValue));
    }

    private string FieldValue(int index)
    {
        return _fieldValues.TryGetValue(index, out var value) ? value : "";
    }

    private static int EnumToInt(T fieldId)
    {
        return Convert.ToInt32(Convert.ChangeType(fieldId, Enum.GetUnderlyingType(typeof(T)), CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }
}

internal sealed record StringSegmentField(string Value) : IHL7SegmentField
{
    public string ToString(HL7FieldDelimiters delimiters)
    {
        return delimiters.AddEscape(Value ?? "");
    }
}

internal sealed record ComponentArrayField(string[] Components) : IHL7SegmentField
{
    public string ToString(HL7FieldDelimiters delimiters)
    {
        return string.Join(delimiters.Component, Components);
    }
}

internal sealed record RepeatSegmentField(IEnumerable<IHL7SegmentField> Values) : IHL7SegmentField
{
    public string ToString(HL7FieldDelimiters delimiters)
    {
        return string.Join(delimiters.Repeat, Values.Select(v => v.ToString(delimiters)));
    }
}

internal sealed record DelimiterSegmentField() : IHL7SegmentField
{
    public string ToString(HL7FieldDelimiters delimiters)
    {
        return delimiters.ToMessageString();
    }
}
