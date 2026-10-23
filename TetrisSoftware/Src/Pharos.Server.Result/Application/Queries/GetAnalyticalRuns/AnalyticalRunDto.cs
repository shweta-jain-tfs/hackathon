namespace Pharos.Server.Result.Application.Queries.GetAnalyticalRuns;

public class AnalyticalRunDto
{
    public int Id { get; set; }

    /// <summary>
    /// The name of the method used in this analytical run.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// The run start time stamp.
    /// </summary>
    public DateTimeOffset StartDateTime { get; set; }

    /// <summary>
    /// The run finish time stamp.
    /// </summary>
    public DateTimeOffset? EndDateTime { get; set; }

    /// <summary>
    /// The instrument serial number.
    /// </summary>
    public string InstrumentName { get; set; } = string.Empty;

    /// <summary>
    /// Issue status to show in GUI. 
    /// </summary>
    public IssueStatus IssueStatus { get; set; }

    /// <summary>
    /// All applicable issue statuses to show in GUI.
    /// </summary>
    public ICollection<IssueStatus> Issues { get; set; } = [];

    /// <summary>
    /// State status to show in GUI. 
    /// </summary>
    public StateStatus StateStatus { get; set; }

    /// <summary>
    /// If the run is created with custom run id, this field will be populated.
    /// Value will only be set if the user is RnD
    /// </summary>
    public string CustomRunId { get; set; } = string.Empty;

    public int NumberOfTests { get; set; }
}

public enum CalculationStateStatus
{
    Valid = 0,
    WaitingForCurveControl = 1,
    WaitingForCurve = 2,
    Invalid = 3,
    Frozen = 4
}

// Please notice that the order of the enum values is important for sorting in the UI. The order is from high to low.
public enum IssueStatus
{
    CalibrationFailed,   //(Major error)
    CurveControlFailed,  //(Major error)
    CalibrationFlagged,  //(Minor error)
    CurveControlFlagged, //(Minor error)
    QualityControlFailed,   //(Minor error)
    QualityControlFlagged,  //(Minor error)
    FailedTests,              //(Minor error)
    PendingApproval,          //(Run state ready)
    ApprovedPendingReflex,    //(Run state approved but has pending reflex tests waiting for user action)
    NoIssue
}

// Please notice that the order of the enum values is important for sorting in the UI. The order is from high to low.
public enum StateStatus
{
    Rejected,    // (Run state ready, all tests rejected)
    Approved,    // (Run state ready, all tests approved or rejected)
    Processing,  // (Run state processing)
    Ready        // (Run state ready, not all test have been approved/rejected/reported)
}
