using MediatR;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;
using TestResult = Pharos.Server.Result.Domain.TestResult;

namespace Pharos.Server.Result.Application.Commands;

public class DeleteAnalyticalRunsCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenDeletedRunIsLastRunWithIssues_ShouldRemoveCalibrationIssueTaskCard()
    {
        // Arrange
        var analyticalRuns = new List<AnalyticalRun>
        {
            new()
            {
                Id = 1,
                InstrumentNumber = "SN001",
                CurveState = CurveState.Valid,
                ApprovalStatus = ResultApprovalStatus.NotApproved,
                CalibrationCurveIssue = AnalyticalRunIssue.Warning,
                CurveControlIssue = AnalyticalRunIssue.NoIssue
            }
        };

        var dbMock = new Mock<IResultDbContext>();
        dbMock.Setup(db => db.AnalyticalRuns).ReturnsDbSet(analyticalRuns);
        dbMock.Setup(db => db.TestResults).ReturnsDbSet(new List<TestResult>());
        dbMock.Setup(db => db.SampleTestResults).ReturnsDbSet(new List<SampleTestResult>());
        dbMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => analyticalRuns.RemoveAll(r => r.Id == 1))
            .ReturnsAsync(1);

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<RemoveTaskCardCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(MediatR.Unit.Value));

        var sut = new DeleteAnalyticalRunsCommandHandler(dbMock.Object, mediatorMock.Object);

        // Act
        await sut.Handle(new DeleteAnalyticalRunsCommand([1]), TestContext.Current.CancellationToken);

        // Assert
        dbMock.Verify(db => db.AnalyticalRuns.RemoveRange(It.IsAny<IEnumerable<AnalyticalRun>>()), Times.Once);
        dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        mediatorMock.Verify(m => m.Send(
            It.Is<RemoveTaskCardCommand>(c => c.CardType == TaskCardType.CalibrationIssue && c.InstrumentNumber == "SN001"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOtherRunStillHasIssues_ShouldKeepCalibrationIssueTaskCard()
    {
        // Arrange
        var analyticalRuns = new List<AnalyticalRun>
        {
            new()
            {
                Id = 1,
                InstrumentNumber = "SN001",
                CurveState = CurveState.Valid,
                ApprovalStatus = ResultApprovalStatus.NotApproved,
                CalibrationCurveIssue = AnalyticalRunIssue.Warning,
                CurveControlIssue = AnalyticalRunIssue.NoIssue
            },
            new()
            {
                Id = 2,
                InstrumentNumber = "SN001",
                CurveState = CurveState.Valid,
                ApprovalStatus = ResultApprovalStatus.NotApproved,
                CalibrationCurveIssue = AnalyticalRunIssue.Error,
                CurveControlIssue = AnalyticalRunIssue.NoIssue
            }
        };

        var dbMock = new Mock<IResultDbContext>();
        dbMock.Setup(db => db.AnalyticalRuns).ReturnsDbSet(analyticalRuns);
        dbMock.Setup(db => db.TestResults).ReturnsDbSet(new List<TestResult>());
        dbMock.Setup(db => db.SampleTestResults).ReturnsDbSet(new List<SampleTestResult>());
        dbMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => analyticalRuns.RemoveAll(r => r.Id == 1))
            .ReturnsAsync(1);

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<TaskCardCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(MediatR.Unit.Value));

        var sut = new DeleteAnalyticalRunsCommandHandler(dbMock.Object, mediatorMock.Object);

        // Act
        await sut.Handle(new DeleteAnalyticalRunsCommand([1]), TestContext.Current.CancellationToken);

        // Assert
        mediatorMock.Verify(m => m.Send(
            It.Is<TaskCardCommand>(c => c.CardType == TaskCardType.CalibrationIssue && c.InstrumentNumber == "SN001" && c.Rules.First().CardState == TaskCardState.Error),
            It.IsAny<CancellationToken>()), Times.Once);
        mediatorMock.Verify(m => m.Send(
            It.Is<RemoveTaskCardCommand>(c => c.CardType == TaskCardType.CalibrationIssue),
            It.IsAny<CancellationToken>()), Times.Never);
    }

}
