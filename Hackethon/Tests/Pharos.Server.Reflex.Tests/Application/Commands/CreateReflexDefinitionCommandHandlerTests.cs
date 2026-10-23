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
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Reflex.Application.Commands.CreateReflexDefinitionCommand;
using Pharos.Server.Reflex.Application.Queries;
using Pharos.Server.Reflex.Domain;
using Pharos.Server.Reflex.Interfaces;
using Xunit;

namespace Pharos.Server.Reflex.Application.Commands;

public class CreateReflexDefinitionCommandHandlerTests
{
    private readonly Mock<IReflexDbContext> _dbContextMock;
    private readonly CreateReflexDefinitionCommandHandler _sut;

    public CreateReflexDefinitionCommandHandlerTests()
    {
        _dbContextMock = new Mock<IReflexDbContext>();
        var methodsMock = new Mock<IMethodsService>();
        methodsMock
            .Setup(x => x.GetMethodTest(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Expression<Func<MethodTest, CreateReflexDefinitionCommandHandler.ReflexMethodTestValidationInfo>>>()
            ))
            .ReturnsAsync(new CreateReflexDefinitionCommandHandler.ReflexMethodTestValidationInfo
            {
                Technology = AssayTechnology.ImmunoCap,
                InstrumentDilutionFactor = 1
            });

        var settingsMock = new Mock<IOptionsSnapshot<RequestSettings>>();
        settingsMock.Setup(x => x.Value).Returns(new RequestSettings{IsRequestorInformationEnabled = true});

        var authServiceMock = new Mock<IAuthorizationService>();
        authServiceMock.Setup(x => x.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData)).ReturnsAsync(true);

