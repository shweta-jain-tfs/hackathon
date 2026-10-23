using MediatR;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Request;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Events;

public class RequestUpdatedEventHandlerTests
{
    [Fact]
    public async Task Handle_WhenRequestUpdatedToApprovedPendingReflex_ShouldSetResultStatusToApprovedPendingReflex()
    {
        // Arrange
        var testOrderUid = Guid.NewGuid();
        const string instrument = "SN001";
        var run = new AnalyticalRun
        {
            Id = 1,
            InstrumentNumber = instrument,
            RunStatus = RunStatus.Ready
        };

        var sampleTestResult = new SampleTestResult
        {
            Id = 1,
            TestOrderId = testOrderUid,
            AnalyticalRunId = run.Id,
            AnalyticalRun = run,
            Status = TestResultStatus.Approved,
            ApprovalStatus = ResultApprovalStatus.Approved
        };

        var dbMock = new Mock<IResultDbContext>();
        dbMock.Setup(x => x.AnalyticalRuns).ReturnsDbSet([run]);
        dbMock.Setup(x => x.SampleTestResults).ReturnsDbSet(new List<SampleTestResult> { sampleTestResult });
        dbMock.Setup(x => x.HasChanges()).Returns(true);

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<IRequest<MediatR.Unit>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MediatR.Unit.Value);

        var sut = new RequestUpdatedEventHandler(dbMock.Object, mediatorMock.Object);

        var notification = new RequestUpdatedEvent(
            RequestId: 1,
            RequestUid: new Pharos.Server.Domain.Entities.RequestUid { SampleId = "S1", RequestCreatedDate = DateTimeOffset.UtcNow },
            TestOrders:
            [
                new TestOrderStub(testOrderUid, TestOrderState.ApprovedPendingReflex)
            ]);

        // Act
        await sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        sampleTestResult.Status.Should().Be(TestResultStatus.ApprovedPendingReflex);
        dbMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRequestUpdatedToApproved_ShouldSetPendingReflexResultStatusBackToApproved()
    {
        // Arrange
        var testOrderUid = Guid.NewGuid();
        const string instrument = "SN001";
        var run = new AnalyticalRun
        {
            Id = 1,
            InstrumentNumber = instrument,
            RunStatus = RunStatus.Ready
        };

        var sampleTestResult = new SampleTestResult
        {
            Id = 2,
            TestOrderId = testOrderUid,
            AnalyticalRunId = run.Id,
            AnalyticalRun = run,
            Status = TestResultStatus.ApprovedPendingReflex,
            ApprovalStatus = ResultApprovalStatus.Approved
        };

        var dbMock = new Mock<IResultDbContext>();
        dbMock.Setup(x => x.AnalyticalRuns).ReturnsDbSet([run]);
        dbMock.Setup(x => x.SampleTestResults).ReturnsDbSet(new List<SampleTestResult> { sampleTestResult });
        dbMock.Setup(x => x.HasChanges()).Returns(true);

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<IRequest<MediatR.Unit>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MediatR.Unit.Value);

        var sut = new RequestUpdatedEventHandler(dbMock.Object, mediatorMock.Object);

        var notification = new RequestUpdatedEvent(
            RequestId: 1,
            RequestUid: new Pharos.Server.Domain.Entities.RequestUid { SampleId = "S2", RequestCreatedDate = DateTimeOffset.UtcNow },
            TestOrders:
            [
                new TestOrderStub(testOrderUid, TestOrderState.Approved)
            ]);

        // Act
        await sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        sampleTestResult.Status.Should().Be(TestResultStatus.Approved);
        dbMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed record TestOrderStub(Guid Uid, TestOrderState State) : ITestOrder
    {
        public string? TubeId => null;
        public bool HasReflexTests => false;
        public bool IsActive => true;
        public int? LisChannelId => null;
        public EntityState EntityState => EntityState.Unchanged;
    }
}
