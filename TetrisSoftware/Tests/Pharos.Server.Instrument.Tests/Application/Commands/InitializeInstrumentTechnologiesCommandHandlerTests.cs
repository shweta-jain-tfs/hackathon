using System.Linq.Expressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Instrument.Application.Commands.InitializeInstrumentMethods;
using Pharos.Server.Instrument.Application.Commands.InitializeInstrumentTechnologies;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Domain;

namespace Pharos.Server.Instrument.Application.Commands;

public class InitializeInstrumentTechnologiesCommandHandlerTests
{
    private readonly List<InstrumentTechnology> _instrumentTechnologies = [];
    private readonly Mock<IInstrumentDbContext> _dbContextMock = new();
    private readonly Mock<IMethodsService> _methodsServiceMock = new();
    private readonly Mock<ISender> _senderMock = new();
    private readonly Mock<DbSet<InstrumentTechnology>> _instrumentTechnologiesDbSetMock;
    private readonly InitializeInstrumentTechnologiesCommandHandler _sut;

    public InitializeInstrumentTechnologiesCommandHandlerTests()
    {
        _dbContextMock.Setup(x => x.InstrumentTechnologies).ReturnsDbSet(_instrumentTechnologies);
        _instrumentTechnologiesDbSetMock = Mock.Get(_dbContextMock.Object.InstrumentTechnologies);
        _methodsServiceMock
            .Setup(x => x.GetMethods(It.IsAny<Expression<Func<Method, bool>>>(), It.IsAny<Expression<Func<Method, AssayTechnology>>>()))
            .ReturnsAsync((Expression<Func<Method, bool>> filter, Expression<Func<Method, AssayTechnology>> selector) =>
                _availableMethods.Where(filter.Compile()).Select(selector.Compile()).ToList());
        _senderMock
            .Setup(x => x.Send(It.IsAny<InitializeInstrumentMethodsCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new InitializeInstrumentTechnologiesCommandHandler(
            _dbContextMock.Object,
            _methodsServiceMock.Object,
            _senderMock.Object);
    }

    private readonly List<Method> _availableMethods = [];

    [Fact]
    [Trait("ProductRequirement", "PR_2767")]
    public async Task Handle_WhenTechnologiesAreAvailable_ShouldAddMissingTechnologiesAsActive()
    {
        // Arrange
        _availableMethods.AddRange(
        [
            CreateMethod(AssayTechnology.ImmunoCap),
            CreateMethod(AssayTechnology.ImmunoCap),
            CreateMethod(AssayTechnology.EliA)
        ]);

        // Act
        await _sut.Handle(new InitializeInstrumentTechnologiesCommand(1), CancellationToken.None);

        // Assert
        _instrumentTechnologiesDbSetMock.Verify(
            x => x.AddRangeAsync(
                It.Is<IEnumerable<InstrumentTechnology>>(technologies => technologies.Select(x => x.Technology)
                    .OrderBy(x => x)
                    .SequenceEqual(new[] { AssayTechnology.ImmunoCap, AssayTechnology.EliA })
                    && technologies.All(x => x.IsActive)),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _senderMock.Verify(x => x.Send(new InitializeInstrumentMethodsCommand(1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2767")]
    public async Task Handle_WhenNewTechnologyBecomesAvailable_ShouldSelectNewTechnologyAndPreserveExistingSelection()
    {
        // Arrange
        _availableMethods.AddRange(
        [
            CreateMethod(AssayTechnology.ImmunoCap),
            CreateMethod(AssayTechnology.EliA)
        ]);
        _instrumentTechnologies.Add(new InstrumentTechnology { InstrumentId = 1, Technology = AssayTechnology.ImmunoCap, IsActive = false });

        // Act
        await _sut.Handle(new InitializeInstrumentTechnologiesCommand(1), CancellationToken.None);

        // Assert
        _instrumentTechnologies.Single(x => x.Technology == AssayTechnology.ImmunoCap).IsActive.Should().BeFalse();
        _instrumentTechnologiesDbSetMock.Verify(
            x => x.AddRangeAsync(
                It.Is<IEnumerable<InstrumentTechnology>>(technologies =>
                    technologies.Count() == 1
                    && technologies.Single().Technology == AssayTechnology.EliA
                    && technologies.Single().IsActive),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2767")]
    public async Task Handle_WhenTechnologyIsTemporarilyUnavailable_ShouldPreserveItsSelectionAndInitializeMethods()
    {
        // Arrange
        _availableMethods.Add(CreateMethod(AssayTechnology.ImmunoCap));
        _instrumentTechnologies.AddRange(
        [
            new InstrumentTechnology { InstrumentId = 1, Technology = AssayTechnology.ImmunoCap, IsActive = true },
            new InstrumentTechnology { InstrumentId = 1, Technology = AssayTechnology.ViewAllergy, IsActive = false }
        ]);

        // Act
        await _sut.Handle(new InitializeInstrumentTechnologiesCommand(1), CancellationToken.None);

        // Assert
        _instrumentTechnologies.Single(x => x.Technology == AssayTechnology.ViewAllergy).IsActive.Should().BeFalse();
        _instrumentTechnologiesDbSetMock.Verify(x => x.RemoveRange(It.IsAny<IEnumerable<InstrumentTechnology>>()), Times.Never);
        _senderMock.Verify(
            x => x.Send(new InitializeInstrumentMethodsCommand(1), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2767")]
    public async Task Handle_WhenTechnologyBecomesAvailableAgain_ShouldRestoreItsPriorSelection()
    {
        // Arrange
        _instrumentTechnologies.Add(new InstrumentTechnology
        {
            InstrumentId = 1,
            Technology = AssayTechnology.EliA,
            IsActive = false
        });

        // Act: no methods are currently available for EliA.
        await _sut.Handle(new InitializeInstrumentTechnologiesCommand(1), CancellationToken.None);

        // Act: a later package makes EliA available again.
        _availableMethods.Add(CreateMethod(AssayTechnology.EliA));
        await _sut.Handle(new InitializeInstrumentTechnologiesCommand(1), CancellationToken.None);

        // Assert
        _instrumentTechnologies.Single(x => x.Technology == AssayTechnology.EliA).IsActive.Should().BeFalse();
        _instrumentTechnologiesDbSetMock.Verify(x => x.RemoveRange(It.IsAny<IEnumerable<InstrumentTechnology>>()), Times.Never);
        _instrumentTechnologiesDbSetMock.Verify(x => x.AddRangeAsync(It.IsAny<IEnumerable<InstrumentTechnology>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Method CreateMethod(AssayTechnology technology) =>
        new()
        {
            Technology = technology,
            Enabled = true
        };
}
