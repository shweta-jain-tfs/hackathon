using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Instrument.Application.Commands.InitializeInstrumentMethods;
using Pharos.Server.Instrument.Application.Commands.SendActiveMethods;
using Pharos.Server.Instrument.Application.Commands.UpdateInstrumentTechnologies;
using Pharos.Server.Instrument.Application.Commands.UpdateMethods;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Domain;

namespace Pharos.Server.Instrument.Application.Commands;

public class UpdateInstrumentTechnologiesCommandHandlerTests
{
    private readonly List<Domain.Instrument> _instruments = [];
    private readonly List<InstrumentTechnology> _instrumentTechnologies = [];
    private readonly Mock<IInstrumentDbContext> _dbContextMock = new();
    private readonly Mock<ISender> _senderMock = new();
    private readonly Mock<ICurrentInstrumentServerHub> _currentInstrumentHubMock = new();
    private readonly Mock<IInstrumentHub> _instrumentHubMock = new();
    private readonly UpdateInstrumentTechnologiesCommandHandler _sut;

    public UpdateInstrumentTechnologiesCommandHandlerTests()
    {
        _dbContextMock.Setup(x => x.Instruments).ReturnsDbSet(_instruments);
        _dbContextMock.Setup(x => x.InstrumentTechnologies).ReturnsDbSet(_instrumentTechnologies);
        _senderMock.Setup(x => x.Send(It.IsAny<InitializeInstrumentMethodsCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _senderMock.Setup(x => x.Send(It.IsAny<UpdateMethodsCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _senderMock.Setup(x => x.Send(It.IsAny<SendActiveMethodsCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _currentInstrumentHubMock.Setup(x => x.InstrumentMethodsChanged(It.IsAny<int>())).Returns(Task.CompletedTask);
        _instrumentHubMock.Setup(x => x.InstrumentTechnologiesChanged(It.IsAny<int>())).Returns(Task.CompletedTask);

        _sut = new UpdateInstrumentTechnologiesCommandHandler(
            _dbContextMock.Object,
            _senderMock.Object,
            _currentInstrumentHubMock.Object,
            _instrumentHubMock.Object,
            new Mock<ILogger<UpdateInstrumentTechnologiesCommandHandler>>().Object);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2767")]
    public async Task Handle_WhenSelectionChangesForConnectedInstrument_ShouldSaveAndSynchronizeMethods()
    {
        // Arrange
        var instrument = AddInstrument();
        _instrumentTechnologies.AddRange(
        [
            CreateTechnology(instrument.Id, AssayTechnology.ImmunoCap, true),
            CreateTechnology(instrument.Id, AssayTechnology.EliA, false)
        ]);
        _dbContextMock.Setup(x => x.HasChanges()).Returns(true);

        // Act
        await _sut.Handle(CreateCommand(instrument.Id, (AssayTechnology.ImmunoCap, false), (AssayTechnology.EliA, true)), CancellationToken.None);

        // Assert
        _instrumentTechnologies.Should().OnlyContain(x => x.IsActive == (x.Technology == AssayTechnology.EliA));
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _senderMock.Verify(x => x.Send(new InitializeInstrumentMethodsCommand(instrument.Id), It.IsAny<CancellationToken>()), Times.Once);
        _senderMock.Verify(x => x.Send(new UpdateMethodsCommand(instrument.SerialNumber), It.IsAny<CancellationToken>()), Times.Once);
        _senderMock.Verify(x => x.Send(new SendActiveMethodsCommand(instrument.SerialNumber), It.IsAny<CancellationToken>()), Times.Once);
        _currentInstrumentHubMock.Verify(x => x.InstrumentMethodsChanged(instrument.Id), Times.Once);
        _instrumentHubMock.Verify(x => x.InstrumentTechnologiesChanged(instrument.Id), Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2767")]
    public async Task Handle_WhenSelectionChanges_ShouldRequestInstrumentSynchronizationAndUpdateClients()
    {
        // Arrange
        var instrument = AddInstrument();
        _instrumentTechnologies.Add(CreateTechnology(instrument.Id, AssayTechnology.ImmunoCap, false));
        _dbContextMock.Setup(x => x.HasChanges()).Returns(true);

        // Act
        await _sut.Handle(CreateCommand(instrument.Id, (AssayTechnology.ImmunoCap, true)), CancellationToken.None);

        // Assert
        _instrumentTechnologies.Should().ContainSingle().Which.IsActive.Should().BeTrue();
        _senderMock.Verify(x => x.Send(new InitializeInstrumentMethodsCommand(instrument.Id), It.IsAny<CancellationToken>()), Times.Once);
        _senderMock.Verify(x => x.Send(new UpdateMethodsCommand(instrument.SerialNumber), It.IsAny<CancellationToken>()), Times.Once);
        _senderMock.Verify(x => x.Send(new SendActiveMethodsCommand(instrument.SerialNumber), It.IsAny<CancellationToken>()), Times.Once);
        _currentInstrumentHubMock.Verify(x => x.InstrumentMethodsChanged(instrument.Id), Times.Once);
        _instrumentHubMock.Verify(x => x.InstrumentTechnologiesChanged(instrument.Id), Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2767")]
    public async Task Handle_WhenDuplicateTechnologiesAreSubmitted_ShouldThrowValidationException()
    {
        // Arrange
        var instrument = AddInstrument();

        // Act
        var action = () => _sut.Handle(
            CreateCommand(instrument.Id, (AssayTechnology.ImmunoCap, true), (AssayTechnology.ImmunoCap, false)),
            CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<ValidationException>();
        VerifyNoChangesWereApplied();
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2767")]
    public async Task Handle_WhenUnavailableTechnologyIsSubmitted_ShouldThrowValidationException()
    {
        // Arrange
        var instrument = AddInstrument();
        _instrumentTechnologies.Add(CreateTechnology(instrument.Id, AssayTechnology.ImmunoCap, false));

        // Act
        var action = () => _sut.Handle(CreateCommand(instrument.Id, (AssayTechnology.EliA, true)), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<ValidationException>();
        VerifyNoChangesWereApplied();
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2767")]
    public async Task Handle_WhenInstrumentDoesNotExist_ShouldThrowObjectGoneException()
    {
        // Arrange

        // Act
        var action = () => _sut.Handle(CreateCommand(1, (AssayTechnology.ImmunoCap, true)), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<ObjectGoneException>();
        VerifyNoChangesWereApplied();
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2767")]
    public async Task Handle_WhenSelectionChanges_ShouldNotChangeAnotherInstrumentSelection()
    {
        // Arrange
        var instrument = AddInstrument();
        var otherInstrument = new Domain.Instrument
        {
            Id = 2,
            SerialNumber = "SEED_002"
        };
        _instruments.Add(otherInstrument);
        var otherInstrumentTechnology = CreateTechnology(otherInstrument.Id, AssayTechnology.EliA, true);
        _instrumentTechnologies.AddRange(
        [
            CreateTechnology(instrument.Id, AssayTechnology.ImmunoCap, false),
            otherInstrumentTechnology
        ]);
        _dbContextMock.Setup(x => x.HasChanges()).Returns(true);

        // Act
        await _sut.Handle(CreateCommand(instrument.Id, (AssayTechnology.ImmunoCap, true)), CancellationToken.None);

        // Assert
        otherInstrumentTechnology.IsActive.Should().BeTrue();
    }

    private Domain.Instrument AddInstrument()
    {
        var instrument = new Domain.Instrument
        {
            Id = 1,
            SerialNumber = "SEED_001"
        };
        _instruments.Add(instrument);
        return instrument;
    }

    private static UpdateInstrumentTechnologiesCommand CreateCommand(
        int instrumentId,
        params (AssayTechnology Technology, bool IsSelected)[] technologies) =>
        new()
        {
            InstrumentId = instrumentId,
            Technologies = technologies
                .Select(technology => new UpdateInstrumentTechnologyDto
                {
                    Technology = technology.Technology,
                    IsSelected = technology.IsSelected
                })
                .ToList()
        };

    private static InstrumentTechnology CreateTechnology(int instrumentId, AssayTechnology technology, bool isActive) =>
        new()
        {
            InstrumentId = instrumentId,
            Technology = technology,
            IsActive = isActive
        };

    private void VerifyNoChangesWereApplied()
    {
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _senderMock.Verify(x => x.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _currentInstrumentHubMock.Verify(x => x.InstrumentMethodsChanged(It.IsAny<int>()), Times.Never);
        _instrumentHubMock.Verify(x => x.InstrumentTechnologiesChanged(It.IsAny<int>()), Times.Never);
    }
}
