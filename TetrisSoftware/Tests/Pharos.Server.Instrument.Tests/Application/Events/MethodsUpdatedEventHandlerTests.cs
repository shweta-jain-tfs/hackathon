using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Instrument.Application.Commands.InitializeInstrumentTechnologies;
using Pharos.Server.Instrument.Application.Commands.SendActiveMethods;
using Pharos.Server.Instrument.Application.Commands.UpdateMethods;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Application.Events;

public class MethodsUpdatedEventHandlerTests
{
    [Fact]
    [Trait("ProductRequirement", "PR_2767")]
    public async Task Handle_WhenMethodsAreUpdated_ShouldRefreshTechnologiesAndConnectedInstruments()
    {
        // Arrange
        var connectedInstrument = CreateInstrument(1, "SEED_001");
        var offlineInstrument = CreateInstrument(2, "SEED_002");
        var dbContextMock = new Mock<IInstrumentDbContext>();
        dbContextMock.Setup(x => x.Instruments).ReturnsDbSet([connectedInstrument, offlineInstrument]);
        var senderMock = new Mock<ISender>();
        senderMock.Setup(x => x.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sessionManagerMock = new Mock<ICommunicationSessionManager>();
        sessionManagerMock.Setup(x => x.GetSession(connectedInstrument.SerialNumber)).Returns(CreateSession(connectedInstrument.SerialNumber));
        var currentInstrumentHubMock = new Mock<ICurrentInstrumentServerHub>();
        currentInstrumentHubMock.Setup(x => x.InstrumentMethodsChanged(It.IsAny<int>())).Returns(Task.CompletedTask);
        var instrumentHubMock = new Mock<IInstrumentHub>();
        instrumentHubMock.Setup(x => x.InstrumentTechnologiesChanged(It.IsAny<int>())).Returns(Task.CompletedTask);
        var sut = new MethodsUpdatedEventHandler(
            dbContextMock.Object,
            senderMock.Object,
            sessionManagerMock.Object,
            currentInstrumentHubMock.Object,
            instrumentHubMock.Object,
            new Mock<ILogger<MethodsUpdatedEventHandler>>().Object);

        // Act
        await sut.Handle(new MethodsUpdatedEvent(), CancellationToken.None);

        // Assert
        senderMock.Verify(x => x.Send(new InitializeInstrumentTechnologiesCommand(connectedInstrument.Id), It.IsAny<CancellationToken>()), Times.Once);
        senderMock.Verify(x => x.Send(new InitializeInstrumentTechnologiesCommand(offlineInstrument.Id), It.IsAny<CancellationToken>()), Times.Once);
        senderMock.Verify(x => x.Send(new UpdateMethodsCommand(connectedInstrument.SerialNumber), It.IsAny<CancellationToken>()), Times.Once);
        senderMock.Verify(x => x.Send(new SendActiveMethodsCommand(connectedInstrument.SerialNumber), It.IsAny<CancellationToken>()), Times.Once);
        senderMock.Verify(x => x.Send(It.Is<UpdateMethodsCommand>(command => command.InstrumentNumber == offlineInstrument.SerialNumber), It.IsAny<CancellationToken>()), Times.Never);
        currentInstrumentHubMock.Verify(x => x.InstrumentMethodsChanged(It.IsAny<int>()), Times.Exactly(2));
        instrumentHubMock.Verify(x => x.InstrumentTechnologiesChanged(It.IsAny<int>()), Times.Exactly(2));
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2767")]
    public async Task Handle_WhenMethodEnabledChanges_ShouldRefreshMethodsAndActiveMethods()
    {
        // Arrange
        var instrument = CreateInstrument(1, "SEED_001");
        var dbContextMock = new Mock<IInstrumentDbContext>();
        dbContextMock.Setup(x => x.Instruments).ReturnsDbSet([instrument]);
        var senderMock = new Mock<ISender>();
        senderMock.Setup(x => x.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sessionManagerMock = new Mock<ICommunicationSessionManager>();
        sessionManagerMock.Setup(x => x.GetSession(instrument.SerialNumber)).Returns(CreateSession(instrument.SerialNumber));
        var currentInstrumentHubMock = new Mock<ICurrentInstrumentServerHub>();
        currentInstrumentHubMock.Setup(x => x.InstrumentMethodsChanged(It.IsAny<int>())).Returns(Task.CompletedTask);
        var instrumentHubMock = new Mock<IInstrumentHub>();
        instrumentHubMock.Setup(x => x.InstrumentTechnologiesChanged(It.IsAny<int>())).Returns(Task.CompletedTask);
        var sut = new MethodsUpdatedEventHandler(
            dbContextMock.Object,
            senderMock.Object,
            sessionManagerMock.Object,
            currentInstrumentHubMock.Object,
            instrumentHubMock.Object,
            new Mock<ILogger<MethodsUpdatedEventHandler>>().Object);

        // Act
        await sut.Handle(new MethodEnabledEvent("sIgE", true), CancellationToken.None);

        // Assert
        senderMock.Verify(x => x.Send(new InitializeInstrumentTechnologiesCommand(instrument.Id), It.IsAny<CancellationToken>()), Times.Once);
        senderMock.Verify(x => x.Send(new UpdateMethodsCommand(instrument.SerialNumber), It.IsAny<CancellationToken>()), Times.Once);
        senderMock.Verify(x => x.Send(new SendActiveMethodsCommand(instrument.SerialNumber), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Domain.Instrument CreateInstrument(int id, string serialNumber) =>
        new()
        {
            Id = id,
            SerialNumber = serialNumber
        };

    private static CommunicationSession CreateSession(string instrumentNumber) =>
        new()
        {
            SessionId = Guid.NewGuid(),
            InstrumentNumber = instrumentNumber
        };
}
