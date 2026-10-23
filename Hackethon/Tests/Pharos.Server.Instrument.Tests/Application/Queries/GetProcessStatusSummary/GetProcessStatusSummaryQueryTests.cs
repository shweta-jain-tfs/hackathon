using Moq;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Tetris.Protocol.Pharos.ProcessStatus;

namespace Pharos.Server.Instrument.Application.Queries.GetProcessStatusSummary;

public class GetProcessStatusSummaryQueryTests
{
    private readonly Mock<IInstrumentManager> _instrumentManagerMock;
    private readonly GetProcessStatusSummaryQueryHandler _handler;

    private const int _testInstrumentId = 1;
    private const string _testInstrumentNumber = "S001";

    public GetProcessStatusSummaryQueryTests()
    {
        _instrumentManagerMock = new Mock<IInstrumentManager>();
        _handler = new GetProcessStatusSummaryQueryHandler(_instrumentManagerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnDefaultDto_WhenProcessStatusIsNull()
    {
        // Arrange
        var instrument = CreateTestInstrument(processStatus: null);
        var query = new GetProcessStatusSummaryQuery(_testInstrumentId);
        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentId))
            .Returns(instrument);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.RemainingResults.Should().Be(0);
        result.LastPipettingTime.Should().Be(DateTimeOffset.UnixEpoch);
        result.LastRemainingResultTime.Should().Be(DateTimeOffset.UnixEpoch);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectRemainingResults()
    {
        // Arrange
        var processStatus = CreateProcessStatus(
            remainingResults: 42,
            lastRemainingPipettingTime: DateTimeOffset.UtcNow.AddHours(-1),
            lastRemainingResultTime: DateTimeOffset.UtcNow.AddHours(2));
        var instrument = CreateTestInstrument(processStatus: processStatus);
        var query = new GetProcessStatusSummaryQuery(_testInstrumentId);
        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentId))
            .Returns(instrument);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.RemainingResults.Should().Be(42);
    }

    [Fact]
    public async Task Handle_ShouldReturnLastPipettingTimeInLocalTime()
    {
        // Arrange
        var pipettingTime = DateTimeOffset.UtcNow.AddHours(-1);
        var processStatus = CreateProcessStatus(
            lastRemainingPipettingTime: pipettingTime,
            lastRemainingResultTime: DateTimeOffset.UtcNow.AddHours(2));
        var instrument = CreateTestInstrument(processStatus: processStatus);
        var query = new GetProcessStatusSummaryQuery(_testInstrumentId);
        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentId))
            .Returns(instrument);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.LastPipettingTime.Should().BeCloseTo(pipettingTime.ToLocalTime(), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Handle_ShouldReturnUnixEpoch_WhenLastPipettingTimeIsNull()
    {
        // Arrange
        var processStatus = CreateProcessStatus(
            lastRemainingPipettingTime: null,
            lastRemainingResultTime: DateTimeOffset.UtcNow.AddHours(2));
        var instrument = CreateTestInstrument(processStatus: processStatus);
        var query = new GetProcessStatusSummaryQuery(_testInstrumentId);
        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentId))
            .Returns(instrument);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.LastPipettingTime.Should().Be(DateTimeOffset.UnixEpoch);
    }

    [Fact]
    public async Task Handle_ShouldReturnLastRemainingResultTime_WhenStateIsAssay()
    {
        // Arrange
        var lastResultTime = DateTimeOffset.UtcNow.AddHours(2);
        var lastStartTime = DateTimeOffset.UtcNow.AddHours(3);
        var processStatus = CreateProcessStatus(
            lastRemainingPipettingTime: DateTimeOffset.UtcNow.AddHours(-1),
            lastRemainingResultTime: lastResultTime,
            lastRemainingStartTime: lastStartTime);
        var instrument = CreateTestInstrument(
            state: InstrumentState.Assay,
            processStatus: processStatus);
        var query = new GetProcessStatusSummaryQuery(_testInstrumentId);
        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentId))
            .Returns(instrument);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.LastRemainingResultTime.Should().BeCloseTo(lastResultTime.ToLocalTime(), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Handle_ShouldReturnLastRemainingStartTime_WhenStateIsAssayFinishOrdered()
    {
        // Arrange
        var lastResultTime = DateTimeOffset.UtcNow.AddHours(2);
        var lastStartTime = DateTimeOffset.UtcNow.AddHours(3);
        var processStatus = CreateProcessStatus(
            lastRemainingPipettingTime: DateTimeOffset.UtcNow.AddHours(-1),
            lastRemainingResultTime: lastResultTime,
            lastRemainingStartTime: lastStartTime);
        var instrument = CreateTestInstrument(
            state: InstrumentState.AssayFinishOrdered,
            processStatus: processStatus);
        var query = new GetProcessStatusSummaryQuery(_testInstrumentId);
        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentId))
            .Returns(instrument);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.LastRemainingResultTime.Should().BeCloseTo(lastStartTime.ToLocalTime(), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Handle_ShouldReturnLastRemainingResultTime_WhenStateIsNotAssayFinishOrdered()
    {
        // Arrange
        var lastResultTime = DateTimeOffset.UtcNow.AddHours(2);
        var processStatus = CreateProcessStatus(
            lastRemainingPipettingTime: DateTimeOffset.UtcNow.AddHours(-1),
            lastRemainingResultTime: lastResultTime);
        var instrument = CreateTestInstrument(
            state: InstrumentState.Standby,
            processStatus: processStatus);
        var query = new GetProcessStatusSummaryQuery(_testInstrumentId);
        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentId))
            .Returns(instrument);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.LastRemainingResultTime.Should().BeCloseTo(lastResultTime.ToLocalTime(), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Handle_ShouldReturnUnixEpoch_WhenLastRemainingResultTimeIsNull()
    {
        // Arrange
        var processStatus = CreateProcessStatus(
            lastRemainingPipettingTime: DateTimeOffset.UtcNow.AddHours(-1),
            lastRemainingResultTime: null);
        var instrument = CreateTestInstrument(
            state: InstrumentState.Assay,
            processStatus: processStatus);
        var query = new GetProcessStatusSummaryQuery(_testInstrumentId);
        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentId))
            .Returns(instrument);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.LastRemainingResultTime.Should().Be(DateTimeOffset.UnixEpoch);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnixEpoch_WhenLastRemainingStartTimeIsNull()
    {
        // Arrange
        var processStatus = CreateProcessStatus(
            lastRemainingPipettingTime: DateTimeOffset.UtcNow.AddHours(-1),
            lastRemainingResultTime: DateTimeOffset.UtcNow.AddHours(2),
            lastRemainingStartTime: null);
        var instrument = CreateTestInstrument(
            state: InstrumentState.AssayFinishOrdered,
            processStatus: processStatus);
        var query = new GetProcessStatusSummaryQuery(_testInstrumentId);
        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentId))
            .Returns(instrument);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.LastRemainingResultTime.Should().Be(DateTimeOffset.UnixEpoch);
    }

    [Theory]
    [InlineData(InstrumentState.Assay)]
    [InlineData(InstrumentState.AssayFinishOrdered)]
    public async Task Handle_ShouldUseCorrectTimeSource_ForValidStates(InstrumentState state)
    {
        // Arrange
        var lastResultTime = DateTimeOffset.UtcNow.AddHours(2);
        var lastStartTime = DateTimeOffset.UtcNow.AddHours(3);
        var processStatus = CreateProcessStatus(
            lastRemainingPipettingTime: DateTimeOffset.UtcNow.AddHours(-1),
            lastRemainingResultTime: lastResultTime,
            lastRemainingStartTime: lastStartTime);
        var instrument = CreateTestInstrument(
            state: state,
            processStatus: processStatus);
        var query = new GetProcessStatusSummaryQuery(_testInstrumentId);
        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentId))
            .Returns(instrument);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        var expectedTime = state == InstrumentState.Assay
            ? lastResultTime.ToLocalTime()
            : lastStartTime.ToLocalTime();

        result.LastRemainingResultTime.Should().BeCloseTo(expectedTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Handle_ShouldReturnCompleteDto_WithAllFieldsPopulated()
    {
        // Arrange
        var lastResultTime = DateTimeOffset.UtcNow.AddHours(2);
        var lastPipettingTime = DateTimeOffset.UtcNow.AddHours(-1);
        var processStatus = CreateProcessStatus(
            remainingResults: 15,
            lastRemainingPipettingTime: lastPipettingTime,
            lastRemainingResultTime: lastResultTime);
        var instrument = CreateTestInstrument(
            state: InstrumentState.Assay,
            processStatus: processStatus);
        var query = new GetProcessStatusSummaryQuery(_testInstrumentId);
        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_testInstrumentId))
            .Returns(instrument);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        result.RemainingResults.Should().Be(15);
        result.LastPipettingTime.Should().BeCloseTo(lastPipettingTime.ToLocalTime(), TimeSpan.FromSeconds(1));
        result.LastRemainingResultTime.Should().BeCloseTo(lastResultTime.ToLocalTime(), TimeSpan.FromSeconds(1));
    }

    // Helper methods
    private static Domain.Instrument CreateTestInstrument(
        InstrumentState state = InstrumentState.Standby,
        ProcessStatus? processStatus = null)
    {
        return new Domain.Instrument
        {
            Id = _testInstrumentId,
            SerialNumber = _testInstrumentNumber,
            State = state,
            ProcessStatus = processStatus,
            Enabled = true
        };
    }

    private static ProcessStatus CreateProcessStatus(
        int remainingResults = 0,
        DateTimeOffset? lastRemainingPipettingTime = null,
        DateTimeOffset? lastRemainingResultTime = null,
        DateTimeOffset? lastRemainingStartTime = null)
    {
        return new ProcessStatus
        {
            RemainingResults = remainingResults,
            LastRemainingPipettingTime = lastRemainingPipettingTime.HasValue
                ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(lastRemainingPipettingTime.Value)
                : null,
            LastRemainingResultTime = lastRemainingResultTime.HasValue
                ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(lastRemainingResultTime.Value)
                : null,
            LastRemainingStartTime = lastRemainingStartTime.HasValue
                ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(lastRemainingStartTime.Value)
                : null
        };
    }
}
