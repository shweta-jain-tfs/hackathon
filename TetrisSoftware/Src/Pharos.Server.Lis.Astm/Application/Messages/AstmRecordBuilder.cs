using System.Globalization;

namespace Pharos.Server.Lis.Astm.Application.Messages;

internal sealed class AstmRecordBuilder<T> where T : struct, Enum
{
    private readonly Dictionary<int, string> _fieldValues = new();
    private readonly AstmFieldDelimiters _delimiters;

    public AstmRecordBuilder(AstmFieldDelimiters delimiters)
    {
        _delimiters = delimiters;
    }

    public void AddField(T fieldId, params string[] value)
    {
        // remove empty trailing components fists
        var components = new List<IAstmRecordField>();
        var hasNonEmptyValue = false;
        for (var i = value.Length - 1; i >= 0; i--)
        {
            hasNonEmptyValue |= !string.IsNullOrWhiteSpace(value[i]);
            if (hasNonEmptyValue)
            {
                components.Add(new StringRecordField(value[i]));
            }
        }
        components.Reverse();
        AddField(fieldId, components.ToArray());
    }

    public void AddField(T fieldId, params IAstmRecordField[] value)
    {
        var escaped = value.Select(v => v.ToString(_delimiters)).ToArray();
        AddFieldWithoutEscaping(fieldId, escaped);
    }

    private void AddFieldWithoutEscaping(T fieldId, params string[] value)
    {
        _fieldValues.Add(EnumToInt(fieldId), string.Join(_delimiters.Component, value));
    }

    public string ToRecordString()
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

internal sealed record StringRecordField(string Value) : IAstmRecordField
{
    public string ToString(AstmFieldDelimiters delimiters)
    {
        return delimiters.AddEscape(Value ?? "");
    }
}

internal sealed record ComponentArrayField(string[] Components) : IAstmRecordField
{
    public string ToString(AstmFieldDelimiters delimiters)
    {
        return string.Join(delimiters.Component, Components);
    }
}

internal sealed record RepeatRecordField(IEnumerable<IAstmRecordField> Values) : IAstmRecordField
{
    public string ToString(AstmFieldDelimiters delimiters)
    {
        return string.Join(delimiters.Repeat, Values.Select(v => v.ToString(delimiters)));
    }
}

internal sealed record DelimiterRecordField() : IAstmRecordField
{
    public string ToString(AstmFieldDelimiters delimiters)
    {
        return delimiters.ToMessageString();
    }
}
