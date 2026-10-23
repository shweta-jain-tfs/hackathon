using FluentAssertions;
using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Reflex.Application.Commands.DeleteReflexDefinitionCommand;
using Pharos.Server.Reflex.Domain;
using Pharos.Server.Reflex.Interfaces;
using Xunit;

namespace Pharos.Server.Reflex.Application.Commands;

public class DeleteReflexDefinitionCommandHandlerTests
{
    private readonly Mock<IReflexDbContext> _dbContextMock;
    private readonly DeleteReflexDefinitionCommandHandler _sut;

    public DeleteReflexDefinitionCommandHandlerTests()
    {
        _dbContextMock = new Mock<IReflexDbContext>();
        _sut = new DeleteReflexDefinitionCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_Should_DeleteSingleReflexDefinition_When_OneIdProvided()
    {
        // Arrange
        var reflexDefinition = CreateReflexDefinition(1, "Reflex 1");
        var reflexDefinitions = new List<ReflexDefinition> { reflexDefinition };

        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new DeleteReflexDefinitionCommand.DeleteReflexDefinitionCommand([1]);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _dbContextMock.Verify(x => x.ReflexDefinitions.Remove(It.IsAny<ReflexDefinition>()), Times.Once);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_DeleteMultipleReflexDefinitions_When_MultipleIdsProvided()
    {
        // Arrange
        var reflexDefinition1 = CreateReflexDefinition(1, "Reflex 1");
        var reflexDefinition2 = CreateReflexDefinition(2, "Reflex 2");
        var reflexDefinition3 = CreateReflexDefinition(3, "Reflex 3");

        var reflexDefinitions = new List<ReflexDefinition> { reflexDefinition1, reflexDefinition2, reflexDefinition3 };

        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new DeleteReflexDefinitionCommand.DeleteReflexDefinitionCommand([1, 2]);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _dbContextMock.Verify(x => x.ReflexDefinitions.Remove(It.IsAny<ReflexDefinition>()), Times.Exactly(2));
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_DeleteOnlyMatchingReflexDefinitions_When_SomeIdsDoNotExist()
    {
        // Arrange
        var reflexDefinition1 = CreateReflexDefinition(1, "Reflex 1");
        var reflexDefinition2 = CreateReflexDefinition(2, "Reflex 2");

        var reflexDefinitions = new List<ReflexDefinition> { reflexDefinition1, reflexDefinition2 };

        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new DeleteReflexDefinitionCommand.DeleteReflexDefinitionCommand([1, 999]); // 999 doesn't exist

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _dbContextMock.Verify(x => x.ReflexDefinitions.Remove(reflexDefinition1), Times.Once);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_DeleteReflexDefinitionWithTests_When_TestsExist()
    {
        // Arrange
        var reflexDefinition = new ReflexDefinition
        {
            Id = 1,
            Name = "Reflex with Tests",
            Automatic = true,
            Active = true,
            ReflexCondition = new ReflexCondition
            {
                TestName = "TestName",
                MethodName = "MethodName",
                ResultType = ResultType.Concentration,
                CompareType = CompareType.Above,
                ConcentrationCompareValue = 1.0
            },
            ReflexTests = new List<ReflexTest>
            {
                new()
                {
                    Id = 1,
                    TestName = "Test1",
                    MethodName = "Method1",
                },
                new()
                {
                    Id = 2,
                    TestName = "Test2",
                    MethodName = "Method2",
                }
            }
        };

        var reflexDefinitions = new List<ReflexDefinition> { reflexDefinition };

        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new DeleteReflexDefinitionCommand.DeleteReflexDefinitionCommand([1]);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _dbContextMock.Verify(x => x.ReflexDefinitions.Remove(reflexDefinition), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_RaiseDeletedEvent_When_ReflexDefinitionDeleted()
    {
        // Arrange
        var reflexDefinition = CreateReflexDefinition(1, "Reflex 1");
        var reflexDefinitions = new List<ReflexDefinition> { reflexDefinition };

        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new DeleteReflexDefinitionCommand.DeleteReflexDefinitionCommand([1]);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        reflexDefinition.DomainEvents.Should().HaveCount(1);
        reflexDefinition.DomainEvents.First().Should().BeOfType<Events.ReflexDefinitionDeletedEvent>();
    }

    private static ReflexDefinition CreateReflexDefinition(int id, string name)
    {
        return new ReflexDefinition
        {
            Id = id,
            Name = name,
            Automatic = true,
            Active = true,
            ReflexCondition = new ReflexCondition
            {
                TestName = "TestName",
                MethodName = "MethodName",
                ResultType = ResultType.Concentration,
                CompareType = CompareType.Above,
                ConcentrationCompareValue = 1.0
            },
            ReflexTests = new List<ReflexTest>()
        };
    }
}
