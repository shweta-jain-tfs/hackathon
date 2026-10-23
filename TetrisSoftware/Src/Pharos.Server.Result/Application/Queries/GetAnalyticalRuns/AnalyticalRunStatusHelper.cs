using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetAnalyticalRuns;

public static class AnalyticalRunStatusHelper
{
    public static List<IssueStatus> GetIssueStatuses(
        AnalyticalRunIssue calibrationCurveIssue, AnalyticalRunIssue curveControlIssue,
        AnalyticalRunIssue testIssue, AnalyticalRunIssue qualityControlIssue,
        RunStatus runStatus, ResultApprovalStatus approvalStatus,
        int testsPendingApproval,
        bool hasApprovedPendingReflex = false)
    {
        var issues = new List<IssueStatus>();

        if (calibrationCurveIssue == AnalyticalRunIssue.Error)
        {
            issues.Add(IssueStatus.CalibrationFailed);
        }
        if (curveControlIssue == AnalyticalRunIssue.Error)
        {
            issues.Add(IssueStatus.CurveControlFailed);
        }
        if (calibrationCurveIssue == AnalyticalRunIssue.Warning)
        {
            issues.Add(IssueStatus.CalibrationFlagged);
        }
        if (curveControlIssue == AnalyticalRunIssue.Warning)
        {
            issues.Add(IssueStatus.CurveControlFlagged);
        }
        if (qualityControlIssue == AnalyticalRunIssue.Error)
        {
            issues.Add(IssueStatus.QualityControlFailed);
        }
        if (qualityControlIssue == AnalyticalRunIssue.Warning)
        {
            issues.Add(IssueStatus.QualityControlFlagged);
        }
        if (testIssue == AnalyticalRunIssue.Warning)
        {
            issues.Add(IssueStatus.FailedTests);
        }

        // If the run is ready and not yet approved, it needs user action — report PendingApproval.
        // PendingApproval takes priority over ApprovedPendingReflex: if any test result is still
        // PendingApproval, ResultHelper ensures the run-level ApprovalStatus stays NotApproved.
        if (runStatus == RunStatus.Ready && approvalStatus == ResultApprovalStatus.NotApproved && testsPendingApproval > 0)
        {
            issues.Add(IssueStatus.PendingApproval);
        }

        if (hasApprovedPendingReflex)
        {
            issues.Add(IssueStatus.ApprovedPendingReflex);
        }

        return issues;
    }

    // If you change this method, please remember to update GetAnalyticalRunsQueryHandler where sorting is done.
    public static IssueStatus GetIssueStatus(
        AnalyticalRunIssue calibrationCurveIssue, AnalyticalRunIssue curveControlIssue,
        AnalyticalRunIssue testIssue, AnalyticalRunIssue qualityControlIssue,
        RunStatus runStatus, ResultApprovalStatus approvalStatus,
        int testsPendingApproval,
        bool hasApprovedPendingReflex = false)
    {
        var issues = GetIssueStatuses(
            calibrationCurveIssue,
            curveControlIssue,
            testIssue,
            qualityControlIssue,
            runStatus,
            approvalStatus,
            testsPendingApproval,
            hasApprovedPendingReflex);

        return issues.Count > 0 ? issues[0] : IssueStatus.NoIssue;
    }

    // If you change this method, please remember to update GetAnalyticalRunsQueryHandler where filtering and sorting is done on StateStatus based on RunStatus and ResultApprovalStatus.
    public static StateStatus GetStateStatus(RunStatus runStatus, ResultApprovalStatus approvalStatus)
    {
        switch (approvalStatus)
        {
            case ResultApprovalStatus.Rejected:
                return StateStatus.Rejected;
            case ResultApprovalStatus.Approved:
                return StateStatus.Approved;
        }

        if (runStatus == RunStatus.ReadyWithoutTests)
        {
            return StateStatus.Ready;
        }

        return runStatus == RunStatus.Processing ? StateStatus.Processing : StateStatus.Ready;
    }
}