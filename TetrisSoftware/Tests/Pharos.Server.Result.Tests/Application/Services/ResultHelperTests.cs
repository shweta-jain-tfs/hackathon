using MediatR;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Services;

public class ResultHelperTests
{
    private const string _instrumentNumber = "SN001";

    [Fact]
    [Trait("ModuleRequirement", "MR_4108")]
    public async Task UpdateTaskCardState_WhenNoRunHasTestIssues_ShouldRemoveResultsIssueTaskCard()
    {
        var dbContextMock = CreateDbContextMock([], []);
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<RemoveTaskCardCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        await ResultHelper.UpdateTaskCardState(
            mediatorMock.Object,
            dbContextMock.Object,
            _instrumentNumber,
            [TaskCardType.ResultsIssue],
            TestContext.Current.CancellationToken);

        mediatorMock.Verify(m => m.Send(
            It.Is<RemoveTaskCardCommand>(command =>
                command.CardType == TaskCardType.ResultsIssue &&
                command.InstrumentNumber == _instrumentNumber),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5806")]
    public async Task UpdateTaskCardState_WhenResultsIssueIsNotIncluded_ShouldNotUpdateReviewPendingTaskCard()
    {
        var run = new AnalyticalRun
        {
            Id = 13,
            InstrumentNumber = _instrumentNumber,
            RunStatus = RunStatus.Ready,
            ApprovalStatus = ResultApprovalStatus.NotApproved,
            CalibrationCurveIssue = AnalyticalRunIssue.Warning
        };

        var sampleResults = new List<SampleTestResult>
        {
            new()
            {
                AnalyticalRunId = run.Id,
                AnalyticalRun = run,
                Status = TestResultStatus.PendingApproval
            }
        };

        var dbContextMock = CreateDbContextMock([run], sampleResults);
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<TaskCardCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));
        mediatorMock
            .Setup(m => m.Send(It.IsAny<RemoveTaskCardCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        await ResultHelper.UpdateTaskCardState(
            mediatorMock.Object,
            dbContextMock.Object,
            _instrumentNumber,
            [TaskCardType.CalibrationIssue],
            TestContext.Current.CancellationToken);

        mediatorMock.Verify(m => m.Send(
            It.Is<TaskCardCommand>(command => command.CardType == TaskCardType.ResultReviewPending),
            It.IsAny<CancellationToken>()), Times.Never);

        mediatorMock.Verify(m => m.Send(
            It.Is<RemoveTaskCardCommand>(command => command.CardType == TaskCardType.ResultReviewPending),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5806")]
    public async Task UpdateTaskCardState_WhenReadyRunHasPendingApprovalResult_ShouldCreateReviewPendingTaskCard()
    {
        var run = new AnalyticalRun
        {
            Id = 11,
            InstrumentNumber = _instrumentNumber,
            RunStatus = RunStatus.Ready,
            ApprovalStatus = ResultApprovalStatus.NotApproved
        };
        var sampleResults = new List<SampleTestResult>
        {
            new()
            {
                AnalyticalRunId = run.Id,
                AnalyticalRun = run,
                Status = TestResultStatus.PendingApproval
            }
        };

        var dbContextMock = CreateDbContextMock([run], sampleResults);
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<TaskCardCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));
        mediatorMock
            .Setup(m => m.Send(It.IsAny<RemoveTaskCardCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        await ResultHelper.UpdateTaskCardState(
            mediatorMock.Object,
            dbContextMock.Object,
            _instrumentNumber,
            [TaskCardType.ResultsIssue],
            TestContext.Current.CancellationToken);

        mediatorMock.Verify(m => m.Send(
            It.Is<TaskCardCommand>(command =>
                command.CardType == TaskCardType.ResultReviewPending
                && command.InstrumentNumber == _instrumentNumber
                && command.Rules.Single().CardState == TaskCardState.Information),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskCardState_WhenNoReadyRunHasPendingApprovalResult_ShouldRemoveReviewPendingTaskCard()
    {
        var run = new AnalyticalRun
        {
            Id = 12,
            InstrumentNumber = _instrumentNumber,
            RunStatus = RunStatus.Ready,
            ApprovalStatus = ResultApprovalStatus.NotApproved
        };
        var sampleResults = new List<SampleTestResult>
        {
            new()
            {
                AnalyticalRunId = run.Id,
                AnalyticalRun = run,
                Status = TestResultStatus.Approved
            }
        };

        var dbContextMock = CreateDbContextMock([run], sampleResults);
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<TaskCardCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));
        mediatorMock
            .Setup(m => m.Send(It.IsAny<RemoveTaskCardCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        await ResultHelper.UpdateTaskCardState(
            mediatorMock.Object,
            dbContextMock.Object,
            _instrumentNumber,
            [TaskCardType.ResultsIssue],
            TestContext.Current.CancellationToken);

        mediatorMock.Verify(m => m.Send(
            It.Is<RemoveTaskCardCommand>(command =>
                command.CardType == TaskCardType.ResultReviewPending
                && command.InstrumentNumber == _instrumentNumber),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_4107")]
    public async Task UpdateTaskCardState_WhenFrozenRunHasTestIssue_ShouldCreateResultsIssueTaskCard()
    {
        var runs = new List<AnalyticalRun>
        {
            new()
            {
                InstrumentNumber = _instrumentNumber,
                CurveState = CurveState.Frozen,
                ApprovalStatus = ResultApprovalStatus.NotApproved,
                TestIssue = AnalyticalRunIssue.Warning
            }
        };
        var dbContextMock = CreateDbContextMock(runs, []);
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<TaskCardCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Unit.Value));

        await ResultHelper.UpdateTaskCardState(
            mediatorMock.Object,
            dbContextMock.Object,
            _instrumentNumber,
            [TaskCardType.ResultsIssue],
            TestContext.Current.CancellationToken);

        mediatorMock.Verify(m => m.Send(
            It.Is<TaskCardCommand>(command =>
                command.CardType == TaskCardType.ResultsIssue &&
                command.InstrumentNumber == _instrumentNumber &&
                command.Rules.Single().CardState == TaskCardState.Warning),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IResultDbContext> CreateDbContextMock(List<AnalyticalRun> runs, List<SampleTestResult> sampleTestResults)
    {
        var dbContextMock = new Mock<IResultDbContext>();
        dbContextMock.Setup(dbContext => dbContext.AnalyticalRuns).ReturnsDbSet(runs);
        dbContextMock.Setup(dbContext => dbContext.SampleTestResults).ReturnsDbSet(sampleTestResults);
        return dbContextMock;
    }
}