        _sut = new CreateReflexDefinitionCommandHandler(_dbContextMock.Object, methodsMock.Object, settingsMock.Object, authServiceMock.Object);
    }

    [Fact]
    public async Task Handle_Should_CreateReflexDefinition_When_ValidCommand()
    {
        // Arrange
        var reflexDefinitions = new List<ReflexDefinition>();
        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new CreateReflexDefinitionCommand.CreateReflexDefinitionCommand
        {
            Name = "New Reflex",
            Automatic = true,
            Active = true,
            HasRequestorEnabled = false,
            RequestorId = null,
            ReflexCondition = new ReflexConditionDto
            {
                TestName = "TestName",
                MethodName = "sIgE",
                ResultType = ResultType.Concentration,
                CompareType = CompareType.Above,
                ConcentrationCompareValue = 3.5
            },
            ReflexTests = new List<ReflexTestDto>
            {
                new()
                {
                    TestName = "FollowUpTest",
                    MethodName = "sIgE",
                    NumberOfReplicates = 2,
                    InstrumentDilutionFactor = 1,
                    TestSortOrder = 1
                }
            }
        };

        // Act
        var result = await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowValidationException_When_NameAlreadyExists()
    {
        // Arrange
        var existingReflexDefinition = new ReflexDefinition
        {
            Id = 1,
            Name = "Existing Reflex",
            Automatic = true,
            Active = true,
            ReflexCondition = new ReflexCondition(),
            ReflexTests = new List<ReflexTest>()
        };

        var reflexDefinitions = new List<ReflexDefinition> { existingReflexDefinition };
        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);

        var command = new CreateReflexDefinitionCommand.CreateReflexDefinitionCommand
        {
            Name = "Existing Reflex",
            Automatic = true,
            Active = true,
            HasRequestorEnabled = false
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _sut.Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_Should_ThrowValidationException_When_DuplicateReflexTests()
    {
        // Arrange
        var reflexDefinitions = new List<ReflexDefinition>();
        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);

        var command = new CreateReflexDefinitionCommand.CreateReflexDefinitionCommand
        {
            Name = "New Reflex",
            Automatic = true,
            Active = true,
            ReflexTests = new List<ReflexTestDto>
            {
                new()
                {
                    TestName = "Test1",
                    MethodName = "sIgE"
                },
                new()
                {
                    TestName = "Test1",
                    MethodName = "sIgE"
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _sut.Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_Should_SetRequestorIdToNull_When_HasRequestorEnabledIsFalse()
    {
        // Arrange
        var reflexDefinitions = new List<ReflexDefinition>();
        var capturedReflexDefinition = (ReflexDefinition?)null;

        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);
        _dbContextMock.Setup(x => x.ReflexDefinitions.Add(It.IsAny<ReflexDefinition>()))
            .Callback<ReflexDefinition>(r => capturedReflexDefinition = r);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new CreateReflexDefinitionCommand.CreateReflexDefinitionCommand
        {
            Name = "New Reflex",
            Automatic = true,
            Active = true,
            HasRequestorEnabled = false,
            RequestorId = "REQ001",
            ReflexCondition = new ReflexConditionDto
            {
                TestName = "TestName",
                MethodName = "MethodName",
                ResultType = ResultType.Concentration,
                CompareType = CompareType.Above,
                ConcentrationCompareValue = 1.0
            }
        };

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        capturedReflexDefinition.Should().NotBeNull();
        capturedReflexDefinition!.RequestorId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_SetRequestorId_When_HasRequestorEnabledIsTrue()
    {
        // Arrange
        var reflexDefinitions = new List<ReflexDefinition>();
        var capturedReflexDefinition = (ReflexDefinition?)null;

        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);
        _dbContextMock.Setup(x => x.ReflexDefinitions.Add(It.IsAny<ReflexDefinition>()))
            .Callback<ReflexDefinition>(r => capturedReflexDefinition = r);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new CreateReflexDefinitionCommand.CreateReflexDefinitionCommand
        {
            Name = "New Reflex",
            Automatic = true,
            Active = true,
            HasRequestorEnabled = true,
            RequestorId = "REQ001",
            ReflexCondition = new ReflexConditionDto
            {
                TestName = "TestName",
                MethodName = "MethodName",
                ResultType = ResultType.Concentration,
                CompareType = CompareType.Above,
                ConcentrationCompareValue = 1.0
            }
        };

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        capturedReflexDefinition.Should().NotBeNull();
        capturedReflexDefinition!.RequestorId.Should().Be("REQ001");
    }

    [Fact]
    public async Task Handle_Should_AddReflexTests_When_ProvidedInCommand()
    {
        // Arrange
        var reflexDefinitions = new List<ReflexDefinition>();
        var capturedReflexDefinition = (ReflexDefinition?)null;

        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);
        _dbContextMock.Setup(x => x.ReflexDefinitions.Add(It.IsAny<ReflexDefinition>()))
            .Callback<ReflexDefinition>(r => capturedReflexDefinition = r);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new CreateReflexDefinitionCommand.CreateReflexDefinitionCommand
        {
            Name = "New Reflex",
            Automatic = true,
            Active = true,
            ReflexCondition = new ReflexConditionDto
            {
                TestName = "TestName",
                MethodName = "MethodName",
                ResultType = ResultType.Concentration,
                CompareType = CompareType.Above,
                ConcentrationCompareValue = 1.0
            },
            ReflexTests = new List<ReflexTestDto>
            {
                new()
                {
                    TestName = "Test1",
                    MethodName = "Method1",
                    NumberOfReplicates = 2,
                    InstrumentDilutionFactor = 1,
                    TestSortOrder = 1
                },
                new()
                {
                    TestName = "Test2",
                    MethodName = "Method2",
                    NumberOfReplicates = 3,
                    InstrumentDilutionFactor = 2,
                    TestSortOrder = 2
                }
            }
        };

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        capturedReflexDefinition.Should().NotBeNull();
        capturedReflexDefinition!.ReflexTests.Should().HaveCount(2);
        capturedReflexDefinition.ReflexTests[0].TestName.Should().Be("Test1");
        capturedReflexDefinition.ReflexTests[1].TestName.Should().Be("Test2");
    }
}
