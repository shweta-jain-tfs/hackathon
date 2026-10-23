using Microsoft.Extensions.Logging;
using Moq;
using Pharos.Server.Application.Identity;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Services;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.Instrument.Application.Events;

public class UserLoggedInEventHandlerTests
{
    private readonly Mock<IDrawerAuthorizationService> _drawerAuthorizationServiceMock;
    private readonly Mock<IInstrumentManager> _instrumentManagerMock;
    private readonly Mock<ICommunicationSessionManager> _sessionManagerMock;
    private readonly Mock<IAutomaticPowerSaveScheduler> _automaticPowerSaveSchedulerMock;
    private readonly Mock<ILogger<UserLoggedInEventHandler>> _loggerMock;
    private readonly UserLoggedInEventHandler _handler;

    private const int _testInstrumentId = 1;
    private const string _testSerialNumber = "S001";
    private static readonly Guid _testUserId = Guid.NewGuid();

    public UserLoggedInEventHandlerTests()
    {
        _drawerAuthorizationServiceMock = new Mock<IDrawerAuthorizationService>();
        _instrumentManagerMock = new Mock<IInstrumentManager>();
        _sessionManagerMock = new Mock<ICommunicationSessionManager>();
        _automaticPowerSaveSchedulerMock = new Mock<IAutomaticPowerSaveScheduler>();
        _loggerMock = new Mock<ILogger<UserLoggedInEventHandler>>();

        _handler = new UserLoggedInEventHandler(
            _drawerAuthorizationServiceMock.Object,
            _instrumentManagerMock.Object,
            _sessionManagerMock.Object,
            _automaticPowerSaveSchedulerMock.Object,
            _loggerMock.Object);
    }

