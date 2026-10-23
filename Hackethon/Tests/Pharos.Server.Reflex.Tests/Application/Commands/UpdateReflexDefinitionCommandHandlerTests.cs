using System.Linq.Expressions;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Options;
using Moq;
using Moq.EntityFrameworkCore;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Reflex.Application.Commands.UpdateReflexDefinitionCommand;
using Pharos.Server.Reflex.Domain;
using Pharos.Server.Reflex.Interfaces;
using Xunit;

namespace Pharos.Server.Reflex.Application.Commands;

public class UpdateReflexDefinitionCommandHandlerTests
{
    private readonly Mock<IReflexDbContext> _dbContextMock;
    private readonly UpdateReflexDefinitionCommandHandler _sut;

    public UpdateReflexDefinitionCommandHandlerTests()
    {
        _dbContextMock = new Mock<IReflexDbContext>();
        _dbContextMock.Setup(x => x.ReflexTests).ReturnsDbSet(new List<ReflexTest>());
        var methodsMock = new Mock<IMethodsService>();
        methodsMock
            .Setup(x => x.GetMethodTest(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Expression<Func<MethodTest, UpdateReflexDefinitionCommandHandler.ReflexMethodTestValidationInfo>>>()
            ))
            .ReturnsAsync(new UpdateReflexDefinitionCommandHandler.ReflexMethodTestValidationInfo
            {
                Technology = AssayTechnology.ImmunoCap,
                InstrumentDilutionFactor = 1
            });

        var settingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();
        settingsMock.Setup(x => x.Value).Returns(new RequestSettings { IsRequestorInformationEnabled = true });

        var authServiceMock = new Mock<IAuthorizationService>();
        authServiceMock.Setup(x => x.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData)).ReturnsAsync(true);

