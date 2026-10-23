using Moq.EntityFrameworkCore;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;

namespace Pharos.Server.Parameters.Application.Queries.GetSpecializedMethodsAndTests;

public class GetSpecializedMethodsAndTestsQueryHandlerTests
{
    private readonly Mock<IParametersDbContext> _contextMock = new();
    private readonly GetSpecializedMethodsAndTestsQueryHandler _handler;

    public GetSpecializedMethodsAndTestsQueryHandlerTests()
    {
        _handler = new GetSpecializedMethodsAndTestsQueryHandler(_contextMock.Object);
    }

    [Theory]
    [Trait("ProductRequirement", "PR_340")]
    [InlineData(true, false, SpecializedMethodOrTestType.RUOMethod)]
    [InlineData(false, true, SpecializedMethodOrTestType.OptionalMethod)]
    public async Task Handle_WhenSpecializedMethodExists_ShouldReturnExpectedMethodType(
        bool isRuo,
        bool isSpecialMethod,
        SpecializedMethodOrTestType expectedType)
    {
        // Arrange
        var method = CreateMethod(1, "Method", isRuo: isRuo, isSpecialMethod: isSpecialMethod, isAvailable: false);
        SetupDb([method], []);

        // Act
        var result = await _handler.Handle(CreateQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new SpecializedMethodOrTestDto
        {
            Id = HashCode.Combine(method.Id, 0),
            MethodId = method.Id,
            MethodTestId = null,
            Name = method.FullMethodName,
            Type = expectedType,
            IsSpecializedMethodOrTestActivated = false
        });
    }

    [Theory]
    [Trait("ProductRequirement", "PR_340")]
    [InlineData(true, null, SpecializedMethodOrTestType.RUOTest, "Test")]
    [InlineData(false, "Special", SpecializedMethodOrTestType.OptionalTest, "Method (Special)")]
    public async Task Handle_WhenSpecializedTestExists_ShouldReturnExpectedTestType(
        bool isRuo,
        string? specialMethodName,
        SpecializedMethodOrTestType expectedType,
        string expectedName)
    {
        // Arrange
        var method = CreateMethod(1, "Method");
        var test = CreateMethodTest(10, method, "Test", isRuo: isRuo, specialMethodName: specialMethodName, isAvailable: false);
        SetupDb([method], [test]);

        // Act
        var result = await _handler.Handle(CreateQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new SpecializedMethodOrTestDto
        {
            Id = HashCode.Combine(method.Id, test.Id),
            MethodId = method.Id,
            MethodTestId = expectedType == SpecializedMethodOrTestType.RUOTest ? test.Id : null,
            Name = expectedName,
            Type = expectedType,
            IsSpecializedMethodOrTestActivated = false
        });
    }

    [Fact]
    [Trait("ProductRequirement", "PR_340")]
    public async Task Handle_WhenRuoTestBelongsToRuoMethod_ShouldNotReturnTest()
    {
        // Arrange
        var ruoMethod = CreateMethod(1, "RUO Method", isRuo: true);
        var ruoTest = CreateMethodTest(10, ruoMethod, "RUO Test", isRuo: true);
        SetupDb([ruoMethod], [ruoTest]);

        // Act
        var result = await _handler.Handle(CreateQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(1); // Only the RUO method should be returned
        result.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new SpecializedMethodOrTestDto
        {
            Id = HashCode.Combine(ruoMethod.Id, 0),
            MethodId = ruoMethod.Id,
            MethodTestId = null,
            Name = ruoMethod.FullMethodName,
            Type = SpecializedMethodOrTestType.RUOMethod,
            IsSpecializedMethodOrTestActivated = ruoMethod.IsSpecializedMethodActivated
        });
    }

