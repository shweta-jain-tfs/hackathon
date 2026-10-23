namespace Pharos.Server.Result.Domain;

/// <summary>
/// Represents the final result for a test order on a calibrator.
/// </summary>
public class CalibratorTestResult : GroupTestResult
{
    public const int CalibratorPointNameLength = 32;

    public CalibratorTestResult()
    {
        ResultType = TestResultType.Calibrator;
    }
    public int? CurveId { get; set; }

    [MaxLength(CalibratorPointNameLength)]
    public string CalibratorPointName { get; set; } = default!;

    public double ExpectedConcentration { get; set; } = default!;

    /// <summary>
    /// A summary of the norm dose checks for the calibrator's replicates
    /// </summary>
    public NormDoseCheckSummary NormDoseCheckSummary { get; set; }

    /// <summary>
    /// Artificial response value created by curve fit
    /// </summary>
    public double? ArtificialResponse { get; set; }

    /// <summary>
    /// Level index returned from curve fit
    /// </summary>
    public int LevelIndex { get; set; }
}
