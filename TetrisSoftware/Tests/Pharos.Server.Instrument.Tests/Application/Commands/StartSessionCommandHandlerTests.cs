using System.Net;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application;
using Pharos.Server.Instrument.Application.Commands.CreateInstrument;
using Pharos.Server.Instrument.Application.Commands.StartSession;
using Pharos.Server.Instrument.Application.Events;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Services;

namespace Pharos.Server.Instrument.Application.Commands;

public class StartSessionCommandHandlerTests
{
    private readonly Mock<IInstrumentDbContext> _dbContext;
    private readonly Mock<IMediator> _mediator;
    private readonly InstrumentManager _instrumentManager;
    private readonly StartSessionCommand _command;
    private readonly StartSessionCommandHandler _sut;

    public StartSessionCommandHandlerTests()
    {
        _dbContext = new Mock<IInstrumentDbContext>();

        var sessionManager = new Mock<ICommunicationSessionManager>();
        sessionManager
            .Setup(x => x.StartSessionAsync(
                It.IsAny<IPAddress>(),
                It.IsAny<int>(),
                It.IsAny<Guid>(),
                It.IsAny<string>()))
            .ReturnsAsync(new CommunicationSession());

        _instrumentManager = new InstrumentManager(
            sessionManager.Object,
            new Mock<ILogger<InstrumentManager>>().Object,
            new Mock<IServiceScopeFactory>().Object);

        var protocolInformation = new Mock<ITetrisProtocolInformation>();
        protocolInformation.SetupGet(x => x.Version).Returns(new Version(4, 2, 3));
        protocolInformation.SetupGet(x => x.VersionString).Returns("4.2.3");

        _mediator = new Mock<IMediator>();
        _mediator
            .Setup(x => x.Send(
                It.IsAny<CreateInstrumentCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateInstrumentCommand command, CancellationToken _) => new Domain.Instrument
            {
                Id = 1,
                IpAddress = command.IpAddress,
                SerialNumber = command.InstrumentNumber,
                InstrumentSoftwareVersion = command.InstrumentSoftwareVersion
            });
        _mediator
            .Setup(x => x.Publish(
                It.IsAny<InstrumentStateChangedEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mediator
            .Setup(x => x.Publish(
                It.IsAny<InstrumentConnectedEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _command = new StartSessionCommand(
            IPAddress.Loopback,
            Guid.NewGuid(),
            "E500-123",
            "4.2.3",
            "4.5.6",
            5001);

        _sut = new StartSessionCommandHandler(
            sessionManager.Object,
            _instrumentManager,
            new Mock<IPharosVersionService>().Object,
            protocolInformation.Object,
            _dbContext.Object,
            _mediator.Object);
    }

    [Fact]
    public async Task NewInstrument_ShouldPassSoftwareVersionToCreateCommand()
    {
        // Act
        await _sut.Handle(_command, TestContext.Current.CancellationToken);

        // Assert
        _mediator.Verify(
            x => x.Send(
                It.Is<CreateInstrumentCommand>(command =>
                    command.InstrumentNumber == "E500-123" &&
                    command.InstrumentSoftwareVersion == "4.5.6"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExistingInstrumentWithChangedSoftwareVersion_ShouldPersistNewVersion()
    {
        // Arrange
        var instrumentInDb = CreateInstrument("4.0.0");
        var instrumentInMemory = CreateInstrument("4.0.0");
        _dbContext.Setup(x => x.Instruments).ReturnsDbSet([instrumentInDb]);
        _instrumentManager.AddInstrument(instrumentInMemory);

        // Act
        await _sut.Handle(_command, TestContext.Current.CancellationToken);

        // Assert
        instrumentInDb.InstrumentSoftwareVersion.Should().Be("4.5.6");
        _dbContext.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExistingInstrumentWithChangedSoftwareVersion_ShouldUpdateVersionInMemory()
    {
        // Arrange
        var instrumentInDb = CreateInstrument("4.0.0");
        var instrumentInMemory = CreateInstrument("4.0.0");
        _dbContext.Setup(x => x.Instruments).ReturnsDbSet([instrumentInDb]);
        _instrumentManager.AddInstrument(instrumentInMemory);

        // Act
        await _sut.Handle(_command, TestContext.Current.CancellationToken);

        // Assert
        instrumentInMemory.InstrumentSoftwareVersion.Should().Be("4.5.6");
    }

    [Fact]
    public async Task ExistingInstrumentWithChangedSoftwareVersion_ShouldRaiseInstrumentUpdatedEvent()
    {
        // Arrange
        var instrumentInDb = CreateInstrument("4.0.0");
        var instrumentInMemory = CreateInstrument("4.0.0");
        _dbContext.Setup(x => x.Instruments).ReturnsDbSet([instrumentInDb]);
        _instrumentManager.AddInstrument(instrumentInMemory);

        // Act
        await _sut.Handle(_command, TestContext.Current.CancellationToken);

        // Assert
        instrumentInDb.DomainEvents.Should().ContainSingle(
            domainEvent => domainEvent is InstrumentUpdatedEvent);
    }

    private static Domain.Instrument CreateInstrument(string instrumentSoftwareVersion)
    {
        return new Domain.Instrument
        {
            Id = 1,
            SerialNumber = "E500-123",
            DisplayName = "E500",
            Enabled = true,
            IpAddress = IPAddress.Loopback,
            InstrumentSoftwareVersion = instrumentSoftwareVersion
        };
    }
}
