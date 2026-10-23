using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Queries.GetTestReplicateDetails;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetTestResults;
public class CurveControlTestResultDto
{
    public int Id { get; set; }
    public string CurveControlPointName { get; set; } = default!;
    public double? ExpectedConcentration { get; set; }
    public string TestName { get; set; } = default!;
    public string MethodName { get; set; } = default!;
    public string? MeanResponseAsText { get; set; }
    public string? MeanConcentrationAsText { get; set; }
    public DateTimeOffset MeasurementDateTime { get; set; }
    public string? Comment { get; set; }
    public AssayTechnology Technology { get; set; }
    public string Unit { get; set; } = default!;
    /// <summary>
    /// Summary of the judgment of the curve control's replicates
    /// </summary>
    public CurveControlJudgment? JudgmentSummary { get; set; }
    public List<CurveControlTestReplicateDto>? Replicates { get; set; }
}
