using Microsoft.Extensions.Options;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Request.Application.Commands.DeleteRequest;
using TestOrder = Pharos.Server.Request.Domain.TestOrder;
using TestOrderState = Pharos.Server.Domain.Enums.TestOrderState;

namespace Pharos.Server.Request.Application.Events;

/// <summary>
/// Unit tests for TestResultsSentToLisEventHandler.
/// </summary>
public class SampleTestResultsSentToLisEventHandlerTests
{
    private readonly Mock<IRequestDbContext> _dbContextMock;
    private readonly Mock<IOptionsSnapshot<LisSettings>> _lisSettingsMock;
    private readonly LisSettings _lisSettings;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly SampleTestResultsSentToLisEventHandler _sut;

    public SampleTestResultsSentToLisEventHandlerTests()
    {
        _dbContextMock = new Mock<IRequestDbContext>();
        _lisSettingsMock = new Mock<IOptionsSnapshot<LisSettings>>();
        _lisSettings = new LisSettings();
        _lisSettingsMock.SetupGet(x => x.Value).Returns(_lisSettings);
        _mediatorMock = new Mock<IMediator>();
        _sut = new SampleTestResultsSentToLisEventHandler(
            _dbContextMock.Object,
            _lisSettingsMock.Object,
            _mediatorMock.Object);
    }

    #region Helper Methods

    private static TestOrder CreateTestOrder(Guid uid, int requestId, TestOrderState state)
    {
        return new TestOrder
        {
            Uid = uid,
            RequestId = requestId,
            State = state,
            TestName = "IgE",
            MethodName = "Im-F"
        };
    }

    private static Domain.Request CreateRequest(int id, params TestOrder[] testOrders)
    {
        var request = new Domain.Request
        {
            Id = id,
            SampleId = $"SAMPLE{id}",
            TestOrders = testOrders.ToList()
        };
        foreach (var testOrder in testOrders)
        {
            testOrder.RequestId = id;
        }
        return request;
    }

    #endregion

    #region Handle - No Test Orders Found

