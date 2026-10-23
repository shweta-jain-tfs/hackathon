using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using TestOrder = Pharos.Server.Request.Domain.TestOrder;

namespace Pharos.Server.Request.Application.Commands.TestOrderScheduling;

[Collection(nameof(RequestTestsCollection))]
public sealed class RequestExtensionsTests(RequestTestsWaf factory) : DatabaseTestBase<RequestTestsWaf>(factory)
{
    private const string _activeTest1 = "c1";
    private const string _activeTest2 = "c2";
    private const string _inactiveMethod = "M2";
    private const string _inactiveTest = "c3";

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        var db = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var method = await db.CreateMethod(testNames: [_activeTest1, _activeTest2, _inactiveTest]);
        method.Tests.First(x => x.TestName == _inactiveTest).Active = false;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        method = await db.CreateMethod(methodName: _inactiveMethod, testNames: [_activeTest1]);
        method.Enabled = false;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    #region GetNotStartedEnabledTests Tests

    [Fact]
    public async Task GetNotStartedEnabledTests_Should_ReturnEmptyList_When_NoNotStartedTests()
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            TestOrders = new List<TestOrder>
            {
                new()
                {
                    MethodName = ParametersDbContextExtensions.MethodName,
                    TestName = _activeTest1,
                    State = TestOrderState.Scheduled,
                    IsActive = true
                }
            }
        };

        // Act
        await using var scope = Factory.Services.CreateAsyncScope();
        var methods = scope.ServiceProvider.GetRequiredService<IMethodsService>();
        var result = await request.GetNotStartedEnabledTests(methods);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNotStartedEnabledTests_Should_ReturnEmptyList_When_NoActiveTests()
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            TestOrders = new List<TestOrder>
            {
                new()
                {
                    MethodName = ParametersDbContextExtensions.MethodName,
                    TestName = _activeTest1,
                    State = TestOrderState.NotStarted,
                    IsActive = false
                }
            }
        };

        // Act
        await using var scope = Factory.Services.CreateAsyncScope();
        var methods = scope.ServiceProvider.GetRequiredService<IMethodsService>();
        var result = await request.GetNotStartedEnabledTests(methods);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNotStartedEnabledTests_Should_ReturnEmptyList_When_TestNotEnabledInMethod()
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            TestOrders = new List<TestOrder>
            {
                new()
                {
                    MethodName = ParametersDbContextExtensions.MethodName,
                    TestName = _inactiveTest,
                    State = TestOrderState.NotStarted,
                    IsActive = true
                }
            }
        };

        // Act
        await using var scope = Factory.Services.CreateAsyncScope();
        var methods = scope.ServiceProvider.GetRequiredService<IMethodsService>();
        var result = await request.GetNotStartedEnabledTests(methods);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNotStartedEnabledTests_Should_ReturnEmptyList_When_MethodNotEnabled()
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            TestOrders = new List<TestOrder>
            {
                new()
                {
                    MethodName = _inactiveMethod,
                    TestName = _activeTest1,
                    State = TestOrderState.NotStarted,
                    IsActive = true
                }
            }
        };

        // Act
        await using var scope = Factory.Services.CreateAsyncScope();
        var methods = scope.ServiceProvider.GetRequiredService<IMethodsService>();
        var result = await request.GetNotStartedEnabledTests(methods);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNotStartedEnabledTests_Should_ReturnEnabledTests_When_TestsAreNotStartedActiveAndEnabled()
    {
        // Arrange
        var testOrder1 = new TestOrder
        {
            MethodName = ParametersDbContextExtensions.MethodName,
            TestName = _activeTest1,
            State = TestOrderState.NotStarted,
            IsActive = true
        };

        var testOrder2 = new TestOrder
        {
            MethodName = ParametersDbContextExtensions.MethodName,
            TestName = _activeTest2,
            State = TestOrderState.NotStarted,
            IsActive = true
        };

        var request = new Domain.Request
        {
            SampleId = "S1",
            TestOrders = new List<TestOrder> { testOrder1, testOrder2 }
        };

        // Act
        await using var scope = Factory.Services.CreateAsyncScope();
        var methods = scope.ServiceProvider.GetRequiredService<IMethodsService>();
        var result = await request.GetNotStartedEnabledTests(methods);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(testOrder1);
        result.Should().Contain(testOrder2);
    }

    [Fact]
    public async Task GetNotStartedEnabledTests_Should_FilterOutDisabledTests_When_SomeTestsAreNotEnabled()
    {
        // Arrange
        var testOrder1 = new TestOrder
        {
            MethodName = ParametersDbContextExtensions.MethodName,
            TestName = _activeTest1,
            State = TestOrderState.NotStarted,
            IsActive = true
        };

        var testOrder2 = new TestOrder
        {
            MethodName = ParametersDbContextExtensions.MethodName,
            TestName = _inactiveTest,
            State = TestOrderState.NotStarted,
            IsActive = true
        };

        var testOrder3 = new TestOrder
        {
            MethodName = ParametersDbContextExtensions.MethodName,
            TestName = _activeTest2,
            State = TestOrderState.NotStarted,
            IsActive = true
        };

        var request = new Domain.Request
        {
            SampleId = "S1",
            TestOrders = new List<TestOrder> { testOrder1, testOrder2, testOrder3 }
        };

        // Act
        await using var scope = Factory.Services.CreateAsyncScope();
        var methods = scope.ServiceProvider.GetRequiredService<IMethodsService>();
        var result = await request.GetNotStartedEnabledTests(methods);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(testOrder1);
        result.Should().NotContain(testOrder2);
        result.Should().Contain(testOrder3);
    }

    [Fact]
    public async Task GetNotStartedEnabledTests_Should_OnlyReturnNotStartedTests_When_MixedStates()
    {
        // Arrange
        var testOrder1 = new TestOrder
        {
            MethodName = ParametersDbContextExtensions.MethodName,
            TestName = _activeTest1,
            State = TestOrderState.NotStarted,
            IsActive = true
        };

        var testOrder2 = new TestOrder
        {
            MethodName = ParametersDbContextExtensions.MethodName,
            TestName = _activeTest2,
            State = TestOrderState.Scheduled,
            IsActive = true
        };

        var testOrder3 = new TestOrder
        {
            MethodName = ParametersDbContextExtensions.MethodName,
            TestName = _activeTest1,
            State = TestOrderState.Approved,
            IsActive = true
        };

        var request = new Domain.Request
        {
            SampleId = "S1",
            TestOrders = new List<TestOrder> { testOrder1, testOrder2, testOrder3 }
        };

        // Act
        await using var scope = Factory.Services.CreateAsyncScope();
        var methods = scope.ServiceProvider.GetRequiredService<IMethodsService>();
        var result = await request.GetNotStartedEnabledTests(methods);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.Should().Contain(testOrder1);
    }

    #endregion

    #region GetTubeType Tests

    [Fact]
    public void GetTubeType_Should_ReturnNormalTube_When_SampleTubeTypeIsNull()
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleTubeType = null
        };

        var settings = new TubeTypesSettings
        {
            NormalTubeDefinitionId = "1",
            TubeTypeList = new List<TubeType>
            {
                new()
                {
                    Name = "NormalTube",
                    TubeDefinitionId = 1,
                    InnerBottomLevel = 2,
                    InnerDiameter = 15
                },
                new()
                {
                    Name = "PediatricTube",
                    TubeDefinitionId = 2,
                    InnerBottomLevel = 1,
                    InnerDiameter = 10
                }
            }
        };

        // Act
        var result = request.GetTubeType(settings);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("NormalTube");
        result.TubeDefinitionId.Should().Be(1);
    }

    [Fact]
    public void GetTubeType_Should_ReturnMatchingTube_When_SampleTubeTypeIsSet()
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleTubeType = 2
        };

        var settings = new TubeTypesSettings
        {
            NormalTubeDefinitionId = "1",
            TubeTypeList = new List<TubeType>
            {
                new()
                {
                    Name = "NormalTube",
                    TubeDefinitionId = 1,
                    InnerBottomLevel = 2,
                    InnerDiameter = 15
                },
                new()
                {
                    Name = "PediatricTube",
                    TubeDefinitionId = 2,
                    InnerBottomLevel = 1,
                    InnerDiameter = 10
                }
            }
        };

        // Act
        var result = request.GetTubeType(settings);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("PediatricTube");
        result.TubeDefinitionId.Should().Be(2);
    }

    [Fact]
    public void GetTubeType_Should_ReturnNull_When_NoMatchingTubeTypeFound()
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleTubeType = 999
        };

        var settings = new TubeTypesSettings
        {
            NormalTubeDefinitionId = "1",
            TubeTypeList = new List<TubeType>
            {
                new()
                {
                    Name = "NormalTube",
                    TubeDefinitionId = 1,
                    InnerBottomLevel = 2,
                    InnerDiameter = 15
                }
            }
        };

        // Act
        var result = request.GetTubeType(settings);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetTubeType_Should_ReturnNull_When_NormalTubeDropDownNotFound()
    {
        // Arrange
        var request = new Domain.Request
        {
            SampleId = "S1",
            SampleTubeType = null
        };

        var settings = new TubeTypesSettings
        {
            NormalTubeDefinitionId = "999",
            TubeTypeList = new List<TubeType>
            {
                new()
                {
                    Name = "NormalTube",
                    TubeDefinitionId = 1,
                    InnerBottomLevel = 2,
                    InnerDiameter = 15
                }
            }
        };

        // Act
        var result = request.GetTubeType(settings);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
