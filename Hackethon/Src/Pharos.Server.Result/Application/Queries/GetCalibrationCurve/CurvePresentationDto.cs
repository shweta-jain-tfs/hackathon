using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Queries.GetAnalyticalRuns;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetCalibrationCurve;

#pragma warning disable CS8618

public class CurvePresentationDto
{
    public int Id { get; set; }
    public CurveJudgment Judgment { get; set; } = CurveJudgment.NotOK;
    public ResultApprovalStatus ApprovalStatus { get; set; } = ResultApprovalStatus.NotApproved;
    public DateTimeOffset? MeasurementDateTime { get; set; }
    public DateOnly? LastDayToUse { get; set; }
    public MethodPresentationDto? MethodPresentation { get; set; }
    public IList<CalibratorTestResultDto>? CalibratorTestResults { get; set; }
    public IList<CalculatedPointDto>? CalculatedPoints { get; set; }
    public bool CurveFittedFlag { get; set; }
    public string? ConjugateLot { get; set; }
    public string? CarrierLot { get; set; }
    public CalculationStateStatus CurveState { get; set; }
    public bool ActiveCurve { get; set; }
    public bool SelectedCurve { get; set; }
    public bool CurveControlDisabledFlag { get; set; }
    public bool IsAccepted { get; set; }
    public bool IsManuallySelected { get; set; }
}

public class CalculatedPointDto
{
    public double Response { get; set; }
    public double Concentration { get; set; }
}
