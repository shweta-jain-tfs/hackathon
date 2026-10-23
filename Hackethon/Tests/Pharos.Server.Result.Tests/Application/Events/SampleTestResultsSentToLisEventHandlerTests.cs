using MediatR;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Lis;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Events;

public class SampleTestResultsSentToLisEventHandlerTests
{
    [Fact]
    [Trait("ModuleRequirement", "MR_5870")]
    [Trait("ProductRequirement", "PR_1813")]
    public async Task Handle_WhenApprovedOrRejected_ShouldMarkAsReportedAndSetReportedToLisFlag()
    {
        // Arrange
        var approvedTestOrderId = Guid.NewGuid();
        var rejectedTestOrderId = Guid.NewGuid();
        const string instrument = "SN001";
        var run = new AnalyticalRun
        {
            Id = 1,
            InstrumentNumber = instrument,
            RunStatus = RunStatus.Ready
        };

        var approvedResult = new SampleTestResult
        {
            Id = 1,
            TestOrderId = approvedTestOrderId,
            AnalyticalRunId = run.Id,
            AnalyticalRun = run,
            Status = TestResultStatus.Approved,
            ApprovalStatus = ResultApprovalStatus.Approved
        };

        var rejectedResult = new SampleTestResult
        {
            Id = 2,
            TestOrderId = rejectedTestOrderId,
            AnalyticalRunId = run.Id,
            AnalyticalRun = run,
            Status = TestResultStatus.Rejected,
            ApprovalStatus = ResultApprovalStatus.Rejected
        };

        var dbMock = new Mock<IResultDbContext>();
        dbMock.Setup(x => x.AnalyticalRuns).ReturnsDbSet([run]);
        dbMock.Setup(x => x.SampleTestResults).ReturnsDbSet(new List<SampleTestResult>
        {
            approvedResult,
            rejectedResult
        });

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<IRequest<MediatR.Unit>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MediatR.Unit.Value);

        var sut = new SampleTestResultsSentToLisEventHandler(dbMock.Object, mediatorMock.Object);
        var notification = new SampleTestResultsSentToLisEvent([approvedTestOrderId, rejectedTestOrderId], LisChannelId: 1);

        // Act
        await sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        approvedResult.Status.Should().Be(TestResultStatus.Reported);
        rejectedResult.Status.Should().Be(TestResultStatus.Reported);
        approvedResult.IsReportedToLis.Should().BeTrue();
        rejectedResult.IsReportedToLis.Should().BeTrue();

        approvedResult.DomainEvents.OfType<TestResultUpdatedEvent>().Should().HaveCount(1);
        rejectedResult.DomainEvents.OfType<TestResultUpdatedEvent>().Should().HaveCount(1);

        dbMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5870")]
    [Trait("ProductRequirement", "PR_1813")]
    public async Task Handle_WhenApprovedPendingReflex_ShouldSetReportedToLisFlagWithoutChangingStatus()
    {
        // Arrange
        var testOrderId = Guid.NewGuid();
        const string instrument = "SN001";
        var run = new AnalyticalRun
        {
            Id = 1,
            InstrumentNumber = instrument,
            RunStatus = RunStatus.Ready
        };

        var pendingReflexResult = new SampleTestResult
        {
            Id = 1,
            TestOrderId = testOrderId,
            AnalyticalRunId = run.Id,
            AnalyticalRun = run,
            Status = TestResultStatus.ApprovedPendingReflex,
            ApprovalStatus = ResultApprovalStatus.Approved
        };

        var dbMock = new Mock<IResultDbContext>();
        dbMock.Setup(x => x.AnalyticalRuns).ReturnsDbSet([run]);
        dbMock.Setup(x => x.SampleTestResults).ReturnsDbSet(new List<SampleTestResult>
        {
            pendingReflexResult
        });

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<IRequest<MediatR.Unit>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MediatR.Unit.Value);

        var sut = new SampleTestResultsSentToLisEventHandler(dbMock.Object, mediatorMock.Object);
        var notification = new SampleTestResultsSentToLisEvent([testOrderId], LisChannelId: 1);

        // Act
        await sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        pendingReflexResult.Status.Should().Be(TestResultStatus.ApprovedPendingReflex);
        pendingReflexResult.IsReportedToLis.Should().BeTrue();
        pendingReflexResult.DomainEvents.OfType<TestResultUpdatedEvent>().Should().HaveCount(1);

        dbMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5870")]
    [Trait("ProductRequirement", "PR_1813")]
    public async Task Handle_WhenStatusIsNotApprovedOrRejected_ShouldNotChangeStatusOrAddUpdatedDomainEvent()
    {
        // Arrange
        var testOrderId = Guid.NewGuid();

        var pendingResult = new SampleTestResult
        {
            Id = 3,
            TestOrderId = testOrderId,
            Status = TestResultStatus.PendingApproval,
            ApprovalStatus = ResultApprovalStatus.NotApproved
        };

        var dbMock = new Mock<IResultDbContext>();
        dbMock.Setup(x => x.SampleTestResults).ReturnsDbSet(new List<SampleTestResult>
        {
            pendingResult
        });

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<IRequest<MediatR.Unit>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MediatR.Unit.Value);

        var sut = new SampleTestResultsSentToLisEventHandler(dbMock.Object, mediatorMock.Object);
        var notification = new SampleTestResultsSentToLisEvent([testOrderId], LisChannelId: 1);

        // Act
        await sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        pendingResult.Status.Should().Be(TestResultStatus.PendingApproval);
        pendingResult.DomainEvents.OfType<TestResultUpdatedEvent>().Should().BeEmpty();

        dbMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
