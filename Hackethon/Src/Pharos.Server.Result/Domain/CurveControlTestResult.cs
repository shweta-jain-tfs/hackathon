namespace Pharos.Server.Result.Domain;

/// <summary>
/// Represents the final result for a test order on a curve control.
/// </summary>
public class CurveControlTestResult : GroupTestResult
{
    public const int CurveControlPointNameLength = 32;

    public CurveControlTestResult()
    {
        ResultType = TestResultType.CurveControl;
    }

    [MaxLength(CurveControlPointNameLength)]
    public string CurveControlPointName { get; set; } = default!;

    public double ExpectedConcentration { get; set; }

    /// <summary>
    /// Summary of the judgment of the curve control's replicates
    /// </summary>
    public CurveControlJudgment JudgmentSummary { get; set; } = CurveControlJudgment.NotDefined;
}
