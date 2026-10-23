namespace Pharos.Server.LogDump.Application.Queries.GetLogDumpCollectionResult;

public class LogDumpCollectionResultDto
{
    public Guid FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTimeOffset FileCreatedDateTime { get; set; }
}