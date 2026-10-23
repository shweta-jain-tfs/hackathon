using Pharos.Server.Application.Request;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Application.Helpers;

internal static class SampleTubeExtensions
{
    private static readonly HashSet<TestOrderState> _allCompletedStatuses =
    [
        TestOrderState.Pipetted, TestOrderState.PendingApproval,
       TestOrderState.Flagged, TestOrderState.Aborted, TestOrderState.Rejected, TestOrderState.Approved,
       TestOrderState.Reported
    ];

    public static bool UpdateTubeCompletionStatus(this SampleTube tube, List<ITestOrder> testOrderStates)
    {
        if (tube.TubeId == null || tube.Status is SampleTubeStatus.NotIdentified or SampleTubeStatus.FollowUpReflex)
        {
            // TODO: check if the above condition is correct  
            return false;
        }

        if (tube.InternalError is SampleTubeError.MissingTestOrders || tube.InternalError is SampleTubeError.MethodsNotActive)
        {
            tube.InternalError = SampleTubeError.None;
        }

        tube.Status = GetCompletionStatus(testOrderStates);
        return true;
    }

    private static SampleTubeStatus GetCompletionStatus(List<ITestOrder> testOrderStates)
    {
        var hasReflexTestToBeAdded = testOrderStates.Exists(x=> x.HasReflexTests);
        if (hasReflexTestToBeAdded)
        {
            return SampleTubeStatus.InProgress;
        }

        var allPipetted = testOrderStates.TrueForAll(x => _allCompletedStatuses.Contains(x.State));
        if (allPipetted)
        {
            return SampleTubeStatus.Completed;
        }

        var anyPipetted = testOrderStates.Exists(x => _allCompletedStatuses.Contains(x.State));
        var anyNotStarted = testOrderStates.Exists(x => x.State == TestOrderState.NotStarted);
        var allNotStartedOrPipetted = testOrderStates.TrueForAll(x => x.State == TestOrderState.NotStarted || _allCompletedStatuses.Contains(x.State));

        if (anyNotStarted && anyPipetted && allNotStartedOrPipetted)
        {
            return SampleTubeStatus.PartlyCompleted;
        }

        return SampleTubeStatus.InProgress;
    }
}
