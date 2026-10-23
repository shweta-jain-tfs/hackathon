using Moq.EntityFrameworkCore;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;

namespace Pharos.Server.Parameters.Application.Queries.GetMethods;

public class GetMethodDetailsQueryHandlerTests
{
    private readonly Mock<IParametersDbContext> _dbMock = new();
    private readonly GetMethodDetailsQueryHandler _sut;

    public GetMethodDetailsQueryHandlerTests()
    {
        _sut = new GetMethodDetailsQueryHandler(_dbMock.Object);
    }

    [Theory]
    [InlineData(null, "M1")]
    [InlineData("Custom LIS ID", "Custom LIS ID")]
    public async Task Handle_ShouldReturnEffectiveLisMethodId(string? methodNameLis, string expectedMethodNameLis)
    {
        // Arrange
        var method = CreateMethod();
        method.MethodNameLis = methodNameLis;
        _dbMock.Setup(x => x.Methods).ReturnsDbSet([method]);

        // Act
        var result = await _sut.Handle(new GetMethodDetailsQuery(method.Id), TestContext.Current.CancellationToken);

        // Assert
        result.MethodNameLis.Should().Be(expectedMethodNameLis);
    }

    private static Method CreateMethod()
    {
        var methodTest = new MethodTest { TestName = "T1" };

        return new Method
        {
            Id = 1,
            MethodName = "M1",
            FullMethodName = "Method 1",
            AnalyticalRun = new MethodAnalyticalRun(),
            Reporting = new MethodReporting(),
            CalibrationCurve = new MethodCalibrationCurve
            {
                MethodTest = methodTest,
                CalibrationPoints = []
            },
            ClassTables = [],
            ClassTableBelows = [],
            CurveControls = []
        };
    }
}
