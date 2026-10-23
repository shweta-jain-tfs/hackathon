using FluentValidation;
using Quartz;
using Tetris.Protocol.Pharos;
using EntityState = Pharos.Server.Application.Request.EntityState;
using TestOrder = Pharos.Server.Request.Domain.TestOrder;
using TestOrderState = Pharos.Server.Domain.Enums.TestOrderState;

namespace Pharos.Server.Request.Application.Events;

public class TestOrderStatusChangedEventHandlerTests
{
    private readonly Mock<IRequestDbContext> _dbContextMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ISchedulerFactory> _schedulerFactoryMock;
    private readonly Mock<IScheduler> _schedulerMock;
    private readonly TestOrderStatusChangedEventHandler _handler;
    private readonly Guid _testOrderUid;

    public TestOrderStatusChangedEventHandlerTests()
    {
        _dbContextMock = new Mock<IRequestDbContext>();
        _mediatorMock = new Mock<IMediator>();
        _schedulerFactoryMock = new Mock<ISchedulerFactory>();
        _schedulerMock = new Mock<IScheduler>();

        _schedulerFactoryMock
            .Setup(x => x.GetScheduler(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_schedulerMock.Object);

        _handler = new TestOrderStatusChangedEventHandler(
            _dbContextMock.Object,
            _mediatorMock.Object,
            _schedulerFactoryMock.Object);

        _testOrderUid = Guid.NewGuid();
    }

    private static TestOrderStatusChangedEvent CreateNotification(string testOrderId, EnumTestOrderStatus status)
    {
        var statusData = new TestOrderStatus
        {
            TestOrderId = testOrderId,
            Status = status
        };

        var message = new TestOrderStatusChange
        {
            TestOrderStatus = statusData
        };

        return new TestOrderStatusChangedEvent(message);
    }

    [Fact]
    public async Task Handle_RequestIdNotFound_ShouldReturnEarly()
    {
        // Arrange
        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet(new List<TestOrder>());
        var notification = CreateNotification(_testOrderUid.ToString(), EnumTestOrderStatus.Started);

        // Act
        await _handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mediatorMock.Verify(x => x.Publish(It.IsAny<RequestUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TestOrderNotFoundInRequest_ShouldReturnEarly()
    {
        // Arrange
        var differentTestOrderUid = Guid.NewGuid();

        var testOrder = new TestOrder
        {
            Uid = _testOrderUid,
            RequestId = 1
        };

        var request = new Domain.Request
        {
            Id = 1,
            TestOrders = new List<TestOrder>
            {
                new() { Uid = differentTestOrderUid, RequestId = 1 }
            }
        };

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet(new List<TestOrder> { testOrder });
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(new List<Domain.Request> { request });
        var notification = CreateNotification(_testOrderUid.ToString(), EnumTestOrderStatus.Started);

        // Act
        await _handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mediatorMock.Verify(x => x.Publish(It.IsAny<RequestUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(EnumTestOrderStatus.Started, TestOrderState.Started, false)]
    [InlineData(EnumTestOrderStatus.Pipetted, TestOrderState.Pipetted, false)]
    [InlineData(EnumTestOrderStatus.SampleUnloaded, TestOrderState.NotStarted, true)]
    [InlineData(EnumTestOrderStatus.MethodNotActive, TestOrderState.NotStarted, true)]
    [InlineData(EnumTestOrderStatus.Cancelled, TestOrderState.NotStarted, true)]
    [InlineData(EnumTestOrderStatus.UnableToStart, TestOrderState.UnableToStart, false)]
    public async Task Handle_ValidStatusChange_ShouldMapStatusCorrectly(EnumTestOrderStatus status, TestOrderState expectedState, bool triggerReschedule)
    {
        // Arrange
        var testOrder = new TestOrder
        {
            Uid = _testOrderUid,
            RequestId = 1,
            State = TestOrderState.Scheduled,
            EntityState = EntityState.Unchanged
        };

        var request = new Domain.Request
        {
            Id = 1,
            TestOrders = new List<TestOrder> { testOrder }
        };

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet(new List<TestOrder> { testOrder });
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(new List<Domain.Request> { request });
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var notification = CreateNotification(_testOrderUid.ToString(), status);

        // Act
        await _handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedState, testOrder.State);
        Assert.Equal(EntityState.Modified, testOrder.EntityState);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(x => x.Publish(It.IsAny<RequestUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _schedulerMock.Verify(
            x => x.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()),
            triggerReschedule ? Times.Once : Times.Never);
    }

    [Fact]
    public async Task Handle_StateUnchanged_ShouldReturnEarly()
    {
        // Arrange
        var testOrder = new TestOrder
        {
            Uid = _testOrderUid,
            RequestId = 1,
            State = TestOrderState.Started
        };

        var request = new Domain.Request
        {
            Id = 1,
            TestOrders = new List<TestOrder> { testOrder }
        };

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet(new List<TestOrder> { testOrder });
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(new List<Domain.Request> { request });
        var notification = CreateNotification(_testOrderUid.ToString(), EnumTestOrderStatus.Started);

        // Act
        await _handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mediatorMock.Verify(x => x.Publish(It.IsAny<RequestUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(EnumTestOrderStatus.SampleUnloaded)]
    [InlineData(EnumTestOrderStatus.MethodNotActive)]
    [InlineData(EnumTestOrderStatus.Cancelled)]
    public async Task Handle_StateChangedToNotStarted_ShouldClearTubeIdAndInstrumentNumber(EnumTestOrderStatus status)
    {
        // Arrange
        var testOrder = new TestOrder
        {
            Uid = _testOrderUid,
            RequestId = 1,
            State = TestOrderState.Scheduled,
            TubeId = "TUBE123",
            InstrumentNumber = "INST1"
        };

        var request = new Domain.Request
        {
            Id = 1,
            TestOrders = new List<TestOrder> { testOrder }
        };

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet(new List<TestOrder> { testOrder });
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(new List<Domain.Request> { request });
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var notification = CreateNotification(_testOrderUid.ToString(), status);

        // Act
        await _handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(TestOrderState.NotStarted, testOrder.State);
        Assert.Null(testOrder.TubeId);
        Assert.Null(testOrder.InstrumentNumber);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidStatus_ShouldThrowValidationException()
    {
        // Arrange
        var testOrder = new TestOrder
        {
            Uid = _testOrderUid,
            RequestId = 1,
            State = TestOrderState.NotStarted
        };

        var request = new Domain.Request
        {
            Id = 1,
            TestOrders = new List<TestOrder> { testOrder }
        };

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet(new List<TestOrder> { testOrder });
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(new List<Domain.Request> { request });
        var notification = CreateNotification(_testOrderUid.ToString(), (EnumTestOrderStatus)9999);

        // Act & Assert
        var validStatuses = new[]
        {
            EnumTestOrderStatus.Started,
            EnumTestOrderStatus.Pipetted,
            EnumTestOrderStatus.Scheduled,
            EnumTestOrderStatus.SampleUnloaded,
            EnumTestOrderStatus.MethodNotActive,
            EnumTestOrderStatus.Cancelled,
            EnumTestOrderStatus.UnableToStart
        };
        foreach (var status in Enum.GetValues<EnumTestOrderStatus>())
        {
            if (!validStatuses.Contains(status))
            {
                await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(notification, TestContext.Current.CancellationToken));
            }
        }
        await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(notification, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ValidStatusChange_ShouldPublishRequestUpdatedEventWithCorrectData()
    {
        // Arrange
        var testOrder = new TestOrder
        {
            Uid = _testOrderUid,
            RequestId = 1,
            State = TestOrderState.NotStarted
        };

        var request = new Domain.Request
        {
            Id = 1,
            SampleId = "SAMPLE123",
            Created = DateTimeOffset.UtcNow,
            TestOrders = new List<TestOrder> { testOrder }
        };

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet(new List<TestOrder> { testOrder });
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(new List<Domain.Request> { request });
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var notification = CreateNotification(_testOrderUid.ToString(), EnumTestOrderStatus.Started);

        // Act
        await _handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(
            x => x.Publish(
                It.Is<RequestUpdatedEvent>(evt =>
                    evt.RequestId == 1 &&
                    evt.RequestUid.SampleId == "SAMPLE123" &&
                    evt.TestOrders.Count == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidStatusChange_ShouldSaveChangesBeforePublishingEvent()
    {
        // Arrange
        var callOrder = new List<string>();

        var testOrder = new TestOrder
        {
            Uid = _testOrderUid,
            RequestId = 1,
            State = TestOrderState.NotStarted
        };

        var request = new Domain.Request
        {
            Id = 1,
            TestOrders = new List<TestOrder> { testOrder }
        };

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet(new List<TestOrder> { testOrder });
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(new List<Domain.Request> { request });
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChanges"))
            .ReturnsAsync(1);

        _mediatorMock.Setup(x => x.Publish(It.IsAny<RequestUpdatedEvent>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Publish"))
            .Returns(Task.CompletedTask);

        var notification = CreateNotification(_testOrderUid.ToString(), EnumTestOrderStatus.Started);

        // Act
        await _handler.Handle(notification, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(new[] { "SaveChanges", "Publish" }, callOrder);
    }
}
