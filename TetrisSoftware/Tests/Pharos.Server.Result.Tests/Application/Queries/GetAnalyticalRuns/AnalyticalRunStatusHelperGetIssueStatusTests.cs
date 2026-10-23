using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetAnalyticalRuns;

public class AnalyticalRunStatusHelperGetIssueStatusTests
{
    [Fact]
    public void GetIssueStatuses_WhenNoIssues_ReturnsEmptyCollection()
    {
        var issues = AnalyticalRunStatusHelper.GetIssueStatuses(
            calibrationCurveIssue: AnalyticalRunIssue.NoIssue,
            curveControlIssue: AnalyticalRunIssue.NoIssue,
            testIssue: AnalyticalRunIssue.NoIssue,
            qualityControlIssue: AnalyticalRunIssue.NoIssue,
            runStatus: RunStatus.Ready,
            approvalStatus: ResultApprovalStatus.NotApproved,
            testsPendingApproval: 0,
            hasApprovedPendingReflex: false);

        issues.Should().BeEmpty();
    }

    [Fact]
    public void GetIssueStatuses_WhenOneIssue_ReturnsThatIssue()
    {
        var issues = AnalyticalRunStatusHelper.GetIssueStatuses(
            calibrationCurveIssue: AnalyticalRunIssue.NoIssue,
            curveControlIssue: AnalyticalRunIssue.NoIssue,
            testIssue: AnalyticalRunIssue.Warning,
            qualityControlIssue: AnalyticalRunIssue.NoIssue,
            runStatus: RunStatus.Processing,
            approvalStatus: ResultApprovalStatus.NotApproved,
            testsPendingApproval: 0,
            hasApprovedPendingReflex: false);

        issues.Should().Equal(IssueStatus.FailedTests);
    }

    [Fact]
    public void GetIssueStatuses_WhenMultipleIssuesPresent_ReturnsAllInPriorityOrder()
    {
        var issues = AnalyticalRunStatusHelper.GetIssueStatuses(
            calibrationCurveIssue: AnalyticalRunIssue.Warning,
            curveControlIssue: AnalyticalRunIssue.Error,
            testIssue: AnalyticalRunIssue.Warning,
            qualityControlIssue: AnalyticalRunIssue.Warning,
            runStatus: RunStatus.Ready,
            approvalStatus: ResultApprovalStatus.NotApproved,
            testsPendingApproval: 1,
            hasApprovedPendingReflex: true);

        issues.Should().Equal(
            IssueStatus.CurveControlFailed,
            IssueStatus.CalibrationFlagged,
            IssueStatus.QualityControlFlagged,
            IssueStatus.FailedTests,
            IssueStatus.PendingApproval,
            IssueStatus.ApprovedPendingReflex);
    }

    [Fact]
    public void GetIssueStatuses_WhenApprovedAndPendingReflex_ReturnsApprovedPendingReflex()
    {
        var issues = AnalyticalRunStatusHelper.GetIssueStatuses(
            calibrationCurveIssue: AnalyticalRunIssue.NoIssue,
            curveControlIssue: AnalyticalRunIssue.NoIssue,
            testIssue: AnalyticalRunIssue.NoIssue,
            qualityControlIssue: AnalyticalRunIssue.NoIssue,
            runStatus: RunStatus.Ready,
            approvalStatus: ResultApprovalStatus.Approved,
            testsPendingApproval: 0,
            hasApprovedPendingReflex: true);

        issues.Should().Equal(IssueStatus.ApprovedPendingReflex);
    }

    [Fact]
    public void GetIssueStatus_WhenMultipleIssuesPresent_ReturnsHighestPriority()
    {
        var status = AnalyticalRunStatusHelper.GetIssueStatus(
            calibrationCurveIssue: AnalyticalRunIssue.Warning,
            curveControlIssue: AnalyticalRunIssue.Error,
            testIssue: AnalyticalRunIssue.Warning,
            qualityControlIssue: AnalyticalRunIssue.Warning,
            runStatus: RunStatus.Ready,
            approvalStatus: ResultApprovalStatus.NotApproved,
            testsPendingApproval: 1,
            hasApprovedPendingReflex: true);

        status.Should().Be(IssueStatus.CurveControlFailed);
    }

    [Fact]
    public void GetIssueStatus_WhenReadyNotApprovedAndHasPendingApprovalTests_ReturnsPendingApproval()
    {
        var status = AnalyticalRunStatusHelper.GetIssueStatus(
            calibrationCurveIssue: AnalyticalRunIssue.NoIssue,
            curveControlIssue: AnalyticalRunIssue.NoIssue,
            testIssue: AnalyticalRunIssue.NoIssue,
            qualityControlIssue: AnalyticalRunIssue.NoIssue,
            runStatus: RunStatus.Ready,
            approvalStatus: ResultApprovalStatus.NotApproved,
            testsPendingApproval: 1,
            hasApprovedPendingReflex: false);

        status.Should().Be(IssueStatus.PendingApproval);
    }

    [Fact]
    public void GetIssueStatus_WhenReadyNotApprovedAndNoPendingApprovalTests_DoesNotReturnPendingApproval()
    {
        var status = AnalyticalRunStatusHelper.GetIssueStatus(
            calibrationCurveIssue: AnalyticalRunIssue.NoIssue,
            curveControlIssue: AnalyticalRunIssue.NoIssue,
            testIssue: AnalyticalRunIssue.NoIssue,
            qualityControlIssue: AnalyticalRunIssue.NoIssue,
            runStatus: RunStatus.Ready,
            approvalStatus: ResultApprovalStatus.NotApproved,
            testsPendingApproval: 0,
            hasApprovedPendingReflex: false);

        status.Should().Be(IssueStatus.NoIssue);
    }

    [Fact]
    public void GetIssueStatus_WhenNoPendingApprovalButHasApprovedPendingReflex_ReturnsApprovedPendingReflex()
    {
        var status = AnalyticalRunStatusHelper.GetIssueStatus(
            calibrationCurveIssue: AnalyticalRunIssue.NoIssue,
            curveControlIssue: AnalyticalRunIssue.NoIssue,
            testIssue: AnalyticalRunIssue.NoIssue,
            qualityControlIssue: AnalyticalRunIssue.NoIssue,
            runStatus: RunStatus.Ready,
            approvalStatus: ResultApprovalStatus.Approved,
            testsPendingApproval: 0,
            hasApprovedPendingReflex: true);

        status.Should().Be(IssueStatus.ApprovedPendingReflex);
    }
}
