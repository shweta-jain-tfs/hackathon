namespace Pharos.Server.Domain.Enums;

/// <summary>
/// Lists all available statuses for a test result.
/// </summary>
/// <remarks>
/// Do not modify numbers for the existing enum entries (they are persisted).
/// </remarks>
public enum TestResultStatus
{
    /// <summary>
    /// The test order has more than one replicate, but at least one replicate is not measured yet.
    /// </summary>
    Processing = 0,

    /// <summary>
    /// All replicates of the test order have been measured, but at least one replicate has
    /// no response value (reagent blank is still missing).
    /// </summary>
    PendingBlank = 10,

    /// <summary>
    /// All replicates of the test order have been measured and have response values, but
    /// the concentration is not calculated because the calibration curve is missing
    /// (or the user has decided to re-run the calibration and results were reset).
    /// </summary>
    PendingCalibration = 11,

    /// <summary>
    /// All replicates of the test order have been measured and have concentration.
    /// Sample test orders use the <see cref="PendingApproval"/> status.
    /// </summary>
    Ready = 1,

    /// <summary>
    /// This is a <see cref="Ready"/> status for sample test orders.
    /// </summary>
    PendingApproval = 2,

    /// <summary>
    /// The test result is measured, but there are errors or
    /// the concentration status is doubtful (it can still be approved).
    /// </summary>
    Flagged = 4,

    /// <summary>
    /// At least one replicate or the test order is aborted (e.g. due to an error) or
    /// at least one replicate has the "low RU" value.
    /// </summary>
    Aborted = 5,

    /// <summary>
    /// The test result is rejected (as invalid).
    /// </summary>
    Rejected = 6,

    /// <summary>
    /// The result is approved (as valid).
    /// </summary>
    Approved = 7,

    /// <summary>
    /// The result is approved (as valid) but has pending reflex tests waiting for the user action.
    /// </summary>
    ApprovedPendingReflex = 9,

    /// <summary>
    /// The result is approved (or rejected) and reported to LIS (or printed).
    /// </summary>
    Reported = 8
}