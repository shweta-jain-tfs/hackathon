using Moq;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Commands.ControlSignalTower;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.Instrument.Application.Commands;

public class ControlSignalTowerCommandHandlerTests
{
    private readonly Mock<IInstrumentManager> _instrumentManagerMock;
    private readonly Mock<ICommunicationSessionManager> _sessionManagerMock;
    private readonly ControlSignalTowerCommandHandler _sut;

    public ControlSignalTowerCommandHandlerTests()
    {
        _instrumentManagerMock = new Mock<IInstrumentManager>();
        _sessionManagerMock = new Mock<ICommunicationSessionManager>();

        _sut = new ControlSignalTowerCommandHandler(_instrumentManagerMock.Object, _sessionManagerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSessionIsNull_DoesNotCallConnection()
    {
        // Arrange
        var command = CreateCommand("SN001", EnumSignalTowerLightState.Green, EnumSignalTowerControlSoundState.SoundOff);
        _sessionManagerMock.Setup(m => m.GetSession("SN001")).Returns((CommunicationSession?)null);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _instrumentManagerMock.Verify(m => m.GetInstrument(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSessionNotSynchronized_DoesNotCallConnection()
    {
        // Arrange
        var command = CreateCommand("SN001", EnumSignalTowerLightState.Green, EnumSignalTowerControlSoundState.SoundOff);

        var session = CreateSession("SN001", isSynchronized: false);
        _sessionManagerMock.Setup(m => m.GetSession("SN001")).Returns(session);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _instrumentManagerMock.Verify(m => m.GetInstrument(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenLightStateDiffers_SendsRequest()
    {
        // Arrange
        var command = CreateCommand("SN001", EnumSignalTowerLightState.Red, EnumSignalTowerControlSoundState.SoundOff);

        var instrument = CreateInstrument("SN001", SignalTowerLightState.Green, SignalTowerSoundState.Off);
        _instrumentManagerMock.Setup(m => m.GetInstrument("SN001")).Returns(instrument);

        var connectionMock = new Mock<IInstrumentConnection>();
        var session = CreateSession("SN001", isSynchronized: true, connectionMock.Object);
        _sessionManagerMock.Setup(m => m.GetSession("SN001")).Returns(session);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        connectionMock.Verify(c => c.ControlSignalTowerAsync(command.ControlSignalTowerRequest, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenLightSameAndSoundContinuousOnAlreadyContinuousOn_DoesNotSend()
    {
        // Arrange
        var command = CreateCommand("SN001", EnumSignalTowerLightState.Green, EnumSignalTowerControlSoundState.SoundContinuosOn);

        var instrument = CreateInstrument("SN001", SignalTowerLightState.Green, SignalTowerSoundState.ContinuousOn);
        _instrumentManagerMock.Setup(m => m.GetInstrument("SN001")).Returns(instrument);

        var connectionMock = new Mock<IInstrumentConnection>();
        var session = CreateSession("SN001", isSynchronized: true, connectionMock.Object);
        _sessionManagerMock.Setup(m => m.GetSession("SN001")).Returns(session);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        connectionMock.Verify(c => c.ControlSignalTowerAsync(It.IsAny<ControlSignalTowerRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenLightSameAndSoundOffAlreadyOff_DoesNotSend()
    {
        // Arrange
        var command = CreateCommand("SN001", EnumSignalTowerLightState.Green, EnumSignalTowerControlSoundState.SoundOff);

        var instrument = CreateInstrument("SN001", SignalTowerLightState.Green, SignalTowerSoundState.Off);
        _instrumentManagerMock.Setup(m => m.GetInstrument("SN001")).Returns(instrument);

        var connectionMock = new Mock<IInstrumentConnection>();
        var session = CreateSession("SN001", isSynchronized: true, connectionMock.Object);
        _sessionManagerMock.Setup(m => m.GetSession("SN001")).Returns(session);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        connectionMock.Verify(c => c.ControlSignalTowerAsync(It.IsAny<ControlSignalTowerRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenLightSameAndShortSignalWhileContinuousOn_DoesNotSend()
    {
        // Arrange
        var command = CreateCommand("SN001", EnumSignalTowerLightState.Green, EnumSignalTowerControlSoundState.SoundShortSignal);

        var instrument = CreateInstrument("SN001", SignalTowerLightState.Green, SignalTowerSoundState.ContinuousOn);
        _instrumentManagerMock.Setup(m => m.GetInstrument("SN001")).Returns(instrument);

        var connectionMock = new Mock<IInstrumentConnection>();
        var session = CreateSession("SN001", isSynchronized: true, connectionMock.Object);
        _sessionManagerMock.Setup(m => m.GetSession("SN001")).Returns(session);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        connectionMock.Verify(c => c.ControlSignalTowerAsync(It.IsAny<ControlSignalTowerRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenLightSameButSoundDifferent_SendsRequest()
    {
        // Arrange
        var command = CreateCommand("SN001", EnumSignalTowerLightState.Green, EnumSignalTowerControlSoundState.SoundShortSignal);

        var instrument = CreateInstrument("SN001", SignalTowerLightState.Green, SignalTowerSoundState.Off);
        _instrumentManagerMock.Setup(m => m.GetInstrument("SN001")).Returns(instrument);

        var connectionMock = new Mock<IInstrumentConnection>();
        var session = CreateSession("SN001", isSynchronized: true, connectionMock.Object);
        _sessionManagerMock.Setup(m => m.GetSession("SN001")).Returns(session);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        connectionMock.Verify(c => c.ControlSignalTowerAsync(command.ControlSignalTowerRequest, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ControlSignalTowerCommand CreateCommand(string instrumentNumber, EnumSignalTowerLightState light, EnumSignalTowerControlSoundState sound)
    {
        return new ControlSignalTowerCommand(instrumentNumber, new ControlSignalTowerRequest
        {
            SetLight = light,
            SetSound = sound
        });
    }

    private static Domain.Instrument CreateInstrument(string serialNumber, SignalTowerLightState lightState, SignalTowerSoundState soundState)
    {
        var signalTower = new SignalTower();
        signalTower.SetState(lightState, soundState);

        return new Domain.Instrument
        {
            SerialNumber = serialNumber,
            SignalTower = signalTower
        };
    }

    private static CommunicationSession CreateSession(string instrumentNumber, bool isSynchronized, IInstrumentConnection? connection = null)
    {
        // CommunicationSession is an internal class with a read-only IsSynchronized based on private state,
        // so for tests we create one and, if necessary, flip it to synchronized using the flag.
        var session = new CommunicationSession
        {
            SessionId = Guid.NewGuid(),
            InstrumentNumber = instrumentNumber,
            Connection = connection ?? Mock.Of<IInstrumentConnection>()
        };

        if (isSynchronized)
        {
            // Set the synchronization flag to reach the Synchronized composite state.
            session.SetSynchronizationFlag(CommunicationSynchronizationState.Synchronized);
        }

        return session;
    }
}
