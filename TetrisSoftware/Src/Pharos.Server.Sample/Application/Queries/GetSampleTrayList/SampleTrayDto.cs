namespace Pharos.Server.Sample.Application.Queries.GetSampleTrayList;

public class SampleTrayDto
{
    public string Id { get; set; } = default!;
    public int FreePositions { get; set; }
    public string? InstrumentName { get; set; }
    public int ErrorCount { get; set; }
    public bool HasTubeIssues => ErrorCount > 0;
}