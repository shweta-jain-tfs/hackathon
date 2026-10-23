namespace Pharos.Server.Request.Application.Queries.GetRequestors;

public class RequestorSummaryDto
{
    public int Id { get; set; }
    public string RequestorId { get; set; } = default!;
    public string? Name { get; set; }
    public bool ReflexEnabled { get; set; }
    public DateTimeOffset? LatestSampleDate { get; set; }
    public int NumberOfReportedSamples { get; set; }
    public int NumberOfReportedTests { get; set; }
    public DateTimeOffset? CounterResetDate { get; set; }
}