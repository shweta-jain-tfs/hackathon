using System.Text.Json.Serialization;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Result.Domain;

public class Curve : BaseEntity
{
    /// <summary>
    /// The instrument serial number.
    /// </summary>
    [MaxLength(Constants.InstrumentSerialNumberLength)]
    public string InstrumentNumber { get; set; } = default!;

    [MaxLength(Constants.MethodNameLength)]
    public string MethodName { get; set; } = default!;

    [MaxLength(Constants.LotNumberLength)]
    public string? ConjugateLotNumber { get; set; }

    [MaxLength(Constants.LotNumberLength)]
    public string? ImmunoCapLotNumber { get; set; }

    public ResultApprovalStatus ApprovalStatus { get; set; } = ResultApprovalStatus.NotApproved;
    public CalculationType CalculationType { get; set; } = CalculationType.Rodbard5Par;
    public double? CalculationParameterA { get; set; }
    public double? CalculationParameterB { get; set; }
    public double? CalculationParameterC { get; set; }
    public double? CalculationParameterD { get; set; }
    public double? CalculationParameterE { get; set; }
    public bool CurveFittedFlag { get; set; }
    public int? IterationsUsed { get; set; }
    public CurveJudgment Judgment { get; set; } = CurveJudgment.NotOK;
    public int? MaxIterationsAllowed { get; set; }
    public DateTimeOffset MeasurementDateTime { get; set; }
    public DateOnly FirstDayToUse { get; set; }
    public DateOnly LastDayToUse { get; set; }
    public int? ZeroPointCalculationNumberOfIntervals { get; set; }
    public int? NumberOfCalibrationPointsInZeroPointCalculation { get; set; }
    public double? ZeroPointCalculationCoefficientAlfa { get; set; }
    public double? ZeroPointCalculationCoefficientBeta { get; set; }
    public bool? ErrorPrecisionFlag { get; set; }
    public bool? ErrorFlatCurveFlag { get; set; }
    public bool? ErrorLowerPartTooHighFlag { get; set; }
    public bool? ErrorSingleCalibratorAboveRangeFlag { get; set; }
    public bool? ErrorSingleCalibratorBelowRangeFlag { get; set; }
    public bool? ErrorNoRisingTrendFlag { get; set; }
    public bool? CurveControlDisabledFlag { get; set; }
    public double? ZeroPointSwitchConcentration { get; set; }
    public double? CurveMinConcentration { get; set; }
    public double? CurveMaxConcentration { get; set; }
    public bool LastCurveControlsAborted { get; set; }
    public Guid GroupId { get; set; }
    public Guid InstrumentRunId { get; set; }

    #region Navigation properties

    public virtual IList<CurrentCurve>? CurrentCurves { get; set; }

    [JsonIgnore]
    public virtual IList<CalibratorTestResult>? TestResults { get; set; }

    #endregion
}
