using System.Net;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Server.Instrument.Application.Commands.InitializeInstrumentTechnologies;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Services;

namespace Pharos.Server.Instrument.Application.Commands.CreateInstrument;

public sealed class CreateInstrumentCommandHandlerTests
{
    private readonly Mock<IInstrumentDbContext> _dbContext = new();
    private readonly Mock<ISender> _sender = new();
    private readonly InstrumentManager _instrumentManager;

    public CreateInstrumentCommandHandlerTests()
    {
        var sessionManager = new Mock<ICommunicationSessionManager>();
        _instrumentManager = new InstrumentManager(
            sessionManager.Object,
            new Mock<ILogger<InstrumentManager>>().Object,
            new Mock<IServiceScopeFactory>().Object);

        _sender
            .Setup(value => value.Send(
                It.IsAny<InitializeInstrumentTechnologiesCommand>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task NewInstrument_ShouldDelegateCreationToCreateInstrumentCommand()
    {
        Domain.Instrument? addedInstrument = null;
        _dbContext.Setup(value => value.Instruments).ReturnsDbSet([]);
        _dbContext
            .Setup(value => value.Instruments.Add(It.IsAny<Domain.Instrument>()))
            .Callback<Domain.Instrument>(instrument => addedInstrument = instrument);

        var handler = new CreateInstrumentCommandHandler(_dbContext.Object, _sender.Object, _instrumentManager);
        var command = new CreateInstrumentCommand(IPAddress.Loopback, "E500-123", "4.5.6");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Same(addedInstrument, result);
        Assert.Equal("E500-123", result.SerialNumber);
        Assert.Equal("4.5.6", result.InstrumentSoftwareVersion);
        Assert.Same(result, _instrumentManager.FindInstrument("E500-123"));
        _dbContext.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sender.Verify(
            value => value.Send(
                It.Is<InitializeInstrumentTechnologiesCommand>(initialization =>
                    initialization.InstrumentId == result.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
