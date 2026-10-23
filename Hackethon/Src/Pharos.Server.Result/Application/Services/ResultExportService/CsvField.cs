namespace Pharos.Server.Result.Application.Services.ResultExportService;

[AttributeUsage(AttributeTargets.Property)]
public sealed class CsvField(string fieldHeader) : Attribute
{
    public string FieldHeader { get; } = fieldHeader;
}