    [Theory]
    [InlineData(InstrumentState.Standby)]
    [InlineData(InstrumentState.PowerSaving)]
    public async Task Handle_ShouldSendStandbyRequest_WhenInstrumentIsInStandbyOrPowerSaving(InstrumentState state)
    {
        // Arrange
        var @event = new UserLoggedInEvent(_testUserId, _testSerialNumber);
        var instrument = CreateInstrument(state);
        var connectionMock = SetupInstrumentConnection(instrument);

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        connectionMock.Verify(
            x => x.ControlInstrumentStateAsync(
                It.Is<ControlInstrumentStateRequest>(r => r.OrderedState == EnumOrderedInstrumentState.Standby),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotSendStandbyRequest_WhenInstrumentIdIsNull()
    {
        // Arrange
        var @event = new UserLoggedInEvent(_testUserId, InstrumentNumber: null);
        var connectionMock = new Mock<IInstrumentConnection>();

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        connectionMock.Verify(
            x => x.ControlInstrumentStateAsync(It.IsAny<ControlInstrumentStateRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(InstrumentState.Assay)]
    [InlineData(InstrumentState.Offline)]
    [InlineData(InstrumentState.Startup)]
    public async Task Handle_ShouldNotSendStandbyRequest_WhenInstrumentIsInOtherState(InstrumentState state)
    {
        // Arrange
        var @event = new UserLoggedInEvent(_testUserId, _testSerialNumber);
        var instrument = CreateInstrument(state);
        var connectionMock = SetupInstrumentConnection(instrument);

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        connectionMock.Verify(
            x => x.ControlInstrumentStateAsync(It.IsAny<ControlInstrumentStateRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCallSendUnlockIfWaiting_WithCorrectParameters()
    {
        // Arrange
        var @event = new UserLoggedInEvent(_testUserId, _testSerialNumber);
        CreateInstrument(InstrumentState.Offline);

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        _drawerAuthorizationServiceMock.Verify(
            x => x.SendUnlockIfWaiting(_testUserId, _testSerialNumber),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotCallSendUnlockIfWaiting_WhenUserIdIsNull()
    {
        // Arrange
        var @event = new UserLoggedInEvent(null, _testSerialNumber);
        CreateInstrument(InstrumentState.Offline);

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        _drawerAuthorizationServiceMock.Verify(
            x => x.SendUnlockIfWaiting(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldNotCallSendUnlockIfWaiting_WhenInstrumentIdIsNull()
    {
        // Arrange
        var @event = new UserLoggedInEvent(_testUserId, null);
        CreateInstrument(InstrumentState.Offline);

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        _drawerAuthorizationServiceMock.Verify(
            x => x.SendUnlockIfWaiting(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldLogWarning_WhenControlInstrumentStateAsyncReturnsNonOkResponse()
    {
        // Arrange
        var @event = new UserLoggedInEvent(_testUserId, _testSerialNumber);
        var instrument = CreateInstrument(InstrumentState.Standby);
        var connectionMock = SetupInstrumentConnection(instrument);
        connectionMock
            .Setup(x => x.ControlInstrumentStateAsync(It.IsAny<ControlInstrumentStateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ControlInstrumentStateResponse { Response = EnumInstrumentStateResponse.Error });

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldHandleNullSession_GracefullyWhenInStandby()
    {
        // Arrange
        var @event = new UserLoggedInEvent(_testUserId, _testSerialNumber);
        var instrument = CreateInstrument(InstrumentState.Standby);
        _sessionManagerMock
            .Setup(x => x.GetSession(instrument.SerialNumber))
            .Returns((CommunicationSession?)null);

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert - Should not throw and should call SendUnlockIfWaiting
        _drawerAuthorizationServiceMock.Verify(
            x => x.SendUnlockIfWaiting(_testUserId, _testSerialNumber),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldHandleNullConnection_GracefullyWhenInStandby()
    {
        // Arrange
        var @event = new UserLoggedInEvent(_testUserId, _testSerialNumber);
        var instrument = CreateInstrument(InstrumentState.Standby);
        var session = new CommunicationSession
        {
            SessionId = Guid.NewGuid(),
            InstrumentNumber = instrument.SerialNumber,
            Connection = null!
        };
        _sessionManagerMock
            .Setup(x => x.GetSession(instrument.SerialNumber))
            .Returns(session);

        // Act
        await _handler.Handle(@event, TestContext.Current.CancellationToken);

        // Assert - Should not throw
        _drawerAuthorizationServiceMock.Verify(
            x => x.SendUnlockIfWaiting(_testUserId, _testSerialNumber),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPropagateException_FromControlInstrumentStateAsync()
    {
        // Arrange
        var @event = new UserLoggedInEvent(_testUserId, _testSerialNumber);
        var instrument = CreateInstrument(InstrumentState.Standby);
        var connectionMock = SetupInstrumentConnection(instrument);
        connectionMock
            .Setup(x => x.ControlInstrumentStateAsync(It.IsAny<ControlInstrumentStateRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(@event, TestContext.Current.CancellationToken));
    }

    private Domain.Instrument CreateInstrument(InstrumentState state)
    {
        var instrument = new Domain.Instrument
        {
            Id = _testInstrumentId,
            SerialNumber = _testSerialNumber,
            State = state
        };

        _instrumentManagerMock
            .Setup(x => x.GetInstrument(instrument.SerialNumber))
            .Returns(instrument);

        return instrument;
    }

    private Mock<IInstrumentConnection> SetupInstrumentConnection(Domain.Instrument instrument)
    {
        var connectionMock = new Mock<IInstrumentConnection>();
        var session = new CommunicationSession
        {
            SessionId = Guid.NewGuid(),
            InstrumentNumber = instrument.SerialNumber,
            Connection = connectionMock.Object
        };

        session.SetSynchronizationFlag(CommunicationSynchronizationState.Synchronized);

        _sessionManagerMock
            .Setup(x => x.GetSession(instrument.SerialNumber))
            .Returns(session);

        connectionMock
            .Setup(x => x.ControlInstrumentStateAsync(It.IsAny<ControlInstrumentStateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ControlInstrumentStateResponse { Response = EnumInstrumentStateResponse.Ok });

        return connectionMock;
    }
}
