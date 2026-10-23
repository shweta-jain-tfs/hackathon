using FluentAssertions;
using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Reflex.Application.Commands.SetActiveReflexDefinitionCommand;
using Pharos.Server.Reflex.Domain;
using Pharos.Server.Reflex.Interfaces;
using Xunit;

namespace Pharos.Server.Reflex.Application.Commands;

public class SetActiveReflexDefinitionCommandHandlerTests
{
    private readonly Mock<IReflexDbContext> _dbContextMock;
    private readonly SetActiveReflexDefinitionCommandHandler _sut;

    public SetActiveReflexDefinitionCommandHandlerTests()
    {
        _dbContextMock = new Mock<IReflexDbContext>();
        _sut = new SetActiveReflexDefinitionCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_Should_SetActiveToTrue_When_CommandActiveIsTrue()
    {
        // Arrange
        var reflexDefinition = new ReflexDefinition
        {
            Id = 1,
            Name = "Test Reflex",
            Active = false,
            Automatic = true,
            ReflexCondition = new ReflexCondition(),
            ReflexTests = new List<ReflexTest>()
        };

        var reflexDefinitions = new List<ReflexDefinition> { reflexDefinition };
        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new SetActiveReflexDefinitionCommand.SetActiveReflexDefinitionCommand(1, true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        reflexDefinition.Active.Should().BeTrue();
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_SetActiveToFalse_When_CommandActiveIsFalse()
    {
        // Arrange
        var reflexDefinition = new ReflexDefinition
        {
            Id = 1,
            Name = "Test Reflex",
            Active = true,
            Automatic = true,
            ReflexCondition = new ReflexCondition(),
            ReflexTests = new List<ReflexTest>()
        };

        var reflexDefinitions = new List<ReflexDefinition> { reflexDefinition };
        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new SetActiveReflexDefinitionCommand.SetActiveReflexDefinitionCommand(1, false);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        reflexDefinition.Active.Should().BeFalse();
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_ReflexDefinitionNotFound()
    {
        // Arrange
        var reflexDefinitions = new List<ReflexDefinition>();
        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);

        var command = new SetActiveReflexDefinitionCommand.SetActiveReflexDefinitionCommand(999, true);

        // Act & Assert
        await Assert.ThrowsAsync<ObjectGoneException>(() => _sut.Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_Should_ToggleActiveState_When_CalledMultipleTimes()
    {
        // Arrange
        var reflexDefinition = new ReflexDefinition
        {
            Id = 1,
            Name = "Test Reflex",
            Active = false,
            Automatic = true,
            ReflexCondition = new ReflexCondition(),
            ReflexTests = new List<ReflexTest>()
        };

        var reflexDefinitions = new List<ReflexDefinition> { reflexDefinition };
        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act - Set to true
        await _sut.Handle(new SetActiveReflexDefinitionCommand.SetActiveReflexDefinitionCommand(1, true), TestContext.Current.CancellationToken);
        reflexDefinition.Active.Should().BeTrue();

        // Act - Set to false
        await _sut.Handle(new SetActiveReflexDefinitionCommand.SetActiveReflexDefinitionCommand(1, false), TestContext.Current.CancellationToken);

        // Assert
        reflexDefinition.Active.Should().BeFalse();
    }
}
