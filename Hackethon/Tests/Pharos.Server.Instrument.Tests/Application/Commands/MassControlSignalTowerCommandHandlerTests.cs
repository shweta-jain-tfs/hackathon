using MediatR;
using Moq;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Commands.ControlSignalTower;
using Pharos.Server.Instrument.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.Instrument.Application.Commands;

public class MassControlSignalTowerCommandHandlerTests
{
    private readonly Mock<IInstrumentManager> _instrumentManagerMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly MassControlSignalTowerCommandHandler _sut;

    public MassControlSignalTowerCommandHandlerTests()
    {
        _instrumentManagerMock = new Mock<IInstrumentManager>();
        _publisherMock = new Mock<IPublisher>();
        _sut = new MassControlSignalTowerCommandHandler(_instrumentManagerMock.Object, _publisherMock.Object);
    }

    [Fact]
    public async Task Handle_WithAllInstruments_SetsRequestForAllInstruments()
    {
        // Arrange
        var allInstrumentsRequest = new ControlSignalTowerRequest
        {
            SetLight = EnumSignalTowerLightState.Red,
            SetSound = EnumSignalTowerControlSoundState.SoundContinuosOn
        };

        var instruments = CreateTestInstruments();
        _instrumentManagerMock.Setup(x => x.GetInstruments()).Returns(instruments);
        SetupInstrumentManagerForIndividualInstruments(instruments);

        var command = new MassControlSignalTowerCommand(
            new Dictionary<string, ControlSignalTowerRequest>(),
            allInstrumentsRequest);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _publisherMock.Verify(x => x.Publish(
            It.Is<ControlSignalTowerCommand>(e =>
                e.InstrumentNumber == "SN001" &&
                e.ControlSignalTowerRequest.SetLight == EnumSignalTowerLightState.Red &&
                e.ControlSignalTowerRequest.SetSound == EnumSignalTowerControlSoundState.SoundContinuosOn),
            It.IsAny<CancellationToken>()), Times.Once);

        _publisherMock.Verify(x => x.Publish(
            It.Is<ControlSignalTowerCommand>(e =>
                e.InstrumentNumber == "SN002" &&
                e.ControlSignalTowerRequest.SetLight == EnumSignalTowerLightState.Red &&
                e.ControlSignalTowerRequest.SetSound == EnumSignalTowerControlSoundState.SoundContinuosOn),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutAllInstruments_UsesDefaultOffState()
    {
        // Arrange
        var instruments = CreateTestInstruments();
        _instrumentManagerMock.Setup(x => x.GetInstruments()).Returns(instruments);
        SetupInstrumentManagerForIndividualInstruments(instruments);

        var command = new MassControlSignalTowerCommand(
            new Dictionary<string, ControlSignalTowerRequest>(),
            null);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _publisherMock.Verify(x => x.Publish(
            It.Is<ControlSignalTowerCommand>(e =>
                e.InstrumentNumber == "SN001" &&
                e.ControlSignalTowerRequest.SetLight == EnumSignalTowerLightState.Off &&
                e.ControlSignalTowerRequest.SetSound == EnumSignalTowerControlSoundState.SoundOff),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInstrumentSpecificRequests_OverridesAllInstrumentsRequest()
    {
        // Arrange
        var allInstrumentsRequest = new ControlSignalTowerRequest
        {
            SetLight = EnumSignalTowerLightState.Green,
            SetSound = EnumSignalTowerControlSoundState.SoundOff
        };

        var instrumentSpecificRequest = new ControlSignalTowerRequest
        {
            SetLight = EnumSignalTowerLightState.Red,
            SetSound = EnumSignalTowerControlSoundState.SoundContinuosOn
        };

        var instruments = CreateTestInstruments();
        // Force different states to ensure sending
        ((Domain.Instrument)instruments[0]).SignalTower.Reset();
        ((Domain.Instrument)instruments[1]).SignalTower.Reset();

        _instrumentManagerMock.Setup(x => x.GetInstruments()).Returns(instruments);
        SetupInstrumentManagerForIndividualInstruments(instruments);

        var command = new MassControlSignalTowerCommand(
            new Dictionary<string, ControlSignalTowerRequest> { { "SN001", instrumentSpecificRequest } },
            allInstrumentsRequest);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        // SN001 should get the instrument-specific request (upgraded from Green to Red)
        _publisherMock.Verify(x => x.Publish(
            It.Is<ControlSignalTowerCommand>(e =>
                e.InstrumentNumber == "SN001" &&
                e.ControlSignalTowerRequest.SetLight == EnumSignalTowerLightState.Red &&
                e.ControlSignalTowerRequest.SetSound == EnumSignalTowerControlSoundState.SoundContinuosOn),
            It.IsAny<CancellationToken>()), Times.Once);

        // SN002 should get the original AllInstruments request (not upgraded)
        _publisherMock.Verify(x => x.Publish(
            It.Is<ControlSignalTowerCommand>(e =>
                e.InstrumentNumber == "SN002" &&
                e.ControlSignalTowerRequest.SetLight == EnumSignalTowerLightState.Green &&
                e.ControlSignalTowerRequest.SetSound == EnumSignalTowerControlSoundState.SoundOff),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SoundUpgradePriority_UpgradesFromOffToShortSignal()
    {
        // Arrange
        var allInstrumentsRequest = new ControlSignalTowerRequest
        {
            SetLight = EnumSignalTowerLightState.Green,
            SetSound = EnumSignalTowerControlSoundState.SoundShortSignal
        };

        var instrumentSpecificRequest = new ControlSignalTowerRequest
        {
            SetLight = EnumSignalTowerLightState.Green,
            SetSound = EnumSignalTowerControlSoundState.SoundOff
        };

        var instruments = CreateTestInstruments();
        _instrumentManagerMock.Setup(x => x.GetInstruments()).Returns(instruments);
        SetupInstrumentManagerForIndividualInstruments(instruments);

        var command = new MassControlSignalTowerCommand(
            new Dictionary<string, ControlSignalTowerRequest> { { "SN001", instrumentSpecificRequest } },
            allInstrumentsRequest);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _publisherMock.Verify(x => x.Publish(
            It.Is<ControlSignalTowerCommand>(e =>
                e.InstrumentNumber == "SN001" &&
                e.ControlSignalTowerRequest.SetLight == EnumSignalTowerLightState.Green &&
                e.ControlSignalTowerRequest.SetSound == EnumSignalTowerControlSoundState.SoundShortSignal),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SoundUpgradePriority_UpgradesFromShortSignalToContinuous()
    {
        // Arrange
        var allInstrumentsRequest = new ControlSignalTowerRequest
        {
            SetLight = EnumSignalTowerLightState.Green,
            SetSound = EnumSignalTowerControlSoundState.SoundContinuosOn
        };

        var instrumentSpecificRequest = new ControlSignalTowerRequest
        {
            SetLight = EnumSignalTowerLightState.Green,
            SetSound = EnumSignalTowerControlSoundState.SoundShortSignal
        };

        var instruments = CreateTestInstruments();
        _instrumentManagerMock.Setup(x => x.GetInstruments()).Returns(instruments);
        SetupInstrumentManagerForIndividualInstruments(instruments);

        var command = new MassControlSignalTowerCommand(
            new Dictionary<string, ControlSignalTowerRequest> { { "SN001", instrumentSpecificRequest } },
            allInstrumentsRequest);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _publisherMock.Verify(x => x.Publish(
            It.Is<ControlSignalTowerCommand>(e =>
                e.InstrumentNumber == "SN001" &&
                e.ControlSignalTowerRequest.SetSound == EnumSignalTowerControlSoundState.SoundContinuosOn),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenLightStateChanged_SendsEvent()
    {
        // Arrange
        var request = new ControlSignalTowerRequest
        {
            SetLight = EnumSignalTowerLightState.Red,
            SetSound = EnumSignalTowerControlSoundState.SoundOff
        };

        var instruments = CreateTestInstruments();
        // Set up instrument state different from request
        ((Domain.Instrument)instruments[0]).SignalTower.SetState(SignalTowerLightState.Green, SignalTowerSoundState.Off);

        _instrumentManagerMock.Setup(x => x.GetInstruments()).Returns(instruments);
        SetupInstrumentManagerForIndividualInstruments(instruments);

        var command = new MassControlSignalTowerCommand(
            new Dictionary<string, ControlSignalTowerRequest> { { "SN001", request } },
            null);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _publisherMock.Verify(x => x.Publish(
            It.Is<ControlSignalTowerCommand>(e => e.InstrumentNumber == "SN001"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSoundStateNeedsSending_SendsEvent()
    {
        // Arrange
        var request = new ControlSignalTowerRequest
        {
            SetLight = EnumSignalTowerLightState.Green,
            SetSound = EnumSignalTowerControlSoundState.SoundShortSignal
        };

        var instruments = CreateTestInstruments();
        ((Domain.Instrument)instruments[0]).SignalTower.SetState(SignalTowerLightState.Green, SignalTowerSoundState.Off);

        _instrumentManagerMock.Setup(x => x.GetInstruments()).Returns(instruments);
        SetupInstrumentManagerForIndividualInstruments(instruments);

        var command = new MassControlSignalTowerCommand(
            new Dictionary<string, ControlSignalTowerRequest> { { "SN001", request } },
            null);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _publisherMock.Verify(x => x.Publish(
            It.Is<ControlSignalTowerCommand>(e => e.InstrumentNumber == "SN001"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(EnumSignalTowerLightState.Green, SignalTowerLightState.Green)]
    [InlineData(EnumSignalTowerLightState.Yellow, SignalTowerLightState.Yellow)]
    [InlineData(EnumSignalTowerLightState.Red, SignalTowerLightState.Red)]
    [InlineData(EnumSignalTowerLightState.RedBlink, SignalTowerLightState.FlashingRed)]
    public async Task Handle_MapsLightStatesCorrectly(EnumSignalTowerLightState requestState, SignalTowerLightState expectedMappedState)
    {
        // Arrange
        var request = new ControlSignalTowerRequest
        {
            SetLight = requestState,
            SetSound = EnumSignalTowerControlSoundState.SoundOff
        };

        var instruments = CreateTestInstruments();
        // Set instrument state to something different to ensure sending
        ((Domain.Instrument)instruments[0]).SignalTower.Reset();

        _instrumentManagerMock.Setup(x => x.GetInstruments()).Returns(instruments);
        SetupInstrumentManagerForIndividualInstruments(instruments);

        var command = new MassControlSignalTowerCommand(
            new Dictionary<string, ControlSignalTowerRequest> { { "SN001", request } },
            null);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _publisherMock.Verify(x => x.Publish(
            It.Is<ControlSignalTowerCommand>(e =>
                e.InstrumentNumber == "SN001" &&
                e.ControlSignalTowerRequest.SetLight == requestState),
            It.IsAny<CancellationToken>()),
            expectedMappedState != ((Domain.Instrument)instruments[0]).SignalTower.LightState ? Times.Once : Times.Never);
    }

    private static List<IInstrument> CreateTestInstruments()
    {
        return
        [
            new Domain.Instrument()
            {
                Id = 1,
                State = InstrumentState.Standby,
                SerialNumber = "SN001",
                SignalTower = new SignalTower()
            },


            new Domain.Instrument()
            {
                Id = 2,
                State = InstrumentState.Standby,
                SerialNumber = "SN002",
                SignalTower = new SignalTower()
            }
        ];
    }

    private void SetupInstrumentManagerForIndividualInstruments(ICollection<IInstrument> instruments)
    {
        foreach (var instrument in instruments)
        {
            _instrumentManagerMock.Setup(x => x.GetInstrument(instrument.SerialNumber))
                .Returns(instrument);
        }
    }

    [Fact]
    public async Task Handle_WithHigherSeverityAllInstruments_UpgradesInstrumentSpecific()
    {
        // Arrange
        var allInstrumentsRequest = new ControlSignalTowerRequest
        {
            SetLight = EnumSignalTowerLightState.Red,
            SetSound = EnumSignalTowerControlSoundState.SoundContinuosOn
        };

        var instrumentSpecificRequest = new ControlSignalTowerRequest
        {
            SetLight = EnumSignalTowerLightState.Yellow,
            SetSound = EnumSignalTowerControlSoundState.SoundOff
        };

        var instruments = CreateTestInstruments();
        _instrumentManagerMock.Setup(x => x.GetInstruments()).Returns(instruments);
        SetupInstrumentManagerForIndividualInstruments(instruments);

        var command = new MassControlSignalTowerCommand(
            new Dictionary<string, ControlSignalTowerRequest> { { "SN001", instrumentSpecificRequest } },
            allInstrumentsRequest);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        // SN001 should get upgraded to Red light and Continuous sound
        _publisherMock.Verify(x => x.Publish(
            It.Is<ControlSignalTowerCommand>(e =>
                e.InstrumentNumber == "SN001" &&
                e.ControlSignalTowerRequest.SetLight == EnumSignalTowerLightState.Red &&
                e.ControlSignalTowerRequest.SetSound == EnumSignalTowerControlSoundState.SoundContinuosOn),
            It.IsAny<CancellationToken>()), Times.Once);

        // SN002 should get the original AllInstruments request
        _publisherMock.Verify(x => x.Publish(
            It.Is<ControlSignalTowerCommand>(e =>
                e.InstrumentNumber == "SN002" &&
                e.ControlSignalTowerRequest.SetLight == EnumSignalTowerLightState.Red &&
                e.ControlSignalTowerRequest.SetSound == EnumSignalTowerControlSoundState.SoundContinuosOn),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}