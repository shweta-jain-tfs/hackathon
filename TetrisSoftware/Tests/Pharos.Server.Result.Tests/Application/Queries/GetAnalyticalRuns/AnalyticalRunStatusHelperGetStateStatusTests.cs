using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetAnalyticalRuns;

public class AnalyticalRunStatusHelperGetStateStatusTests
{
    // ApprovalStatus.Rejected always wins, regardless of RunStatus
    [Theory]
    [InlineData(RunStatus.Ready)]
    [InlineData(RunStatus.Aborted)]
    [InlineData(RunStatus.Processing)]
    [InlineData(RunStatus.ReadyWithoutTests)]
    public void GetStateStatus_WhenRejected_ReturnsRejected(RunStatus runStatus)
    {
        AnalyticalRunStatusHelper.GetStateStatus(runStatus, ResultApprovalStatus.Rejected)
            .Should().Be(StateStatus.Rejected);
    }

    // ApprovalStatus.Approved always wins, regardless of RunStatus
    [Theory]
    [InlineData(RunStatus.Ready)]
    [InlineData(RunStatus.Aborted)]
    [InlineData(RunStatus.Processing)]
    [InlineData(RunStatus.ReadyWithoutTests)]
    public void GetStateStatus_WhenApproved_ReturnsApproved(RunStatus runStatus)
    {
        AnalyticalRunStatusHelper.GetStateStatus(runStatus, ResultApprovalStatus.Approved)
            .Should().Be(StateStatus.Approved);
    }

    // RunStatus determines state when ApprovalStatus has not already resolved it
    [Fact]
    public void GetStateStatus_WhenNotApprovedAndProcessing_ReturnsProcessing()
    {
        AnalyticalRunStatusHelper.GetStateStatus(RunStatus.Processing, ResultApprovalStatus.NotApproved)
            .Should().Be(StateStatus.Processing);
    }

    [Fact]
    public void GetStateStatus_WhenNotApprovedAndReady_ReturnsReady()
    {
        AnalyticalRunStatusHelper.GetStateStatus(RunStatus.Ready, ResultApprovalStatus.NotApproved)
            .Should().Be(StateStatus.Ready);
    }

    [Fact]
    public void GetStateStatus_WhenNotApprovedAndAborted_ReturnsReady()
    {
        // Aborted is not a distinct StateStatus — it maps to Ready
        AnalyticalRunStatusHelper.GetStateStatus(RunStatus.Aborted, ResultApprovalStatus.NotApproved)
            .Should().Be(StateStatus.Ready);
    }
}
