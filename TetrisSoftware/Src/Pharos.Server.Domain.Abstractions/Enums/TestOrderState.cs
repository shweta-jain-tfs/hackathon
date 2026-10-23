namespace Pharos.Server.Domain.Enums;

/// <summary>
/// Lists all available states for a test on a sample.
/// </summary>
/// <remarks>
/// Do not modify numbers for the existing enum entries (they are persisted).
/// </remarks>
public enum TestOrderState
{
    /// <summary>
    /// The initial test order state.
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// Sent to an instrument for processing (may spend some time in the instrument before it starts processing).
    /// </summary>
    Scheduled = 1,

    /// <summary>
    /// The instrument is not able to start the test order because either reagents or sample is missing.
    /// </summary>
    UnableToStart = 2,

    /// <summary>
    /// ImmunoCAPs / EliA wells have been dispensed for this test, but the sample has not been aspirated yet.
    /// </summary>
    Started = 3,

    /// <summary>
    /// The sample aspiration for all replicates has been complete. This state stays until all replicates are measured or aborted.
    /// </summary>
    // ReSharper disable once IdentifierTypo
    Pipetted = 4,

    /// <summary>
    /// All replicates of the test order have been measured, but at least one replicate has
    /// no response value (reagent blank is still missing).
    /// </summary>
    PendingBlank = 12,

    /// <summary>
    /// All replicates of the test order have been measured and have response values, but
    /// the concentration is not calculated because the calibration curve is missing
    /// (or the user has decided to re-run the calibration and results were reset).
    /// </summary>
    PendingCalibration = 13,

    /// <summary>
    /// All replicates of the test order have been measured and have concentration.
    /// The result is waiting for approval.
    /// </summary>
    PendingApproval = 5,

    /// <summary>
    /// The test result is measured, but there are errors or
    /// the concentration state is doubtful (it can still be approved).
    /// </summary>
    Flagged = 7,

    /// <summary>
    /// The test has been aborted (e.g. due to an error).
    /// The result for this test order has the "low RU" value.
    /// </summary>
    Aborted = 8,

    /// <summary>
    /// The test result for this test order is rejected (as invalid).
    /// </summary>
    Rejected = 9,

    /// <summary>
    /// The result for this test order is approved (as valid).
    /// </summary>
    Approved = 10,

    /// <summary>
    /// The result for this test order is approved (as valid) but has pending reflex tests waiting for the user action.
    /// </summary>
    ApprovedPendingReflex = 14,

    /// <summary>
    /// The result for this test order is approved (or rejected) and reported to LIS (or printed).
    /// </summary>
    Reported = 11
}