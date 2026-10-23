using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetTestReplicateDetails;

public class CurveControlTestReplicateDto
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
    /// The allergen concentration in the serum (calculated using a calibration curve
    /// after the raw response is received).
    /// </summary>
    public double? Concentration { get; set; }

    public bool IsAborted { get; set; }

    public DateTimeOffset MeasurementDateTime { get; set; }

    public CurveControlJudgment? CurveControlJudgment { get; set; }

    public string? ConjugateLotNumber { get; set; }

    public bool HasError { get; set; }
}