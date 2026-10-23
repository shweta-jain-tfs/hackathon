using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Result.Domain;

/// <summary>
/// Represents the final result for a test order on a curve control.
/// </summary>
public class QualityControlTestResult : GroupTestResult
{
    public QualityControlTestResult()
    {
        ResultType = TestResultType.QualityControl;
    }

    /// <summary>
    /// Name of this Quality Control
    /// </summary>
    [MaxLength(Constants.QualityControlNameLength)]
    public string QualityControlName { get; set; } = default!;

    /// <summary>
    /// Type of QC
    /// </summary>
    public QualityControlType QualityControlType { get; set; }

    /// <summary>
    /// Expected concentration for this QC.
    /// </summary>
    public double? ExpectedConcentration { get; set; }

    /// <summary>
    /// Expected standard deviation for this QC.
    /// </summary>
    public double? ExpectedStdDev { get; set; }

    /// <summary>
    /// Standard deviation limit for this QC.
    /// </summary>
    public double? StdDevLimit { get; set; }

    /// <summary>
    /// Min concentration limit for this QC
    /// </summary>
    public double? MinValue { get; set; }

    /// <summary>
    /// If set to true the <see cref="MinValue"/> must be excluded from the range even if
    /// the value is provided (target is greater than MinValue).
    /// </summary>
    public bool ExcludeMinValue { get; set; }

    /// <summary>
    /// Max concentration limit for this QC.
    /// </summary>
    public double? MaxValue { get; set; }

    /// <summary>
    /// If set to true the <see cref="MaxValue"/> must be excluded from the range even if
    /// the value is provided (target is less than MaxValue).
    /// </summary>
    public bool ExcludeMaxValue { get; set; }

    ///// <summary>
    ///// Quality control judgement
    ///// </summary>
    public QualityControlJudgement Judgment { get; set; } = QualityControlJudgement.NotDefined;

    [MaxLength(SampleTestResult.ClassLength)]
    public string? MeanClass { get; set; }

    [MaxLength(SampleTestResult.CutOffLength)]
    public string? MeanCutOff { get; set; }

    public double? MeanQuotient { get; set; }

    [MaxLength(SampleTestResult.QuotientAsTextLength)]
    public string? MeanQuotientAsText { get; set; }

    public bool LowestClassFlag { get; set; }
    public bool HighestClassFlag { get; set; }

    /// <summary>
    /// If the Quality Control was produced from a tube, this is the tube identifier. Null for cartridge-based Quality Controls.
    /// </summary>
    [MaxLength(Constants.TubeIdMaxLength)]
    public string? TubeId { get; set; }
}