        _sut = new UpdateReflexDefinitionCommandHandler(_dbContextMock.Object, methodsMock.Object, settingsMock.Object, authServiceMock.Object);
    }

    [Fact]
    public async Task Handle_Should_UpdateReflexDefinition_When_ValidCommand()
    {
        // Arrange
        var reflexDefinition = CreateReflexDefinition(1, "Old Name");
        var reflexDefinitions = new List<ReflexDefinition> { reflexDefinition };

        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new UpdateReflexDefinitionCommand.UpdateReflexDefinitionCommand
        {
            Id = 1,
            Name = "Updated Name",
            Automatic = false,
            Active = false,
            HasRequestorEnabled = true,
            RequestorId = "REQ001",
            ReflexCondition = new UpdatedReflexCondition
            {
                TestName = "UpdatedTest",
                MethodName = "UpdatedMethod",
                ResultType = ResultType.Class,
                CompareType = CompareType.EqualTo,
                ClassCompareValue = "3"
            },
            ReflexTests = new List<UpdatedReflexTest>()
        };

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        reflexDefinition.Name.Should().Be("Updated Name");
        reflexDefinition.Automatic.Should().BeFalse();
        reflexDefinition.Active.Should().BeFalse();
        reflexDefinition.RequestorId.Should().Be("REQ001");
        reflexDefinition.ReflexCondition.TestName.Should().Be("UpdatedTest");
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_ReflexDefinitionNotFound()
    {
        // Arrange
        var reflexDefinitions = new List<ReflexDefinition>();
        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);

        var command = new UpdateReflexDefinitionCommand.UpdateReflexDefinitionCommand
        {
            Id = 999,
            Name = "Test",
            ReflexCondition = new UpdatedReflexCondition { TestName = "Test", MethodName = "Method" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ObjectGoneException>(() => _sut.Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_Should_ThrowValidationException_When_NameAlreadyExists()
    {
        // Arrange
        var reflexDefinition1 = CreateReflexDefinition(1, "Reflex 1");
        var reflexDefinition2 = CreateReflexDefinition(2, "Reflex 2");
        var reflexDefinitions = new List<ReflexDefinition> { reflexDefinition1, reflexDefinition2 };

        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);

        var command = new UpdateReflexDefinitionCommand.UpdateReflexDefinitionCommand
        {
            Id = 1,
            Name = "Reflex 2", // Name already used by reflexDefinition2
            ReflexCondition = new UpdatedReflexCondition { TestName = "Test", MethodName = "Method" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _sut.Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_Should_UpdateExistingReflexTest_When_TestIdProvided()
    {
        // Arrange
        var existingTest = new ReflexTest
        {
            Id = 10,
            TestName = "OldTest",
            MethodName = "OldMethod",
            NumberOfReplicates = 1,
            InstrumentDilutionFactor = 1,
            TestSortOrder = 1
        };

        var reflexDefinition = CreateReflexDefinition(1, "Test Reflex");
        reflexDefinition.ReflexTests.Add(existingTest);

        var reflexDefinitions = new List<ReflexDefinition> { reflexDefinition };
        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new UpdateReflexDefinitionCommand.UpdateReflexDefinitionCommand
        {
            Id = 1,
            Name = "Test Reflex",
            ReflexCondition = new UpdatedReflexCondition { TestName = "Test", MethodName = "Method" },
            ReflexTests = new List<UpdatedReflexTest>
            {
                new()
                {
                    Id = 10,
                    TestName = "UpdatedTest",
                    MethodName = "UpdatedMethod",
                    NumberOfReplicates = 3,
                    InstrumentDilutionFactor = 2,
                    TestSortOrder = 1
                }
            }
        };

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        existingTest.TestName.Should().Be("UpdatedTest");
        existingTest.MethodName.Should().Be("UpdatedMethod");
        existingTest.NumberOfReplicates.Should().Be(3);
        existingTest.InstrumentDilutionFactor.Should().Be(2);
    }

    [Fact]
    public async Task Handle_Should_AddNewReflexTest_When_NoTestIdProvided()
    {
        // Arrange
        var reflexDefinition = CreateReflexDefinition(1, "Test Reflex");
        reflexDefinition.ReflexTests.Clear();

        var reflexDefinitions = new List<ReflexDefinition> { reflexDefinition };
        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new UpdateReflexDefinitionCommand.UpdateReflexDefinitionCommand
        {
            Id = 1,
            Name = "Test Reflex",
            ReflexCondition = new UpdatedReflexCondition { TestName = "Test", MethodName = "Method" },
            ReflexTests = new List<UpdatedReflexTest>
            {
                new()
                {
                    Id = null, // No ID means new test
                    TestName = "NewTest",
                    MethodName = "NewMethod",
                    NumberOfReplicates = 2,
                    InstrumentDilutionFactor = 1,
                    TestSortOrder = 1
                }
            }
        };

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        reflexDefinition.ReflexTests.Should().HaveCount(1);
        reflexDefinition.ReflexTests[0].TestName.Should().Be("NewTest");
        reflexDefinition.ReflexTests[0].MethodName.Should().Be("NewMethod");
    }

    [Fact]
    public async Task Handle_Should_DeleteRemovedReflexTests_When_TestNotInUpdateList()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var test1 = new ReflexTest { Id = 10, TestName = "Test1", MethodName = "Method1", Created = now };
        var test2 = new ReflexTest { Id = 20, TestName = "Test2", MethodName = "Method2", Created = now };
        var test3 = new ReflexTest { Id = 30, TestName = "Test3", MethodName = "Method3", Created = now + TimeSpan.FromMinutes(10) };

        var reflexDefinition = CreateReflexDefinition(1, "Test Reflex");
        reflexDefinition.ReflexTests.Add(test1);
        reflexDefinition.ReflexTests.Add(test2);
        reflexDefinition.ReflexTests.Add(test3);

        var reflexDefinitions = new List<ReflexDefinition> { reflexDefinition };
        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new UpdateReflexDefinitionCommand.UpdateReflexDefinitionCommand
        {
            Id = 1,
            Name = "Test Reflex",
            ReflexCondition = new UpdatedReflexCondition { TestName = "Test", MethodName = "Method" },
            ReflexDefinitionQueriedAt = now.AddMinutes(1), // delete one test created before this time, keep one created after
            ReflexTests =
            [
                new()
                {
                    Id = 10,
                    TestName = "Test1",
                    MethodName = "Method1",
                    TestSortOrder = 1
                }
            ]
        };

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        reflexDefinition.ReflexTests.Should().HaveCount(2);
        _dbContextMock.Verify(x => x.ReflexTests.Remove(test2), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowValidationException_When_DuplicateNewTestsAdded()
    {
        // Arrange
        var reflexDefinition = CreateReflexDefinition(1, "Test Reflex");
        var reflexDefinitions = new List<ReflexDefinition> { reflexDefinition };

        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);

        var command = new UpdateReflexDefinitionCommand.UpdateReflexDefinitionCommand
        {
            Id = 1,
            Name = "Test Reflex",
            ReflexCondition = new UpdatedReflexCondition { TestName = "Test", MethodName = "Method" },
            ReflexTests = new List<UpdatedReflexTest>
            {
                new() { Id = null, TestName = "Test1", MethodName = "Method1", TestSortOrder = 1 },
                new() { Id = null, TestName = "Test1", MethodName = "Method1", TestSortOrder = 2 }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _sut.Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_Should_SetRequestorIdToNull_When_HasRequestorEnabledIsFalse()
    {
        // Arrange
        var reflexDefinition = CreateReflexDefinition(1, "Test Reflex");
        reflexDefinition.RequestorId = "REQ001";

        var reflexDefinitions = new List<ReflexDefinition> { reflexDefinition };
        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new UpdateReflexDefinitionCommand.UpdateReflexDefinitionCommand
        {
            Id = 1,
            Name = "Test Reflex",
            HasRequestorEnabled = false,
            RequestorId = "REQ001",
            ReflexCondition = new UpdatedReflexCondition { TestName = "Test", MethodName = "Method" },
            ReflexTests = new List<UpdatedReflexTest>()
        };

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        reflexDefinition.RequestorId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_UpdateTestSortOrder_When_MultipleTestsProvided()
    {
        // Arrange
        var test1 = new ReflexTest { Id = 10, TestName = "Test1", MethodName = "Method1", TestSortOrder = 1 };
        var test2 = new ReflexTest { Id = 20, TestName = "Test2", MethodName = "Method2", TestSortOrder = 2 };

        var reflexDefinition = CreateReflexDefinition(1, "Test Reflex");
        reflexDefinition.ReflexTests.Add(test1);
        reflexDefinition.ReflexTests.Add(test2);

        var reflexDefinitions = new List<ReflexDefinition> { reflexDefinition };
        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new UpdateReflexDefinitionCommand.UpdateReflexDefinitionCommand
        {
            Id = 1,
            Name = "Test Reflex",
            ReflexCondition = new UpdatedReflexCondition { TestName = "Test", MethodName = "Method" },
            ReflexTests = new List<UpdatedReflexTest>
            {
                new() { Id = 20, TestName = "Test2", MethodName = "Method2", TestSortOrder = 3 }, // Changed order
                new() { Id = 10, TestName = "Test1", MethodName = "Method1", TestSortOrder = 5 }
            }
        };

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        test2.TestSortOrder.Should().Be(1); // First in list gets order 1
        test1.TestSortOrder.Should().Be(2); // Second in list gets order 2
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
