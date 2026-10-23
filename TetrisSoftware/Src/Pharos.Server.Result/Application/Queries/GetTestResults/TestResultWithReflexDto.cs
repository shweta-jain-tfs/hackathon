using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Result.Application.Queries.GetTestResults;

public class TestResultWithReflexDto
{
    public int Id { get; set; }
    public Guid TestOrderId { get; set; }
    public TestResultStatus Status { get; set; }
    public string? Concentration { get; set; }
    public string? ConcentrationRaw { get; set; }
    public string? Quotient { get; set; }
    public string? CutOff { get; set; }
    public string? Class { get; set; }
}