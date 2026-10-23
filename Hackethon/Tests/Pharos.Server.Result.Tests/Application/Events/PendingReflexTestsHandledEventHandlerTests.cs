using MediatR;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Events;

public class PendingReflexTestsHandledEventHandlerTests
{
    [Fact]
    public async Task Handle_WhenPendingReflexResultIsNotReportedToLis_ShouldSetResultToApproved()
    {
        // Arrange
        const string instrument = "SN001";
        var testOrderId = Guid.NewGuid();
        var run = new AnalyticalRun
        {
            Id = 1,
            InstrumentNumber = instrument,
            RunStatus = RunStatus.Ready
        };

        var result = new SampleTestResult
        {
            Id = 1,
            TestOrderId = testOrderId,
            AnalyticalRunId = run.Id,
            AnalyticalRun = run,
            Status = TestResultStatus.ApprovedPendingReflex,
            IsReportedToLis = false,
            ApprovalStatus = ResultApprovalStatus.NotApproved
        };

        var dbMock = new Mock<IResultDbContext>();
        dbMock.Setup(x => x.SampleTestResults).ReturnsDbSet([result]);
        dbMock.Setup(x => x.AnalyticalRuns).ReturnsDbSet([run]);
        dbMock.Setup(x => x.HasChanges()).Returns(true);

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mediatorMock
            .Setup(m => m.Send(It.IsAny<IRequest<MediatR.Unit>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MediatR.Unit.Value);

        var sut = new PendingReflexTestsHandledEventHandler(dbMock.Object, mediatorMock.Object);
        var notification = new PendingReflexTestsHandledEvent([testOrderId]);

        // Act
        await sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        result.ApprovalStatus.Should().Be(ResultApprovalStatus.Approved);
        result.Status.Should().Be(TestResultStatus.Approved);
        result.DomainEvents.OfType<TestResultUpdatedEvent>().Should().HaveCount(1);
        dbMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        mediatorMock.Verify(x => x.Publish(
            It.Is<TestResultStatusChangedEvent>(e => e.TestResults.SequenceEqual(new[] { result })),
            It.IsAny<CancellationToken>()), Times.Once);
        mediatorMock.Verify(m => m.Send(
            It.Is<RemoveTaskCardCommand>(c => c.CardType == TaskCardType.ResultReviewPending
                                              && c.InstrumentNumber == instrument),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPendingReflexResultIsReportedToLis_ShouldSetResultToReported()
    {
        // Arrange
        const string instrument = "SN001";
        var testOrderId = Guid.NewGuid();
        var run = new AnalyticalRun
        {
            Id = 2,
            InstrumentNumber = instrument,
            RunStatus = RunStatus.Ready
        };

        var result = new SampleTestResult
        {
            Id = 2,
            TestOrderId = testOrderId,
            AnalyticalRunId = run.Id,
            AnalyticalRun = run,
            Status = TestResultStatus.ApprovedPendingReflex,
            IsReportedToLis = true,
            ApprovalStatus = ResultApprovalStatus.Approved
        };

        var dbMock = new Mock<IResultDbContext>();
        dbMock.Setup(x => x.SampleTestResults).ReturnsDbSet([result]);
        dbMock.Setup(x => x.AnalyticalRuns).ReturnsDbSet([run]);
        dbMock.Setup(x => x.HasChanges()).Returns(true);

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mediatorMock
            .Setup(m => m.Send(It.IsAny<IRequest<MediatR.Unit>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MediatR.Unit.Value);

        var sut = new PendingReflexTestsHandledEventHandler(dbMock.Object, mediatorMock.Object);
        var notification = new PendingReflexTestsHandledEvent([testOrderId]);

        // Act
        await sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        result.ApprovalStatus.Should().Be(ResultApprovalStatus.Approved);
        result.Status.Should().Be(TestResultStatus.Reported);
        result.DomainEvents.OfType<TestResultUpdatedEvent>().Should().HaveCount(1);
        dbMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        mediatorMock.Verify(x => x.Publish(
            It.Is<TestResultStatusChangedEvent>(e => e.TestResults.SequenceEqual(new[] { result })),
            It.IsAny<CancellationToken>()), Times.Once);
        mediatorMock.Verify(m => m.Send(
            It.Is<RemoveTaskCardCommand>(c => c.CardType == TaskCardType.ResultReviewPending
                                              && c.InstrumentNumber == instrument),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