    [Fact]
    public async Task Handle_WhenNoTestOrdersFound_ShouldReturnEarlyWithoutUpdates()
    {
        // Arrange
        var testOrderId = Guid.NewGuid();
        var notification = new SampleTestResultsSentToLisEvent([testOrderId], LisChannelId: 1);

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet(new List<TestOrder>());
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(new List<Domain.Request>());
        ConfigureDeleteSamplesAfterExport(channelId: 1, shouldDelete: false);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mediatorMock.Verify(x => x.Send(It.IsAny<DeleteRequestCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Handle - State Updates

    [Fact]
    [Trait("ModuleRequirement", "MR_5870")]
    [Trait("ProductRequirement", "PR_1813")]
    public async Task Handle_WhenTestOrderIsApproved_ShouldUpdateToReported()
    {
        // Arrange
        var testOrderId = Guid.NewGuid();
        var testOrder = CreateTestOrder(testOrderId, 1, TestOrderState.Approved);
        var request = CreateRequest(1, testOrder);

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet([testOrder]);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet([request]);
        ConfigureDeleteSamplesAfterExport(channelId: 1, shouldDelete: false);

        var notification = new SampleTestResultsSentToLisEvent([testOrderId], LisChannelId: 1);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        testOrder.State.Should().Be(TestOrderState.Reported);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5870")]
    [Trait("ProductRequirement", "PR_1813")]
    public async Task Handle_WhenTestOrderIsRejected_ShouldUpdateToReported()
    {
        // Arrange
        var testOrderId = Guid.NewGuid();
        var testOrder = CreateTestOrder(testOrderId, 1, TestOrderState.Rejected);
        var request = CreateRequest(1, testOrder);

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet([testOrder]);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet([request]);
        ConfigureDeleteSamplesAfterExport(channelId: 1, shouldDelete: false);

        var notification = new SampleTestResultsSentToLisEvent([testOrderId], LisChannelId: 1);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        testOrder.State.Should().Be(TestOrderState.Reported);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(TestOrderState.NotStarted)]
    [InlineData(TestOrderState.Started)]
    [InlineData(TestOrderState.Pipetted)]
    [InlineData(TestOrderState.Scheduled)]
    [InlineData(TestOrderState.PendingApproval)]
    [InlineData(TestOrderState.Reported)]
    [Trait("ModuleRequirement", "MR_5870")]
    [Trait("ProductRequirement", "PR_1813")]
    public async Task Handle_WhenTestOrderIsNotApprovedOrRejected_ShouldNotUpdateState(TestOrderState initialState)
    {
        // Arrange
        var testOrderId = Guid.NewGuid();
        var testOrder = CreateTestOrder(testOrderId, 1, initialState);
        var request = CreateRequest(1, testOrder);

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet([testOrder]);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet([request]);
        ConfigureDeleteSamplesAfterExport(channelId: 1, shouldDelete: false);

        var notification = new SampleTestResultsSentToLisEvent([testOrderId], LisChannelId: 1);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        testOrder.State.Should().Be(initialState);
    }

    #endregion

    #region Handle - Multiple Test Orders

    [Fact]
    [Trait("ModuleRequirement", "MR_5870")]
    [Trait("ProductRequirement", "PR_1813")]
    public async Task Handle_WhenMultipleTestOrdersFromSameRequest_ShouldUpdateAllMatchingOrders()
    {
        // Arrange
        var testOrderId1 = Guid.NewGuid();
        var testOrderId2 = Guid.NewGuid();
        var testOrder1 = CreateTestOrder(testOrderId1, 1, TestOrderState.Approved);
        var testOrder2 = CreateTestOrder(testOrderId2, 1, TestOrderState.Rejected);
        var request = CreateRequest(1, testOrder1, testOrder2);

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet([testOrder1, testOrder2]);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet([request]);
        ConfigureDeleteSamplesAfterExport(channelId: 1, shouldDelete: false);

        var notification = new SampleTestResultsSentToLisEvent([testOrderId1, testOrderId2], LisChannelId: 1);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        testOrder1.State.Should().Be(TestOrderState.Reported);
        testOrder2.State.Should().Be(TestOrderState.Reported);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5870")]
    [Trait("ProductRequirement", "PR_1813")]
    public async Task Handle_WhenMultipleTestOrdersFromDifferentRequests_ShouldUpdateAllMatchingOrders()
    {
        // Arrange
        var testOrderId1 = Guid.NewGuid();
        var testOrderId2 = Guid.NewGuid();
        var testOrder1 = CreateTestOrder(testOrderId1, 1, TestOrderState.Approved);
        var testOrder2 = CreateTestOrder(testOrderId2, 2, TestOrderState.Approved);
        var request1 = CreateRequest(1, testOrder1);
        var request2 = CreateRequest(2, testOrder2);

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet([testOrder1, testOrder2]);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet([request1, request2]);
        ConfigureDeleteSamplesAfterExport(channelId: 1, shouldDelete: false);

        var notification = new SampleTestResultsSentToLisEvent([testOrderId1, testOrderId2], LisChannelId: 1);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        testOrder1.State.Should().Be(TestOrderState.Reported);
        testOrder2.State.Should().Be(TestOrderState.Reported);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5870")]
    [Trait("ProductRequirement", "PR_1813")]
    public async Task Handle_WhenSomeTestOrdersNotInNotificationList_ShouldOnlyUpdateMatchingOrders()
    {
        // Arrange
        var testOrderId1 = Guid.NewGuid();
        var testOrderIdNotInList = Guid.NewGuid();
        var testOrder1 = CreateTestOrder(testOrderId1, 1, TestOrderState.Approved);
        var testOrder2 = CreateTestOrder(testOrderIdNotInList, 1, TestOrderState.Approved);
        var request = CreateRequest(1, testOrder1, testOrder2);

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet([testOrder1]);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet([request]);
        ConfigureDeleteSamplesAfterExport(channelId: 1, shouldDelete: false);

        var notification = new SampleTestResultsSentToLisEvent([testOrderId1], LisChannelId: 1);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        testOrder1.State.Should().Be(TestOrderState.Reported);
        testOrder2.State.Should().Be(TestOrderState.Approved, "because it was not in the notification list");
    }

    #endregion

    #region Handle - DeleteSamplesAfterExport

    [Fact]
    [Trait("ModuleRequirement", "MR_5582")]
    [Trait("ProductRequirement", "PR_1787")]
    public async Task Handle_WhenDeleteSettingEnabled_AndAllTestOrdersReported_ShouldSendDeleteRequestCommand()
    {
        // Arrange
        var testOrderId = Guid.NewGuid();
        var testOrder = CreateTestOrder(testOrderId, 1, TestOrderState.Approved);
        var request = CreateRequest(1, testOrder);
        const int lisChannelId = 5;

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet([testOrder]);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet([request]);
        ConfigureDeleteSamplesAfterExport(channelId: lisChannelId, shouldDelete: true);

        var notification = new SampleTestResultsSentToLisEvent([testOrderId], lisChannelId);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(x => x.Send(
            It.Is<DeleteRequestCommand>(cmd =>
                cmd.Ids.Count == 1 &&
                cmd.Ids.Contains(1)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5582")]
    [Trait("ProductRequirement", "PR_1787")]
    public async Task Handle_WhenDeleteSettingDisabled_ShouldNotSendDeleteRequestCommand()
    {
        // Arrange
        var testOrderId = Guid.NewGuid();
        var testOrder = CreateTestOrder(testOrderId, 1, TestOrderState.Approved);
        var request = CreateRequest(1, testOrder);
        const int lisChannelId = 5;

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet([testOrder]);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet([request]);
        ConfigureDeleteSamplesAfterExport(channelId: lisChannelId, shouldDelete: false);

        var notification = new SampleTestResultsSentToLisEvent([testOrderId], lisChannelId);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(x => x.Send(It.IsAny<DeleteRequestCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5582")]
    [Trait("ProductRequirement", "PR_1787")]
    public async Task Handle_WithMultipleRequests_AndDeleteSettingEnabled_ShouldSendDeleteCommandWithAllFullyReportedRequestIds()
    {
        // Arrange
        var testOrderId1 = Guid.NewGuid();
        var testOrderId2 = Guid.NewGuid();
        var testOrder1 = CreateTestOrder(testOrderId1, 1, TestOrderState.Approved);
        var testOrder2 = CreateTestOrder(testOrderId2, 2, TestOrderState.Approved);
        var request1 = CreateRequest(1, testOrder1);
        var request2 = CreateRequest(2, testOrder2);
        const int lisChannelId = 3;

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet([testOrder1, testOrder2]);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet([request1, request2]);
        ConfigureDeleteSamplesAfterExport(channelId: lisChannelId, shouldDelete: true);

        var notification = new SampleTestResultsSentToLisEvent([testOrderId1, testOrderId2], lisChannelId);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(x => x.Send(
            It.Is<DeleteRequestCommand>(cmd =>
                cmd.Ids.Count == 2 &&
                cmd.Ids.Contains(1) &&
                cmd.Ids.Contains(2)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5582")]
    [Trait("ProductRequirement", "PR_1787")]
    public async Task Handle_WhenNotAllTestOrdersReported_ShouldNotDeleteRequest()
    {
        // Arrange - Request has 2 test orders, only 1 is in the notification
        var testOrderId1 = Guid.NewGuid();
        var testOrderId2 = Guid.NewGuid();
        var testOrder1 = CreateTestOrder(testOrderId1, 1, TestOrderState.Approved);
        var testOrder2 = CreateTestOrder(testOrderId2, 1, TestOrderState.Approved); // Not in notification, won't be updated
        var request = CreateRequest(1, testOrder1, testOrder2);
        const int lisChannelId = 5;

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet([testOrder1]); // Only testOrder1 is found
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet([request]);
        ConfigureDeleteSamplesAfterExport(channelId: lisChannelId, shouldDelete: true);

        var notification = new SampleTestResultsSentToLisEvent([testOrderId1], lisChannelId);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert - testOrder2 is still Approved, so request should not be deleted
        _mediatorMock.Verify(x => x.Send(It.IsAny<DeleteRequestCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5582")]
    [Trait("ProductRequirement", "PR_1787")]
    public async Task Handle_WithMixedRequests_ShouldOnlyDeleteFullyReportedRequests()
    {
        // Arrange
        // Request 1: All test orders will be reported - should be deleted
        var testOrderId1 = Guid.NewGuid();
        var testOrder1 = CreateTestOrder(testOrderId1, 1, TestOrderState.Approved);
        var request1 = CreateRequest(1, testOrder1);

        // Request 2: Has an extra test order not in notification - should NOT be deleted
        var testOrderId2 = Guid.NewGuid();
        var testOrderId3 = Guid.NewGuid();
        var testOrder2 = CreateTestOrder(testOrderId2, 2, TestOrderState.Approved);
        var testOrder3 = CreateTestOrder(testOrderId3, 2, TestOrderState.Approved); // Won't be updated
        var request2 = CreateRequest(2, testOrder2, testOrder3);

        const int lisChannelId = 3;

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet([testOrder1, testOrder2]);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet([request1, request2]);
        ConfigureDeleteSamplesAfterExport(channelId: lisChannelId, shouldDelete: true);

        var notification = new SampleTestResultsSentToLisEvent([testOrderId1, testOrderId2], lisChannelId);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert - Only request 1 should be deleted
        _mediatorMock.Verify(x => x.Send(
            It.Is<DeleteRequestCommand>(cmd =>
                cmd.Ids.Count == 1 &&
                cmd.Ids.Contains(1) &&
                !cmd.Ids.Contains(2)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Handle - Request State Updates

    [Fact]
    [Trait("ModuleRequirement", "MR_5870")]
    [Trait("ProductRequirement", "PR_1813")]
    public async Task Handle_ShouldCallUpdateRequestStateOnEachRequest()
    {
        // Arrange
        var testOrderId = Guid.NewGuid();
        var testOrder = CreateTestOrder(testOrderId, 1, TestOrderState.Approved);
        var request = CreateRequest(1, testOrder);

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet([testOrder]);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet([request]);
        ConfigureDeleteSamplesAfterExport(channelId: 1, shouldDelete: false);

        var notification = new SampleTestResultsSentToLisEvent([testOrderId], LisChannelId: 1);

        // Act
        await _sut.Handle(notification, TestContext.Current.CancellationToken);

        // Assert - UpdateRequestState is called which updates RequestState property
        // Since all test orders are now Reported, the request state should be updated accordingly
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private void ConfigureDeleteSamplesAfterExport(int channelId, bool shouldDelete)
    {
        EnsureChannelExists(channelId);
        _lisSettings.LisChannels[channelId].ExportSettings.Sample.DeleteSamplesAfterExport = shouldDelete;
    }

    private void EnsureChannelExists(int channelId)
    {
        while (_lisSettings.LisChannels.Count <= channelId)
        {
            _lisSettings.LisChannels.Add(new LisChannel());
        }
    }

    #endregion
}
