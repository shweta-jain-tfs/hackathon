using System.Globalization;
using Pharos.Server.Lis.HL7.Application.Common;

namespace Pharos.Server.Lis.HL7.Application.Messages;

internal sealed record HL7SegmentParser(string SegmentString, HL7FieldDelimiters Delimiters)
{
    private readonly string[] _fields = SegmentString.Split(Delimiters.Field);

    public static string GetRecordType(string segmentString)
    {
        if (string.IsNullOrWhiteSpace(segmentString) || segmentString.Length < 3)
        {
            throw new HL7Exception("HL7 segment does not contain a valid segment type.");
        }

        return segmentString[..3].ToUpperInvariant();
    }

    public HL7FieldParser Field<T>(T fieldId) where T : struct, Enum
    {
        var fieldIndex = EnumToInt(fieldId);
        return new HL7FieldParser(_fields.Length <= fieldIndex ? "" : _fields[fieldIndex], Delimiters, fieldIndex);
    }

    public string FieldPart<T>(T fieldId, int componentIndex) where T : struct, Enum
    {
        return Field(fieldId).Component(componentIndex);
    }

    public List<HL7FieldParser> FieldListOfLists<T>(T fieldId) where T : struct, Enum
    {
        var fieldIndex = EnumToInt(fieldId);
        var field = _fields.Length <= fieldIndex ? "" : _fields[fieldIndex];
        return field.Split(Delimiters.Repeat).Select(s => new HL7FieldParser(s, Delimiters, fieldIndex)).ToList();
    }

    private static int EnumToInt<T>(T fieldId) where T : struct, Enum
    {
        return Convert.ToInt32(Convert.ChangeType(fieldId, Enum.GetUnderlyingType(typeof(T)), CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }
}