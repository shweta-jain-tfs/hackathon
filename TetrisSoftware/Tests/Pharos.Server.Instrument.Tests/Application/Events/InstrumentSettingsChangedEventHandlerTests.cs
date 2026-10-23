using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Instrument.Application.Commands.UpdateSettings;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Application.Events;

public class InstrumentSettingsChangedEventHandlerTests
{
    [Fact]
    [Trait("ProductRequirement", "PR_2803")]
    public async Task Handle_WhenTubeBarcodeSettingsChange_ShouldUpdateAllCompatibleInstruments()
    {
        // Arrange
        var compatibleInstrument = CreateInstrument("SEED_001");
        var incompatibleInstrument = CreateInstrument("SEED_002");
        var instrumentManagerMock = new Mock<IInstrumentManager>();
        instrumentManagerMock
            .Setup(x => x.GetInstruments())
            .Returns([compatibleInstrument, incompatibleInstrument]);

        var sessionManagerMock = new Mock<ICommunicationSessionManager>();
        sessionManagerMock
            .Setup(x => x.GetSession(compatibleInstrument.SerialNumber))
            .Returns(CreateSession(compatibleInstrument.SerialNumber, EnumCompatibilityStatus.FullyCompatible));
        sessionManagerMock
            .Setup(x => x.GetSession(incompatibleInstrument.SerialNumber))
            .Returns(CreateSession(incompatibleInstrument.SerialNumber, EnumCompatibilityStatus.FilesApiOnly));

        var senderMock = new Mock<ISender>();
        senderMock
            .Setup(x => x.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new InstrumentSettingsChangedEventHandler(
            senderMock.Object,
            instrumentManagerMock.Object,
            sessionManagerMock.Object,
            new Mock<ILogger<InstrumentSettingsChangedEventHandler>>().Object);

        // Act
        await sut.Handle(new SettingsChangedEvent<TubeBarcodesSettings>(new TubeBarcodesSettings(), new TubeBarcodesSettings()), CancellationToken.None);

        // Assert
        senderMock.Verify(
            x => x.Send(It.Is<UpdateSettingsCommand>(command => command.InstrumentNumber == compatibleInstrument.SerialNumber), It.IsAny<CancellationToken>()),
            Times.Once);
        senderMock.Verify(
            x => x.Send(It.Is<UpdateSettingsCommand>(command => command.InstrumentNumber == incompatibleInstrument.SerialNumber), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Domain.Instrument CreateInstrument(string serialNumber) =>
        new()
        {
            SerialNumber = serialNumber
        };

    private static CommunicationSession CreateSession(string instrumentNumber, EnumCompatibilityStatus compatibilityStatus) =>
        new()
        {
            SessionId = Guid.NewGuid(),
            InstrumentNumber = instrumentNumber,
            CompatibilityStatus = compatibilityStatus
        };
}
