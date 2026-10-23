using System.Globalization;
using System.Reflection;
using System.Text;

namespace Pharos.Server.Result.Application.Services.ResultExportService;
internal class CsvWriter(char delimiter = ';')
{
    public string GenerateCsv<T>(IEnumerable<T> data)
    {
        var properties = typeof(T).GetProperties().Where(property => property.GetCustomAttribute<CsvField>() != null).ToList();
        if (properties.Count == 0)
        {
            throw new ArgumentException($"The object does not contain any properties with the {nameof(CsvField)} attribute.", nameof(data));
        }

        var sb = new StringBuilder();
        // header row
        _ = sb.AppendLine(GetCsvHeaders<T>(properties));
        // data rows
        foreach (var item in data)
        {
            _ = sb.AppendLine(GetCsvLine(item, properties));
        }
        return sb.ToString();
    }

    private string GetCsvHeaders<T>(IEnumerable<PropertyInfo> properties)
    {
        var headers = properties.Select(GetCsvFieldAttribute);
        return string.Join(delimiter, headers);
    }

    private string GetCsvLine<T>(T obj, IEnumerable<PropertyInfo> properties)
    {
        var fields = properties.Select(prop => Convert.ToString(prop.GetValue(obj), CultureInfo.InvariantCulture) ?? "");
        return string.Join(delimiter, fields);
    }

    private static string GetCsvFieldAttribute(MemberInfo property)
    {
        var attr = property.GetCustomAttribute<CsvField>()
            ?? throw new ArgumentNullException($"Missing CsvField custom header for {property.Name}.");

        return attr.FieldHeader;
    }
}