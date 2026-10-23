using Moq;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Tetris.Protocol.Pharos.ProcessStatus;

namespace Pharos.Server.Instrument.Application.Events;

public class ProcessStatusUpdatedEventHandlerTests
{
    private readonly Mock<IInstrumentManager> _instrumentManagerMock;
    private readonly Mock<IInstrumentHub> _instrumentHubMock;
    private readonly Mock<ICurrentInstrumentServerHub> _currentInstrumentHubMock;
    private readonly ProcessStatusUpdatedEventHandler _handler;

    private const string _testInstrumentNumber = "S001";
    private const int _testInstrumentId = 1;

    public ProcessStatusUpdatedEventHandlerTests()
    {
        _instrumentManagerMock = new Mock<IInstrumentManager>();
        _instrumentHubMock = new Mock<IInstrumentHub>();
        _currentInstrumentHubMock = new Mock<ICurrentInstrumentServerHub>();

        _handler = new ProcessStatusUpdatedEventHandler(
            _instrumentManagerMock.Object,
            _instrumentHubMock.Object,
            _currentInstrumentHubMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldUpdateInstrumentProcessStatus()
    {
        // Arrange
        var instrument = CreateTestInstrument();
        var newProcessStatus = CreateProcessStatus(testCount: 42);
        var @event = new ProcessStatusUpdatedEvent(_testInstrumentNumber, newProcessStatus);

        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(instrument);

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        instrument.ProcessStatus.Should().Be(newProcessStatus);
    }

    [Fact]
    public async Task Handle_ShouldUpdateTimeWhenNextStateReached_WhenStateIsValid()
    {
        // Arrange
        var instrument = CreateTestInstrument(state: InstrumentState.Assay);
        var lastResultTime = DateTimeOffset.Now.AddHours(2);
        var newProcessStatus = CreateProcessStatus(
            testCount: 42,
            lastRemainingResultTime: lastResultTime);
        var @event = new ProcessStatusUpdatedEvent(_testInstrumentNumber, newProcessStatus);

        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(instrument);

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        instrument.TimeWhenNextStateReached.Should().NotBeNull();
        instrument.TimeWhenNextStateReached.Should().BeCloseTo(lastResultTime.ToLocalTime(), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Handle_ShouldSetTimeWhenNextStateReachedToNull_WhenStateIsInvalid()
    {
        // Arrange
        var instrument = CreateTestInstrument(state: InstrumentState.Standby);
        var newProcessStatus = CreateProcessStatus(testCount: 42);
        var @event = new ProcessStatusUpdatedEvent(_testInstrumentNumber, newProcessStatus);

        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(instrument);

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        instrument.TimeWhenNextStateReached.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldAlwaysNotifyCurrentInstrumentHub()
    {
        // Arrange
        var instrument = CreateTestInstrument();
        var newProcessStatus = CreateProcessStatus(testCount: 42);
        var @event = new ProcessStatusUpdatedEvent(_testInstrumentNumber, newProcessStatus);

        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(instrument);

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        _currentInstrumentHubMock.Verify(
            x => x.ProcessStatusUpdated(_testInstrumentId),
            Times.Once,
            "ProcessStatusUpdated should always be called for local instrument client");
    }

    [Fact]
    public async Task Handle_ShouldNotNotifyInstrumentHub_WhenTestCountUnchanged()
    {
        // Arrange
        var oldProcessStatus = CreateProcessStatus(testCount: 42);
        var instrument = CreateTestInstrument(processStatus: oldProcessStatus);
        var newProcessStatus = CreateProcessStatus(testCount: 42); // Same count
        var @event = new ProcessStatusUpdatedEvent(_testInstrumentNumber, newProcessStatus);

        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(instrument);

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        _instrumentHubMock.Verify(
            x => x.NumberOfTestsUpdated(It.IsAny<int>(), It.IsAny<int>()),
            Times.Never,
            "NumberOfTestsUpdated should not be called when count is unchanged");
    }

    [Fact]
    public async Task Handle_ShouldNotifyInstrumentHub_WhenTestCountChanged()
    {
        // Arrange
        var oldProcessStatus = CreateProcessStatus(testCount: 42);
        var instrument = CreateTestInstrument(processStatus: oldProcessStatus);
        var newProcessStatus = CreateProcessStatus(testCount: 50); // Different count
        var @event = new ProcessStatusUpdatedEvent(_testInstrumentNumber, newProcessStatus);

        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(instrument);

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        _instrumentHubMock.Verify(
            x => x.NumberOfTestsUpdated(_testInstrumentId, 50),
            Times.Once,
            "NumberOfTestsUpdated should be called when count changes");
    }

    [Fact]
    public async Task Handle_ShouldNotifyInstrumentHub_WhenOldStatusIsNull()
    {
        // Arrange
        var instrument = CreateTestInstrument(processStatus: null);
        var newProcessStatus = CreateProcessStatus(testCount: 42);
        var @event = new ProcessStatusUpdatedEvent(_testInstrumentNumber, newProcessStatus);

        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(instrument);

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        _instrumentHubMock.Verify(
            x => x.NumberOfTestsUpdated(_testInstrumentId, 42),
            Times.Once,
            "NumberOfTestsUpdated should be called when old status is null");
    }

    [Fact]
    public async Task Handle_ShouldNotifyInstrumentHub_WhenTimeChanged()
    {
        // Arrange
        var oldTime = DateTimeOffset.Now.AddHours(1);
        var newTime = DateTimeOffset.Now.AddHours(2);
        var oldProcessStatus = CreateProcessStatus(testCount: 42, lastRemainingResultTime: oldTime);
        var instrument = CreateTestInstrument(
            state: InstrumentState.Assay,
            processStatus: oldProcessStatus,
            timeWhenNextStateReached: oldTime.ToLocalTime());
        var newProcessStatus = CreateProcessStatus(testCount: 42, lastRemainingResultTime: newTime);
        var @event = new ProcessStatusUpdatedEvent(_testInstrumentNumber, newProcessStatus);

        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(instrument);

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        _instrumentHubMock.Verify(
            x => x.TimeWhenNextStateReachedChanged(
                _testInstrumentId,
                It.Is<DateTimeOffset>(dt => Math.Abs((dt - newTime.ToLocalTime()).TotalSeconds) < 1)),
            Times.Once,
            "TimeWhenNextStateReachedChanged should be called when time changes");
    }

    [Fact]
    public async Task Handle_ShouldNotNotifyInstrumentHub_WhenTimeUnchanged()
    {
        // Arrange
        var testTime = DateTimeOffset.Now.AddHours(2);
        var oldProcessStatus = CreateProcessStatus(testCount: 42, lastRemainingResultTime: testTime);
        var instrument = CreateTestInstrument(
            state: InstrumentState.Assay,
            processStatus: oldProcessStatus,
            timeWhenNextStateReached: testTime.ToLocalTime());
        var newProcessStatus = CreateProcessStatus(testCount: 42, lastRemainingResultTime: testTime);
        var @event = new ProcessStatusUpdatedEvent(_testInstrumentNumber, newProcessStatus);

        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(instrument);

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        _instrumentHubMock.Verify(
            x => x.TimeWhenNextStateReachedChanged(It.IsAny<int>(), It.IsAny<DateTimeOffset>()),
            Times.Never,
            "TimeWhenNextStateReachedChanged should not be called when time is unchanged");
    }

    [Theory]
    [InlineData(InstrumentState.Assay)]
    [InlineData(InstrumentState.AssayFinishOrdered)]
    public async Task Handle_ShouldUseCorrectTimeSource_ForValidStates(InstrumentState state)
    {
        // Arrange
        var lastResultTime = DateTimeOffset.Now.AddHours(2);
        var lastStartTime = DateTimeOffset.Now.AddHours(3);
        var instrument = CreateTestInstrument(state: state);
        var newProcessStatus = CreateProcessStatus(
            testCount: 42,
            lastRemainingResultTime: lastResultTime,
            lastRemainingStartTime: lastStartTime);
        var @event = new ProcessStatusUpdatedEvent(_testInstrumentNumber, newProcessStatus);

        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(instrument);

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        var expectedTime = state == InstrumentState.Assay
            ? lastResultTime.ToLocalTime()
            : lastStartTime.ToLocalTime();

        instrument.TimeWhenNextStateReached.Should().NotBeNull();
        instrument.TimeWhenNextStateReached!.Value.Should().BeCloseTo(expectedTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Handle_ShouldUseMinValue_WhenLastRemainingResultTimeIsNull()
    {
        // Arrange
        var instrument = CreateTestInstrument(state: InstrumentState.Assay);
        var newProcessStatus = CreateProcessStatus(
            testCount: 42,
            lastRemainingResultTime: null);
        var @event = new ProcessStatusUpdatedEvent(_testInstrumentNumber, newProcessStatus);

        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(instrument);

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        instrument.TimeWhenNextStateReached.Should().Be(DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task Handle_ShouldCallHubsInCorrectOrder()
    {
        // Arrange
        var callOrder = new List<string>();
        var instrument = CreateTestInstrument();
        var newProcessStatus = CreateProcessStatus(testCount: 42);
        var @event = new ProcessStatusUpdatedEvent(_testInstrumentNumber, newProcessStatus);

        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentNumber))
            .Returns(instrument);

        _currentInstrumentHubMock
            .Setup(x => x.ProcessStatusUpdated(It.IsAny<int>()))
            .Callback(() => callOrder.Add("CurrentInstrumentHub.ProcessStatusUpdated"))
            .Returns(Task.CompletedTask);

        _instrumentHubMock
            .Setup(x => x.NumberOfTestsUpdated(It.IsAny<int>(), It.IsAny<int>()))
            .Callback(() => callOrder.Add("InstrumentHub.NumberOfTestsUpdated"))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        callOrder.Should().ContainInOrder(
            "CurrentInstrumentHub.ProcessStatusUpdated",
            "InstrumentHub.NumberOfTestsUpdated");
    }

    // Helper methods
    private static Domain.Instrument CreateTestInstrument(
        InstrumentState state = InstrumentState.Standby,
        ProcessStatus? processStatus = null,
        DateTimeOffset? timeWhenNextStateReached = null)
    {
        return new Domain.Instrument
        {
            Id = _testInstrumentId,
            SerialNumber = _testInstrumentNumber,
            State = state,
            ProcessStatus = processStatus,
            TimeWhenNextStateReached = timeWhenNextStateReached,
            Enabled = true
        };
    }

    private static ProcessStatus CreateProcessStatus(
        int testCount,
        DateTimeOffset? lastRemainingResultTime = null,
        DateTimeOffset? lastRemainingStartTime = null)
    {
        var processListItems = new List<ProcessListItem>();
        for (var i = 0; i < testCount; i++)
        {
            processListItems.Add(new ProcessListItem());
        }

        return new ProcessStatus
        {
            RemainingResults = testCount,
            ProcessListItems = { processListItems },
            LastRemainingResultTime = lastRemainingResultTime.HasValue
                ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(lastRemainingResultTime.Value)
                : null,
            LastRemainingStartTime = lastRemainingStartTime.HasValue
                ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(lastRemainingStartTime.Value)
                : null
        };
    }
}
