using Pharos.Common.Web.Components.Status;

namespace Pharos.Client.Common.App.Helpers;

public static class TestOrderStatusHelper
{
    public static StatusType GetStatusType(TestOrderState state, bool isActive)
    {
        if (!isActive && state is TestOrderState.NotStarted or TestOrderState.Scheduled or TestOrderState.UnableToStart)
        {
            return StatusType.WorkflowNotStarted;
        }

        return state switch
        {
            TestOrderState.NotStarted
                => StatusType.WorkflowNotStarted,

            TestOrderState.Aborted
                or TestOrderState.ApprovedPendingReflex
                or TestOrderState.Flagged
                or TestOrderState.PendingApproval
                or TestOrderState.PendingBlank
                or TestOrderState.PendingCalibration
                or TestOrderState.Pipetted
                or TestOrderState.Scheduled
                or TestOrderState.Started
                or TestOrderState.UnableToStart
                => StatusType.WorkflowInProgress,

            TestOrderState.Approved
                or TestOrderState.Rejected
                or TestOrderState.Reported
                => StatusType.WorkflowCompleted,

            _ => StatusType.None
        };
    }
}
