using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetCalibrationCurve;

public class CalibratorTestReplicateDto
{
    public int Id { get; set; }

    public int ReplicateNumber { get; set; }

    /// <summary>
    /// The formatted version of the Response value
    /// </summary>
    public string? ResponseAsText { get; set; }

    /// <summary>
    /// The formatted version of the Concentration value.
    /// Also depends on ConcentrationStatus
    /// </summary>
    public string? ConcentrationAsText { get; set; }

    /// <summary>
    /// The raw response value for the test (after any response adjustments by for example lot specific code on some EliA carrier barcode).
    /// </summary>
    public double? Response { get; set; }

    public DateTimeOffset MeasurementDateTime { get; set; }

    public bool IsAborted { get; set; }

    public bool IsLowResponse { get; set; }

    /// <summary>
    /// For calibrator results: may be set to false if norm dose check fails
    /// For sample results: May be set to false by operator to not use the result of this replicate
    /// (if more than one replicate, the remaining Active replicate results may be used)
    /// </summary>
    public bool Active { get; set; } = true;

    public NormDoseCheckResult NormDoseCheckResult { get; set; } = NormDoseCheckResult.NotDefined;

    public int TestResultId { get; set; }

    public bool HasError { get; set; }
}
