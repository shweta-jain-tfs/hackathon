namespace Pharos.Server.Request.Domain;

/// <summary>
/// Helper class for calculating the display state of a request based on test order statuses.
/// </summary>
public static class RequestStateCalculator
{
    /// <summary>
    /// Calculates the aggregate request state from its test order statuses.
    /// The calculation follows a priority-based approach where the highest priority status is returned.
    /// Priority order (top to bottom): NotStarted -> Started -> PartlyCompleted -> InReview -> Completed
    /// </summary>
    /// <param name="testOrderStates">Collection of test order states for the request.</param>
    /// <returns>The calculated request state based on the highest priority test order status.</returns>
    public static RequestState CalculateState(IEnumerable<TestOrderState> testOrderStates)
    {
        var statuses = testOrderStates.ToList();

        // No tests exist - default to NotStarted
        if (statuses.Count == 0)
        {
            return RequestState.NotStarted;
        }

        // Priority 1 (lowest): All tests are NotStarted
        if (statuses.All(x => x == TestOrderState.NotStarted))
        {
            return RequestState.NotStarted;
        }

        // Priority 2: Any test is in Started, Scheduled, UnableToStart, or Pipetted
        if (statuses.Any(x => x is TestOrderState.Scheduled 
                              or TestOrderState.UnableToStart 
                              or TestOrderState.Started 
                              or TestOrderState.Pipetted))
        {
            return RequestState.Started;
        }

        // Priority 3: Any test is NotStarted (but we know from above that not ALL are NotStarted)
        // This means some tests are in progress/completed while others haven't started
        if (statuses.Any(x => x == TestOrderState.NotStarted))
        {
            return RequestState.PartlyCompleted;
        }

        // Priority 4: Any test is in review states
        if (statuses.Any(x => x is TestOrderState.PendingBlank 
                              or TestOrderState.PendingCalibration 
                              or TestOrderState.PendingApproval
                              or TestOrderState.Aborted 
                              or TestOrderState.Flagged 
                              or TestOrderState.ApprovedPendingReflex))
        {
            return RequestState.InReview;
        }

        // Priority 5 (highest): All tests are in completed states (Approved, Rejected, or Reported)
        if (statuses.All(x => x is TestOrderState.Approved 
                              or TestOrderState.Rejected 
                              or TestOrderState.Reported))
        {
            return RequestState.Completed;
        }

        // Fallback - should not normally reach here
        return RequestState.NotStarted;
    }

    /// <summary>
    /// Calculates the aggregate request state from a request entity.
    /// </summary>
    /// <param name="request">The request entity.</param>
    /// <returns>The calculated request state based on the test orders.</returns>
    internal static RequestState CalculateState(Request request)
    {
        return CalculateState(request.TestOrders.Select(x => x.State));
    }
}
