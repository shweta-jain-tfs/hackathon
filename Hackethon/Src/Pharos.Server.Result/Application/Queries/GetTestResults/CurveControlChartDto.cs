using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetTestResults;
public class CurveControlChartDto
{
    public double ExpectedConcentration { get; set; }
    public double UpperAlertLimit { get; set; }
    public double UpperWarning { get; set; }
    public double LowerWarning { get; set; }
    public double LowerAlert { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public DateTimeOffset StartDateRegion { get; set; }
    public DateTimeOffset EndDateRegion { get; set; }
    public double YMax { get; set; } 
    public double YMin { get; set; }
    public List<DateTimeOffset>? CalibrationCurveDates { get; set; }
    public List<CurveControlTestResultDto>? CurveControls { get; set; }
    public List<int>? ActivePoints { get; set; }
    public CurveControlJudgmentResult CurveControlTotalJudgment { get; set; } = CurveControlJudgmentResult.NotJudged;
    public bool AnyApprovedTestResultInRun { get; set; }
    public DateTimeOffset? FrozenAt { get; set; }
}