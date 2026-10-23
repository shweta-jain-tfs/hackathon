using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Queries.GetTestReplicateDetails;

namespace Pharos.Server.Result.Application.Queries.GetTestResults;

public class SampleTestResultDto
{
    public int Id { get; set; }
    public string TestName { get; set; } = default!; // used
    public string MethodName { get; set; } = default!; // used
    public ResultApprovalStatus ApprovalStatus { get; set; } // used
    public double TotalDilution { get; set; } // used
    public string? Comment { get; set; } // used
    public string Unit { get; set; } = default!; // used
    public List<SampleTestReplicateDto>? Replicates { get; set; } // used
    public int InstrumentDilution { get; set; } // used
    public int PreDilution { get; set; } // used
    public string SampleIdDisplay { get; set; } = default!; // used
}