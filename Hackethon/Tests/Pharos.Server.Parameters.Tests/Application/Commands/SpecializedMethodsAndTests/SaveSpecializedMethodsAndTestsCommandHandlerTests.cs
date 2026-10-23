using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Queries.GetSpecializedMethodsAndTests;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;

namespace Pharos.Server.Parameters.Application.Commands.SpecializedMethodsAndTests;

public class SaveSpecializedMethodsAndTestsCommandHandlerTests
{
    private readonly List<Method> _methods = [];
    private readonly List<MethodTest> _methodTests = [];
    private readonly Mock<IParametersDbContext> _contextMock;
    private readonly SaveSpecializedMethodsAndTestsAvailabilityCommandHandler _handler;

    public SaveSpecializedMethodsAndTestsCommandHandlerTests()
    {
        _contextMock = new Mock<IParametersDbContext>();
        _contextMock.Setup(x => x.Methods).ReturnsDbSet(_methods);
        _contextMock.Setup(x => x.MethodTests).ReturnsDbSet(_methodTests);
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new SaveSpecializedMethodsAndTestsAvailabilityCommandHandler(_contextMock.Object);
    }

    [Fact]
    public async Task Handle_WhenRequestHasNoItems_ShouldNotSaveChanges()
    {
        // Act
        await _handler.Handle(new SaveSpecializedMethodsAndTestsCommand(), TestContext.Current.CancellationToken);

        // Assert
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [Trait("ProductRequirement", "PR_342")]
    [Trait("ProductRequirement", "PR_2765")]
    [InlineData(SpecializedMethodOrTestType.RUOMethod, false)]
    [InlineData(SpecializedMethodOrTestType.OptionalMethod, false)]
    [InlineData(SpecializedMethodOrTestType.RUOMethod, true)]
    [InlineData(SpecializedMethodOrTestType.OptionalMethod, true)]
    public async Task Handle_WhenMethodUpdateExists_ShouldUpdateMethodAndRelatedTests(
        SpecializedMethodOrTestType type,
        bool isAvailable)
    {
        // Arrange
        var method = CreateMethod(1);
        var firstTest = CreateMethodTest(10, method);
        var secondTest = CreateMethodTest(11, method);
        _methods.Add(method);
        _methodTests.AddRange([firstTest, secondTest]);

        // Act
        await _handler.Handle(CreateCommand(CreateRequest(method.Id, null, type, isAvailable)), TestContext.Current.CancellationToken);

        // Assert
        method.IsSpecializedMethodActivated.Should().Be(isAvailable);
        method.Enabled.Should().Be(isAvailable);
        _methodTests.Should().OnlyContain(x => x.Active == isAvailable);
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [Trait("ProductRequirement", "PR_342")]
    [Trait("ProductRequirement", "PR_2765")]
    [InlineData(SpecializedMethodOrTestType.RUOMethod)]
    [InlineData(SpecializedMethodOrTestType.OptionalMethod)]
    public async Task Handle_WhenMethodEnabledChanges_ShouldAddMethodEnabledEventAndMethodTestActivatedEvents(
        SpecializedMethodOrTestType type)
    {
        // Arrange
        var method = CreateMethod(1);
        var firstTest = CreateMethodTest(10, method);
        var secondTest = CreateMethodTest(11, method);
        _methods.Add(method);
        _methodTests.AddRange([firstTest, secondTest]);

        // Act
        await _handler.Handle(CreateCommand(CreateRequest(method.Id, null, type, false)), TestContext.Current.CancellationToken);

        // Assert
        method.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(new MethodEnabledEvent(method.MethodName, false));
        firstTest.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(new MethodTestActivatedEvent(method.MethodName, firstTest.TestName, false));
        secondTest.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(new MethodTestActivatedEvent(method.MethodName, secondTest.TestName, false));
    }

    [Theory]
    [Trait("ProductRequirement", "PR_342")]
    [Trait("ProductRequirement", "PR_2765")]
    [InlineData(SpecializedMethodOrTestType.RUOMethod)]
    [InlineData(SpecializedMethodOrTestType.OptionalMethod)]
    public async Task Handle_WhenMethodEnabledAndRelatedTestsDoNotChange_ShouldNotAddDomainEvents(
        SpecializedMethodOrTestType type)
    {
        // Arrange
        var method = CreateMethod(1);
        var methodTest = CreateMethodTest(10, method);
        _methods.Add(method);
        _methodTests.Add(methodTest);

        // Act
        await _handler.Handle(CreateCommand(CreateRequest(method.Id, null, type, true)), TestContext.Current.CancellationToken);

        // Assert
        method.DomainEvents.Should().BeEmpty();
        methodTest.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [Trait("ProductRequirement", "PR_342")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_WhenRuoTestUpdateExists_ShouldUpdateOnlyMatchingTest(bool isAvailable)
    {
        // Arrange
        var method = CreateMethod(1);
        var updatedTest = CreateMethodTest(10, method);
        var unchangedTest = CreateMethodTest(11, method);
        _methods.Add(method);
        _methodTests.AddRange([updatedTest, unchangedTest]);

        // Act
        await _handler.Handle(
            CreateCommand(CreateRequest(method.Id, updatedTest.Id, SpecializedMethodOrTestType.RUOTest, isAvailable)),
            TestContext.Current.CancellationToken);

        // Assert
        updatedTest.IsSpecializedTestActivated.Should().Be(isAvailable);
        updatedTest.Active.Should().Be(isAvailable);
        unchangedTest.IsSpecializedTestActivated.Should().BeTrue();
        unchangedTest.Active.Should().BeTrue();
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_342")]
    public async Task Handle_WhenRuoTestActiveChanges_ShouldAddMethodTestActivatedEvent()
    {
        // Arrange
        var method = CreateMethod(1);
        var updatedTest = CreateMethodTest(10, method);
        var unchangedTest = CreateMethodTest(11, method);
        _methods.Add(method);
        _methodTests.AddRange([updatedTest, unchangedTest]);

        // Act
        await _handler.Handle(
            CreateCommand(CreateRequest(method.Id, updatedTest.Id, SpecializedMethodOrTestType.RUOTest, false)),
            TestContext.Current.CancellationToken);

        // Assert
        updatedTest.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(new MethodTestActivatedEvent(method.MethodName, updatedTest.TestName, false));
        unchangedTest.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [Trait("ProductRequirement", "PR_2765")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_WhenOptionalTestUpdateExists_ShouldUpdateOnlySpecialTestsForMethod(bool isAvailable)
    {
        // Arrange
        var method = CreateMethod(1);
        var firstSpecialTest = CreateMethodTest(10, method, specialMethodName: "Special 1");
        var secondSpecialTest = CreateMethodTest(11, method, specialMethodName: "Special 2");
        var regularTest = CreateMethodTest(12, method);
        _methods.Add(method);
        _methodTests.AddRange([firstSpecialTest, secondSpecialTest, regularTest]);

        // Act
        await _handler.Handle(
            CreateCommand(CreateRequest(method.Id, null, SpecializedMethodOrTestType.OptionalTest, isAvailable)),
            TestContext.Current.CancellationToken);

        // Assert
        firstSpecialTest.IsSpecializedTestActivated.Should().Be(isAvailable);
        firstSpecialTest.Active.Should().Be(isAvailable);
        secondSpecialTest.IsSpecializedTestActivated.Should().Be(isAvailable);
        secondSpecialTest.Active.Should().Be(isAvailable);
        regularTest.IsSpecializedTestActivated.Should().BeTrue();
        regularTest.Active.Should().BeTrue();
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2765")]
    public async Task Handle_WhenOptionalTestActiveChanges_ShouldAddMethodTestActivatedEvents()
    {
        // Arrange
        var method = CreateMethod(1);
        var firstSpecialTest = CreateMethodTest(10, method, specialMethodName: "Special 1");
        var secondSpecialTest = CreateMethodTest(11, method, specialMethodName: "Special 2");
        var regularTest = CreateMethodTest(12, method);
        _methods.Add(method);
        _methodTests.AddRange([firstSpecialTest, secondSpecialTest, regularTest]);

        // Act
        await _handler.Handle(
            CreateCommand(CreateRequest(method.Id, null, SpecializedMethodOrTestType.OptionalTest, false)),
            TestContext.Current.CancellationToken);

        // Assert
        firstSpecialTest.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(new MethodTestActivatedEvent(method.MethodName, firstSpecialTest.TestName, false));
        secondSpecialTest.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(new MethodTestActivatedEvent(method.MethodName, secondSpecialTest.TestName, false));
        regularTest.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData(SpecializedMethodOrTestType.RUOMethod)]
    [InlineData(SpecializedMethodOrTestType.RUOTest)]
    [InlineData(SpecializedMethodOrTestType.OptionalTest)]
    public async Task Handle_WhenDuplicateUpdatesExist_ShouldUseLastUpdate(SpecializedMethodOrTestType type)
    {
        // Arrange
        var method = CreateMethod(1);
        var methodTest = CreateMethodTest(10, method, specialMethodName: "Special");
        _methods.Add(method);
        _methodTests.Add(methodTest);
        int? methodTestId = type == SpecializedMethodOrTestType.RUOTest ? methodTest.Id : null;

        // Act
        await _handler.Handle(
            CreateCommand(
                CreateRequest(method.Id, methodTestId, type, false),
                CreateRequest(method.Id, methodTestId, type, true)),
            TestContext.Current.CancellationToken);

        // Assert
        if (type is SpecializedMethodOrTestType.RUOMethod)
        {
            method.IsSpecializedMethodActivated.Should().BeTrue();
            method.Enabled.Should().BeTrue();
        }
        else
        {
            methodTest.IsSpecializedTestActivated.Should().BeTrue();
            methodTest.Active.Should().BeTrue();
        }
    }

    [Fact]
    [Trait("ProductRequirement", "PR_342")]
    [Trait("ProductRequirement", "PR_2765")]
    public async Task Handle_WhenMixedUpdatesExist_ShouldApplyAllUpdatesAndSaveOnce()
    {
        // Arrange
        var method = CreateMethod(1);
        var ruoTest = CreateMethodTest(10, method);
        var optionalTest = CreateMethodTest(11, method, specialMethodName: "Special");
        _methods.Add(method);
        _methodTests.AddRange([ruoTest, optionalTest]);

        // Act
        await _handler.Handle(
            CreateCommand(
                CreateRequest(method.Id, null, SpecializedMethodOrTestType.RUOMethod, false),
                CreateRequest(method.Id, ruoTest.Id, SpecializedMethodOrTestType.RUOTest, true),
                CreateRequest(method.Id, null, SpecializedMethodOrTestType.OptionalTest, false)),
            TestContext.Current.CancellationToken);

        // Assert
        method.IsSpecializedMethodActivated.Should().BeFalse();
        method.Enabled.Should().BeFalse();
        ruoTest.IsSpecializedTestActivated.Should().BeTrue();
        ruoTest.Active.Should().BeTrue();
        optionalTest.IsSpecializedTestActivated.Should().BeFalse();
        optionalTest.Active.Should().BeFalse();
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static SaveSpecializedMethodsAndTestsCommand CreateCommand(params SaveSpecializedRequestDto[] items)
    {
        return new SaveSpecializedMethodsAndTestsCommand
        {
            Items = items.ToList()
        };
    }

    private static SaveSpecializedRequestDto CreateRequest(
        int methodId,
        int? methodTestId,
        SpecializedMethodOrTestType type,
        bool isAvailable)
    {
        return new SaveSpecializedRequestDto
        {
            MethodId = methodId,
            MethodTestId = methodTestId,
            Type = type,
            IsSpecializedMethodOrTestActivated = isAvailable
        };
    }

    private static Method CreateMethod(int id)
    {
        return new Method
        {
            Id = id,
            MethodName = $"M{id}",
            FullMethodName = $"Method {id}",
            IsSpecializedMethodActivated = true,
            Enabled = true,
            Tests = []
        };
    }

    private static MethodTest CreateMethodTest(int id, Method method, string? specialMethodName = null)
    {
        var methodTest = new MethodTest
        {
            Id = id,
            MethodId = method.Id,
            Method = method,
            TestName = $"T{id}",
            FullTestName = $"Test {id}",
            IsSpecializedTestActivated = true,
            Active = true,
            SpecialMethodName = specialMethodName
        };

        method.Tests.Add(methodTest);
        return methodTest;
    }
}