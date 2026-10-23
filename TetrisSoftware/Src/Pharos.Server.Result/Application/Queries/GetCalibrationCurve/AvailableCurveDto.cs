using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetCalibrationCurve;
public class AvailableCurveDto
{
    public int Id { get; set; }
    public DateTimeOffset MeasurementDateTime { get; set; }
    public CurveJudgment Judgment { get; set; }
    public string ConjugateLotNr { get; set; } = default!;
}