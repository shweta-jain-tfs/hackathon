using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetCalibrationCurve;

#pragma warning disable CS8618

public class CalibratorTestResultDto
{
    public int Id { get; set; }
    public string CalibratorPointName { get; set; }
    public double ExpectedConcentration { get; set; }
    public string TestName { get; set; } = default!;
    public string MethodName { get; set; } = default!;
    public string? MeanResponseAsText { get; set; }
    public string? MeanConcentrationAsText { get; set; }
    public DateTimeOffset MeasurementDateTime { get; set; }
    public string? Comment { get; set; }
    public AssayTechnology Technology { get; set; }
    public string Unit { get; set; } = default!;

    /// <summary>
    /// A summary of the norm dose checks for the calibrator's replicates
    /// </summary>
    public NormDoseCheckSummary NormDoseCheckSummary { get; set; }

    public List<CalibratorTestReplicateDto> Replicates { get; set; }
}
