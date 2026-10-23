namespace Pharos.Server.Request.Domain;

/// <summary>
/// Represents the state of a request based on its test order statuses.
/// This enum follows a priority-based calculation.
/// </summary>
/// <remarks>
/// The statuses are listed from lowest to highest priority (top to bottom).
/// When calculating the request state, the highest priority status among all test orders is used.
/// </remarks>
public enum RequestState
{
    /// <summary>
    /// All tests for the test orders have status "NotStarted".
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// Any test for the test orders has status "Scheduled", "UnableToStart", "Started", or "Pipetted".
    /// </summary>
    Started = 1,

    /// <summary>
    /// Any test for the test orders has status "NotStarted" (when other tests are in progress).
    /// </summary>
    PartlyCompleted = 2,

    /// <summary>
    /// Any test for the test orders has status "PendingBlank", "PendingCalibration", "PendingApproval", 
    /// "Aborted", "Flagged", or "ApprovedPendingReflex".
    /// </summary>
    InReview = 3,

    /// <summary>
    /// All tests for the test order have status "Approved", "Rejected", or "Reported".
    /// </summary>
    Completed = 4
}
