namespace Pharos.Server.Lis.Astm.Application.Messages;

internal static class AstmRecordParsingExtensions
{
    internal static T ParseFieldOrDefault<T>(this IAstmRecord record, Func<T> parseField, string propertyName, T defaultValue)
    {
        var errorCount = record.ParsingErrors.Count;
        var value = parseField();
        if (record.ParsingErrors.Count == errorCount)
        {
            return value;
        }

        for (var index = errorCount; index < record.ParsingErrors.Count; index++)
        {
            record.ParsingErrors[index] = $"Failed parsing ASTM field for '{propertyName}' in record '{record.GetType().Name}'. {record.ParsingErrors[index]}";
        }

        return defaultValue;
    }
}
