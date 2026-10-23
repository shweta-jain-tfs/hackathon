using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Security;
using Pharos.Server.Parameters.Application.Interfaces;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;

namespace Pharos.Server.Parameters.Application.Queries.GetMethods;

public class GetMethodsQueryHandlerTests
{
    private readonly Mock<IParametersDbContext> _dbMock = new();
    private readonly Mock<IAuthorizationService> _authorizationServiceMock = new();
    private readonly GetMethodsQueryHandler _sut;

    public GetMethodsQueryHandlerTests()
    {
        _sut = new GetMethodsQueryHandler(_dbMock.Object, _authorizationServiceMock.Object);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2764")]
    public async Task Handle_ShouldReturnRegularMethods_WhenSpecializedActivationDefaultsToFalse()
    {
        var regularMethod = CreateMethod(1, "Regular");
        var inactiveSpecializedMethod = CreateMethod(2, "Inactive Specialized", isSpecialMethod: true);
        var activeSpecializedMethod = CreateMethod(3, "Active Specialized", isSpecialMethod: true, isSpecializedMethodActivated: true);
        SetupDb([regularMethod, inactiveSpecializedMethod, activeSpecializedMethod]);

        var result = await _sut.Handle(new GetMethodsQuery(), TestContext.Current.CancellationToken);

        result.Select(x => x.FullMethodName).Should().BeEquivalentTo("Regular", "Active Specialized");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_341")]
    public async Task Handle_ShouldApplyRuoModeAfterSpecializedActivationFilter()
    {
        _authorizationServiceMock.Setup(x => x.RuoMode).Returns(true);
        var inactiveRuoMethod = CreateMethod(1, "Inactive RUO", isRuo: true);
        var activeRuoMethod = CreateMethod(2, "Active RUO", isRuo: true, isSpecializedMethodActivated: true);
        SetupDb([inactiveRuoMethod, activeRuoMethod]);

        var result = await _sut.Handle(new GetMethodsQuery(), TestContext.Current.CancellationToken);

        result.Should().ContainSingle(x => x.FullMethodName == "Active RUO");
    }

    [Theory]
    [InlineData(null, "M1")]
    [InlineData("Custom LIS ID", "Custom LIS ID")]
    public async Task Handle_ShouldReturnEffectiveLisMethodId(string? methodNameLis, string expectedMethodNameLis)
    {
        // Arrange
        var method = CreateMethod(1, "Method");
        method.MethodNameLis = methodNameLis;
        SetupDb([method]);

        // Act
        var result = await _sut.Handle(new GetMethodsQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().ContainSingle();
        result[0].MethodNameLis.Should().Be(expectedMethodNameLis);
    }

    private void SetupDb(List<Method> methods)
    {
        _dbMock.Setup(x => x.Methods).ReturnsDbSet(methods);
    }

    private static Method CreateMethod(
        int id,
        string fullMethodName,
        bool isRuo = false,
        bool isSpecialMethod = false,
        bool isSpecializedMethodActivated = false) =>
        new()
        {
            Id = id,
            MethodName = $"M{id}",
            FullMethodName = fullMethodName,
            IsRuo = isRuo,
            IsSpecialMethod = isSpecialMethod,
            IsSpecializedMethodActivated = isSpecializedMethodActivated,
            Tests = []
        };
}