    [Fact]
    public async Task Handle_WhenMultipleOptionalTestsExistForSameMethod_ShouldReturnOneOptionalTestForMethod()
    {
        // Arrange
        var method = CreateMethod(1, "Method");
        SetupDb(
            [method],
            [
                CreateMethodTest(10, method, "First", specialMethodName: "Beta"),
                CreateMethodTest(11, method, "Second", specialMethodName: "Alpha")
            ]);

        // Act
        var result = await _handler.Handle(CreateQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(x =>
            x.MethodId == method.Id &&
            x.MethodTestId == null &&
            x.Type == SpecializedMethodOrTestType.OptionalTest &&
            x.Name == "Method (Alpha)");
    }

    [Theory]
    [Trait("ProductRequirement", "PR_340")]
    [InlineData(null)]
    [InlineData("")]
    public async Task Handle_WhenSpecialMethodNameIsMissing_ShouldExcludeOptionalTest(string? specialMethodName)
    {
        // Arrange
        var method = CreateMethod(1, "Method");
        var test = CreateMethodTest(10, method, "Test", specialMethodName: specialMethodName);
        SetupDb([method], [test]);

        // Act
        var result = await _handler.Handle(CreateQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Theory]
    [Trait("ProductRequirement", "PR_340")]
    [InlineData("method", null, "RUO Method", "Optional Method", "Test Method (Special)")]
    [InlineData("METHOD", null, "RUO Method", "Optional Method", "Test Method (Special)")]
    [InlineData(null, "RUOMethod", "RUO Method")]
    [InlineData(null, "OptionalMethod", "Optional Method")]
    [InlineData(null, "RUOTest", "RUO Test")]
    [InlineData(null, "OptionalTest", "Test Method (Special)")]
    [InlineData("ruo", "RUOTest", "RUO Test")]
    [InlineData("optional", "RUOTest")]
    [InlineData(null, "Unknown")]
    public async Task Handle_WhenFiltersAreProvided_ShouldReturnMatchingItems(
        string? nameFilter,
        string? typeFilter,
        params string[] expectedNames)
    {
        // Arrange
        var ruoMethod = CreateMethod(1, "RUO Method", isRuo: true);
        var optionalMethod = CreateMethod(2, "Optional Method", isSpecialMethod: true);
        var testMethod = CreateMethod(3, "Test Method");
        SetupDb(
            [ruoMethod, optionalMethod, testMethod],
            [
                CreateMethodTest(10, testMethod, "RUO Test", isRuo: true),
                CreateMethodTest(11, testMethod, "Optional Test", specialMethodName: "Special")
            ]);

        // Act
        var result = await _handler.Handle(CreateQuery(nameFilter, typeFilter), TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(expectedNames.Length);
        result.Items.Select(x => x.Name).Should().BeEquivalentTo(expectedNames);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_340")]
    public async Task Handle_ShouldSortByTypeThenNameAndApplyPaging()
    {
        // Arrange
        var firstMethod = CreateMethod(1, "Zulu Optional", isSpecialMethod: true);
        var secondMethod = CreateMethod(2, "Alpha Optional", isSpecialMethod: true);
        var thirdMethod = CreateMethod(3, "Alpha RUO", isRuo: true);
        var testMethod = CreateMethod(4, "Test Method");
        SetupDb(
            [firstMethod, secondMethod, thirdMethod, testMethod],
            [
                CreateMethodTest(10, testMethod, "Beta RUO Test", isRuo: true),
                CreateMethodTest(11, testMethod, "Alpha Optional Test", specialMethodName: "Special")
            ]);

        // Act
        var result = await _handler.Handle(new GetSpecializedMethodsAndTestsQuery
        {
            StartIndex = 1,
            Count = 3
        }, TestContext.Current.CancellationToken);

        // Assert
        result.TotalCount.Should().Be(5);
        result.Items.Select(x => x.Name).Should().Equal(
            "Zulu Optional",
            "Test Method (Special)",
            "Alpha RUO");
    }

    private void SetupDb(List<Method> methods, List<MethodTest> methodTests)
    {
        _contextMock.Setup(x => x.Methods).ReturnsDbSet(methods);
        _contextMock.Setup(x => x.MethodTests).ReturnsDbSet(methodTests);
    }

    private static GetSpecializedMethodsAndTestsQuery CreateQuery(string? name = null, string? type = null)
    {
        return new GetSpecializedMethodsAndTestsQuery
        {
            Name = name,
            Type = type is null ? [] : [type],
            StartIndex = 0,
            Count = 100
        };
    }

    private static Method CreateMethod(
        int id,
        string fullMethodName,
        bool isRuo = false,
        bool isSpecialMethod = false,
        bool isAvailable = true)
    {
        return new Method
        {
            Id = id,
            MethodName = $"M{id}",
            FullMethodName = fullMethodName,
            IsRuo = isRuo,
            IsSpecialMethod = isSpecialMethod,
            IsSpecializedMethodActivated = isAvailable,
            Enabled = true,
            Tests = []
        };
    }

    private static MethodTest CreateMethodTest(
        int id,
        Method method,
        string fullTestName,
        bool isRuo = false,
        string? specialMethodName = null,
        bool isAvailable = true)
    {
        var methodTest = new MethodTest
        {
            Id = id,
            MethodId = method.Id,
            Method = method,
            TestName = $"T{id}",
            FullTestName = fullTestName,
            IsRuo = isRuo,
            SpecialMethodName = specialMethodName,
            IsSpecializedTestActivated = isAvailable,
            Active = true
        };

        method.Tests.Add(methodTest);
        return methodTest;
    }
}