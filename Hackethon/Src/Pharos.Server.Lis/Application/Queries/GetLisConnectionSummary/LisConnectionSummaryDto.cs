using Pharos.Server.Lis.Application.Interfaces;

namespace Pharos.Server.Lis.Application.Queries.GetLisConnectionSummary;

public class LisConnectionSummaryDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public LisConnectionState PrimaryConnectionState { get; set; }
    public LisConnectionState? ExportConnectionState { get; set; }
    public LisConnectionStatisticsDto Queries { get; set; } = new();
    public LisConnectionStatisticsDto Imported { get; set; } = new();
    public LisConnectionStatisticsDto Exported { get; set; } = new();
}

public class LisConnectionStatisticsDto
{
    public int Samples { get; set; }
    public int? Tests { get; set; }
    public DateTimeOffset? LatestTime { get; set; }
    public string? LatestSample { get; set; }
}