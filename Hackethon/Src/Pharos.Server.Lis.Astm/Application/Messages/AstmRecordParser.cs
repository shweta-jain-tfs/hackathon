using System.Globalization;

namespace Pharos.Server.Lis.Astm.Application.Messages;

internal sealed record AstmRecordParser(string RecordString, AstmFieldDelimiters Delimiters, IList<string>? ParsingErrors = null)
{
    private readonly string[] _fields = RecordString.Split(Delimiters.Field);

    public static char GetRecordType(string recordString)
    {
        return char.ToUpperInvariant(recordString[0]);
    }

    public AstmFieldParser Field<T>(T fieldId) where T : struct, Enum
    {
        var fieldIndex = EnumToInt(fieldId);
        return new AstmFieldParser(_fields.Length <= fieldIndex ? "" : _fields[fieldIndex], Delimiters, fieldIndex, ParsingErrors);
    }

    public string FieldPart<T>(T fieldId, int componentIndex) where T : struct, Enum
    {
        return Field(fieldId).Component(componentIndex);
    }

    public List<AstmFieldParser> FieldListOfLists<T>(T fieldId) where T : struct, Enum
    {
        var fieldIndex = EnumToInt(fieldId);
        var field = _fields.Length <= fieldIndex ? "" : _fields[fieldIndex];
        return field.Split(Delimiters.Repeat).Select(s => new AstmFieldParser(s, Delimiters, fieldIndex, ParsingErrors)).ToList();
    }

    private static int EnumToInt<T>(T fieldId) where T : struct, Enum
    {
        return Convert.ToInt32(Convert.ChangeType(fieldId, Enum.GetUnderlyingType(typeof(T)), CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }
}
