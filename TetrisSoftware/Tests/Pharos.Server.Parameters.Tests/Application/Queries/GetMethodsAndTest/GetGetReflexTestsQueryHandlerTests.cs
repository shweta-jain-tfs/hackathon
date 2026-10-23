using Moq.EntityFrameworkCore;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;
using MethodClassTable = Pharos.Server.Domain.Parameters.Methods.MethodClassTable;
using MethodReporting = Pharos.Server.Domain.Parameters.Methods.MethodReporting;
using MethodTest = Pharos.Server.Domain.Parameters.Methods.MethodTest;

namespace Pharos.Server.Parameters.Application.Queries.GetMethodsAndTest;

public class GetGetReflexTestsQueryHandlerTests
{
    private readonly Mock<IParametersDbContext> _dbMock;
    private readonly GetGetReflexTestsQueryHandler _sut;

    public GetGetReflexTestsQueryHandlerTests()
    {
        _dbMock = new Mock<IParametersDbContext>();

        _sut = new GetGetReflexTestsQueryHandler(_dbMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNoMethodsAndTestsExists_Should_ReturnEmptyList()
    {
        _dbMock.Setup(x => x.Methods).ReturnsDbSet(new List<Method>());
        _dbMock.Setup(x => x.MethodTests).ReturnsDbSet(new List<MethodTest>());

        var result = await _sut.Handle(new GetReflexTestsQuery(), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenNoMethodsExists_Should_ReturnEmptyList()
    {
        _dbMock.Setup(x => x.Methods).ReturnsDbSet(new List<Method>());
        _dbMock.Setup(x => x.MethodTests).ReturnsDbSet(new List<MethodTest>
        {
            new()
            {
                Active = true
            }
        });

        var result = await _sut.Handle(new GetReflexTestsQuery(), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenMethodsExistsButNoTests_Should_ReturnEmptyList()
    {
        _dbMock.Setup(x => x.Methods).ReturnsDbSet(new List<Method>
        {
            CreateMethod()
        });
        _dbMock.Setup(x => x.MethodTests).ReturnsDbSet(new List<MethodTest>());

        var result = await _sut.Handle(new GetReflexTestsQuery(), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenMethodHasOneTest_ShouldReturnOneItem()
    {
        var method = CreateMethod();
        var methodTest = CreateTest(method);

        _dbMock.Setup(x => x.Methods).ReturnsDbSet(new List<Method>
        {
            method
        });
        _dbMock.Setup(x => x.MethodTests).ReturnsDbSet(new List<MethodTest>
        {
            methodTest
        });

        var result = await _sut.Handle(new GetReflexTestsQuery(), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WhenMethodHasNoActiveTests_ShouldReturnEmptyList()
    {
        var method = CreateMethod();
        var methodTest = CreateTest(method);
        methodTest.Active = false;

        _dbMock.Setup(x => x.Methods).ReturnsDbSet(new List<Method>
        {
            method
        });
        _dbMock.Setup(x => x.MethodTests).ReturnsDbSet(new List<MethodTest>
        {
            methodTest
        });

        var result = await _sut.Handle(new GetReflexTestsQuery(), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(0);
        result.Items.Should().HaveCount(0);
    }

    [Fact]
    public async Task Handle_WhenMethodIsNotActive_ShouldReturnEmptyList()
    {
        var method = CreateMethod();
        method.Enabled = false;

        var methodTest = CreateTest(method);

        _dbMock.Setup(x => x.Methods).ReturnsDbSet(new List<Method>
        {
            method
        });
        _dbMock.Setup(x => x.MethodTests).ReturnsDbSet(new List<MethodTest>
        {
            methodTest
        });

        var result = await _sut.Handle(new GetReflexTestsQuery(), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(0);
        result.Items.Should().HaveCount(0);
    }

    private static Method CreateMethod()
    {
        return new Method
        {
            Enabled = true,
            ClassTableBelows = new List<MethodClassTableBelow>(),
            ClassTables = new List<MethodClassTable>(),
            Reporting = new MethodReporting()
        };
    }

    private static MethodTest CreateTest(Method method)
    {
        return new MethodTest
        {
            Active = true,
            Method = method
        };
    }
}