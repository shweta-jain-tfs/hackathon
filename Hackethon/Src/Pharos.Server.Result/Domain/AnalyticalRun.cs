using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;

#pragma warning disable CS8618
namespace Pharos.Server.Result.Domain;

public class AnalyticalRun : BaseEntity
{
    public const int CmrIdLength = 50;

    /// <summary>
    /// The unique ID of the analytical run that is used by the instrument.
    /// </summary>
    public Guid InstrumentAnalyticalRunId { get; set; }

    /// <summary>
    /// The name of the method used in this analytical run.
    /// </summary>
    [MaxLength(Constants.MethodNameLength)]
    public string MethodName { get; set; }

    /// <summary>
    /// The run start time stamp.
    /// </summary>
    public DateTimeOffset StartDateTime { get; set; }

    /// <summary>
    /// The run finish time stamp.
    /// </summary>
    public DateTimeOffset? EndDateTime { get; set; }

    /// <summary>
    /// Conjugate lot used in this analytical run
    /// </summary>
    [MaxLength(Constants.LotNumberLength)]
    public string? ConjugateLotNumber { get; set; }

    /// <summary>
    /// ImmunoCap / EliA Well lot number used in this analytical run.
    /// Null if this lot number does not need to be the same for all included tests (a method parameter setting)
    /// </summary>
    [MaxLength(Constants.LotNumberLength)]
    public string? CarrierLotNumber { get; set; }

    /// <summary>
    /// Marks if the whole analytical runs has been approved
    /// </summary>
    public ResultApprovalStatus ApprovalStatus { get; set; }

    /// <summary>
    /// Operator that approved the analytical run
    /// </summary>
    public Guid? ApprovedBy { get; set; }

    /// <summary>
    /// The instrument serial number.
    /// </summary>
    [MaxLength(Constants.InstrumentSerialNumberLength)]
    public string InstrumentNumber { get; set; }

    /// <summary>
    /// ID of the curve assosiated with the analyticalrun it might be used for calculation dependent on curve state
    /// That curve may be included in another analytical run
    /// </summary>
    public int? CurveId { get; set; }

    /// <summary>
    /// Status of the analytical run. The run is created when it is processing, so that the initial state
    /// </summary>
    public RunStatus RunStatus { get; set; } = RunStatus.Processing;

    /// <summary>
    /// Describes how the curve was assigned to this analytical run.
    /// Replaces the former CurveControlDisabledFlag and WaitingForManuallyTriggeredCurve flags.
    /// </summary>
    public CurveSource CurveSource { get; set; } = CurveSource.Missing;

    /// <summary>
    /// Tracks the calculation readiness and state of this analytical run.
    /// Once Calculated is reached the state is terminal — it never resets on new curve/CC events.
    /// </summary>
    public CurveState CurveState { get; set; } = CurveState.Valid;

    /// <summary>
    /// The UTC timestamp at which the run transitioned to <see cref="CurveState.Frozen"/>.
    /// Null until commitment is reached. Used to exclude CCs that arrived after commitment from chart display.
    /// </summary>
    public DateTimeOffset? FrozenAt { get; set; }

    /// <summary>
    /// True when concentrations are allowed to be calculated for this run.
    /// </summary>
    [NotMapped]
    public bool CanCalculateConcentrations => CurveSource is not CurveSource.Missing
        && CurveState is CurveState.Valid or CurveState.Frozen;

    /// <summary>
    /// Run ID of a Custom made run
    /// </summary>
    [MaxLength(CmrIdLength)]
    public string? CmrRunId { get; set; }

    /// <summary>
    /// Run ID of the run that this analytical run belongs to
    /// </summary>
    public Guid InstrumentRunId { get; set; }

    /// <summary>
    /// The total judgment of the curve controls:
    /// - NotJudged: No curve controls in analytical run / action performed - No action needed
    /// - OK: Curve controls are OK - No action needed
    /// - Flagged: Curve controls are flagged - Action needed: New curve controls / new curve
    /// - NotOK: Curve controls are not OK - Action needed: New curve / select curve / skip calculations?
    /// - ReRunNotOK: Curve controls are still not OK - Action needed: New curve / select curve / skip calculations?
    /// After "action performed": Reset to NotJudged
    /// </summary>
    public CurveControlJudgmentResult CurveControlTotalJudgment { get; set; } = CurveControlJudgmentResult.NotJudged;

    public AnalyticalRunIssue QualityControlIssue { get; set; } = AnalyticalRunIssue.NoIssue;
    public AnalyticalRunIssue CurveControlIssue { get; set; } = AnalyticalRunIssue.NoIssue;
    public AnalyticalRunIssue TestIssue { get; set; } = AnalyticalRunIssue.NoIssue;
    public AnalyticalRunIssue CalibrationCurveIssue { get; set; } = AnalyticalRunIssue.NoIssue;

    #region Navigation properties

    /// <summary>
    /// The navigation property to the curve used for calculations
    /// That curve may be included in another analytical run
    /// </summary>
    [JsonIgnore]
    public virtual Curve? CalculationCurve { get; set; }

    [JsonIgnore]
    public virtual IList<TestResult> TestResults { get; set; }

    #endregion
}
