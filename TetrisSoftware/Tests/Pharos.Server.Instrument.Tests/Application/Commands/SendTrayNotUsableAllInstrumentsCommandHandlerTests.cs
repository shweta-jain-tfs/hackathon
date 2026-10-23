using Microsoft.Extensions.Logging;
using Moq;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Commands.SetTrayNotUsable;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Instrument.SetTrayNotUsable;

namespace Pharos.Server.Instrument.Application.Commands;

public class SendTrayNotUsableAllInstrumentsCommandHandlerTests
{
    private readonly Mock<ICommunicationSessionManager> _sessionManagerMock = new();
    private readonly Mock<IInstrumentManager> _instrumentManagerMock = new();
    private readonly SendTrayNotUsableAllInstrumentsCommandHandler _sut;

    public SendTrayNotUsableAllInstrumentsCommandHandlerTests()
    {
        _sut = new SendTrayNotUsableAllInstrumentsCommandHandler(
            _sessionManagerMock.Object,
            _instrumentManagerMock.Object,
            Mock.Of<ILogger<SendTrayNotUsableAllInstrumentsCommandHandler>>());
    }

    [Fact]
    public async Task Handle_WithAvailableInstruments_SendsTrayNotUsableRequestToEachInstrument()
    {
        // Arrange
        var firstInstrument = CreateInstrument("SN001", InstrumentState.Standby);
        var secondInstrument = CreateInstrument("SN002", InstrumentState.Assay);
        var firstConnectionMock = CreateConnectionMock();
        var secondConnectionMock = CreateConnectionMock();
        _instrumentManagerMock.Setup(x => x.GetInstruments()).Returns([firstInstrument, secondInstrument]);
        _sessionManagerMock.Setup(x => x.GetSession(firstInstrument.SerialNumber)).Returns(CreateSession(firstInstrument.SerialNumber, firstConnectionMock.Object));
        _sessionManagerMock.Setup(x => x.GetSession(secondInstrument.SerialNumber)).Returns(CreateSession(secondInstrument.SerialNumber, secondConnectionMock.Object));
        var command = new SendTrayNotUsableAllInstrumentsCommand("TRAY-123");

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        firstConnectionMock.Verify(x => x.SetTrayNotUsableAsync(
            It.Is<TrayNotUsableRequest>(request => request.TrayBarcode == command.TrayBarcode),
            It.IsAny<CancellationToken>()), Times.Once);
        secondConnectionMock.Verify(x => x.SetTrayNotUsableAsync(
            It.Is<TrayNotUsableRequest>(request => request.TrayBarcode == command.TrayBarcode),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(InstrumentState.Offline)]
    [InlineData(InstrumentState.Limited)]
    [InlineData(InstrumentState.Synchronizing)]
    public async Task Handle_WithUnavailableInstrument_DoesNotRetrieveItsSession(InstrumentState state)
    {
        // Arrange
        var instrument = CreateInstrument("SN001", state);
        _instrumentManagerMock.Setup(x => x.GetInstruments()).Returns([instrument]);

        // Act
        await _sut.Handle(new SendTrayNotUsableAllInstrumentsCommand("TRAY-123"), TestContext.Current.CancellationToken);

        // Assert
        _sessionManagerMock.Verify(x => x.GetSession(instrument.SerialNumber), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInstrumentHasNoSession_DoesNotSendRequest()
    {
        // Arrange
        var instrument = CreateInstrument("SN001", InstrumentState.Standby);
        _instrumentManagerMock.Setup(x => x.GetInstruments()).Returns([instrument]);
        _sessionManagerMock.Setup(x => x.GetSession(instrument.SerialNumber)).Returns((CommunicationSession?)null);

        // Act
        await _sut.Handle(new SendTrayNotUsableAllInstrumentsCommand("TRAY-123"), TestContext.Current.CancellationToken);

        // Assert
        _sessionManagerMock.Verify(x => x.GetSession(instrument.SerialNumber), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSendingToAnInstrumentFails_ContinuesWithRemainingInstruments()
    {
        // Arrange
        var failingInstrument = CreateInstrument("SN001", InstrumentState.Standby);
        var succeedingInstrument = CreateInstrument("SN002", InstrumentState.Standby);
        var failingConnectionMock = CreateConnectionMock();
        var succeedingConnectionMock = CreateConnectionMock();
        failingConnectionMock.Setup(x => x.SetTrayNotUsableAsync(It.IsAny<TrayNotUsableRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());
        _instrumentManagerMock.Setup(x => x.GetInstruments()).Returns([failingInstrument, succeedingInstrument]);
        _sessionManagerMock.Setup(x => x.GetSession(failingInstrument.SerialNumber)).Returns(CreateSession(failingInstrument.SerialNumber, failingConnectionMock.Object));
        _sessionManagerMock.Setup(x => x.GetSession(succeedingInstrument.SerialNumber)).Returns(CreateSession(succeedingInstrument.SerialNumber, succeedingConnectionMock.Object));
        var command = new SendTrayNotUsableAllInstrumentsCommand("TRAY-123");

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        succeedingConnectionMock.Verify(x => x.SetTrayNotUsableAsync(
            It.Is<TrayNotUsableRequest>(request => request.TrayBarcode == command.TrayBarcode),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Domain.Instrument CreateInstrument(string serialNumber, InstrumentState state) => new()
    {
        SerialNumber = serialNumber,
        State = state
    };

    private static Mock<IInstrumentConnection> CreateConnectionMock()
    {
        var connectionMock = new Mock<IInstrumentConnection>();
        connectionMock.Setup(x => x.SetTrayNotUsableAsync(It.IsAny<TrayNotUsableRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return connectionMock;
    }

    private static CommunicationSession CreateSession(string instrumentNumber, IInstrumentConnection connection) => new()
    {
        InstrumentNumber = instrumentNumber,
        Connection = connection
    };
}
