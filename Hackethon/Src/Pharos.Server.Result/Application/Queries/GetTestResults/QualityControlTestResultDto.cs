using Pharos.Server.Result.Application.Queries.GetTestReplicateDetails;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetTestResults;

public class QualityControlTestResultDto
{
    public int Id { get; set; }
    public string? Identity { get; set; }
    public List<QualityControlTestReplicateDto>? Replicates { get; set; }
    /// <summary>
    /// Data for limit calculation.
    /// Can be Min, Max, Min + Max, or from 'expected std dev' calculation
    /// Note that one diagram can contain multiple types of limit calculations
    /// </summary>
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public double? ExpectedConcentration { get; set; }
    public double? ExpectedStdDev { get; set; }
    public double? StdDevLimit { get; set; }

    /// <summary>
    /// Summary of the qc judgment
    /// </summary>
    public QualityControlJudgement? Judgment { get; set; }

    public string TestName { get; set; } = default!;
    public string MethodName { get; set; } = default!;
    public string? MeanResponseAsText { get; set; }
    public string? MeanConcentrationAsText { get; set; }
    public double? Concentration { get; set; }
    public DateTimeOffset MeasurementDateTime { get; set; }
    public double TotalDilution { get; set; }
    public double InstrumentDilution { get; set; }
    public double PreDilution { get; set; }
    public string? Comment { get; set; }
    public string Unit { get; set; } = default!;
}
