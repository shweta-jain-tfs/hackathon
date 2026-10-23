namespace Pharos.Server.Request.Application.Models;

internal class ImportedRequests
{
    public string? CustomRunId { get; set; }
    public List<ImportedSample> Samples { get; } = new List<ImportedSample>();
}

