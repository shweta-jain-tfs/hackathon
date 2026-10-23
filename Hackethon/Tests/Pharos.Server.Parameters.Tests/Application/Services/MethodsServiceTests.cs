using MockQueryable.Moq;
using Pharos.Server.Application.Security;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Services;

public class MethodsServiceTests
{
    private readonly Mock<IParametersDbContext> _mockContext;
    private readonly Mock<IAuthorizationService> _mockAuthorizationService;
    private readonly MethodsService _service;
    private readonly Fixture _fixture;

    public MethodsServiceTests()
    {
        _mockContext = new Mock<IParametersDbContext>();
        _mockAuthorizationService = new Mock<IAuthorizationService>();
        _mockAuthorizationService.Setup(x => x.RuoMode).Returns(false);
        _service = new MethodsService(_mockContext.Object, _mockAuthorizationService.Object);
        _fixture = new Fixture();
    }

    #region GetMethodTestByConfiguredLabNames Tests

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetMethodTestByConfiguredLabNames_InternalTestNameDoesNotMatchConfiguredLabName_ShouldReturnNull(bool forceUniqueLaboratoryTestNames)
    {
        // Arrange
        const string methodName = "sIgE";
        const string internalTestName = "Mtw1R";
        const string laboratoryTestName = "Mtw-1R";

        var method = CreateMethod(methodName);
        var concentrationUnit = CreateConcentrationUnit(method.Id, 1, "kUA/l");
        var methodTest = CreateMethodTest(method.Id, internalTestName, concentrationUnit.UnitId, method, concentrationUnit);
        var methodTestLabName = CreateMethodTestLabName(methodTest, laboratoryTestName);
        SetupDatabase([method], [concentrationUnit], [methodTest], [methodTestLabName]);

        // Act
        var result = await _service.GetMethodTestByLabName(
            methodName,
            internalTestName,
            x => x.TestName,
            forceUniqueLaboratoryTestNames);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetMethodTestByConfiguredLabNames_ConfiguredLabNameMatches_ShouldReturnMethodTest(bool forceUniqueLaboratoryTestNames)
    {
        // Arrange
        const string methodName = "sIgE";
        const string internalTestName = "Mtw1R";
        const string laboratoryTestName = "Mtw-1R";

        var method = CreateMethod(methodName);
        var concentrationUnit = CreateConcentrationUnit(method.Id, 1, "kUA/l");
        var methodTest = CreateMethodTest(method.Id, internalTestName, concentrationUnit.UnitId, method, concentrationUnit);
        var methodTestLabName = CreateMethodTestLabName(methodTest, laboratoryTestName);
        SetupDatabase([method], [concentrationUnit], [methodTest], [methodTestLabName]);

        // Act
        var result = await _service.GetMethodTestByLabName(
            methodName,
            laboratoryTestName,
            x => x.TestName,
            forceUniqueLaboratoryTestNames);

        // Assert
        result.Should().Be(internalTestName);
    }

    #endregion

    #region GetMethodTestByLabNames Tests

    [Fact]
    public async Task GetMethodTestByLabNames_InternalTestNameReceived_ShouldReturnMethodTest()
    {
        // Arrange
        const string methodName = "sIgE";
        const string internalTestName = "Mtw1R";

        var method = CreateMethod(methodName);
        var concentrationUnit = CreateConcentrationUnit(method.Id, 1, "kUA/l");
        var methodTest = CreateMethodTest(method.Id, internalTestName, concentrationUnit.UnitId, method, concentrationUnit);
        SetupDatabase([method], [concentrationUnit], [methodTest]);

        // Act
        var result = await _service.GetMethodTestByLabNameOrTestName(
            methodName,
            internalTestName,
            x => x.TestName);

        // Assert
        result.Should().Be(internalTestName);
    }

    #endregion

    #region GetUnitsByMethodTest Tests

    [Fact]
    public async Task GetUnitsByMethodTest_ReturnsEmptyDictionary_WhenNoMatchingMethodTestsFound()
    {
        // Arrange
        const string methodName = "sIgE";
        const string testName = "c1";
        const string unit = "kUA/l";

        var method = CreateMethod("DifferentMethod");
        var concentrationUnit = CreateConcentrationUnit(method.Id, 1, unit);
        var methodTest = CreateMethodTest(method.Id, "DifferentTest", concentrationUnit.UnitId, method, concentrationUnit);

        SetupDatabase([method], [concentrationUnit], [methodTest]);

        var methodTestPairs = new[] { (methodName, testName) };

        // Act
        var result = await _service.GetMethodTestConcentrationUnits(methodTestPairs);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Trait("ProductRequirement", "PR_339")]
    [Trait("ModuleRequirement", "MR_5951")]
    [Fact]
    public async Task GetUnitsByMethodTest_ReturnsSameUnit_WhenMultipleTestsShareSameConcentrationUnit()
    {
        // Arrange
        const string methodName = "sIgE";
        const string testName1 = "c1";
        const string testName2 = "c2";
        const string unit = "kUA/l";

        var method = CreateMethod(methodName);
        var concentrationUnit = CreateConcentrationUnit(method.Id, 1, unit);
        var methodTest1 = CreateMethodTest(method.Id, testName1, concentrationUnit.UnitId, method, concentrationUnit);
        var methodTest2 = CreateMethodTest(method.Id, testName2, concentrationUnit.UnitId, method, concentrationUnit);

        SetupDatabase([method], [concentrationUnit], [methodTest1, methodTest2]);

        var methodTestPairs = new[] { (methodName, testName1), (methodName, testName2) };

        // Act
        var result = await _service.GetMethodTestConcentrationUnits(methodTestPairs);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result![(methodName, testName1)].Should().Be(unit);
        result[(methodName, testName2)].Should().Be(unit);
    }

    [Trait("ProductRequirement", "PR_339")]
    [Trait("ModuleRequirement", "MR_5951")]
    [Fact]
    public async Task GetUnitsByMethodTest_ReturnsDifferentUnits_WhenTestsUseDifferentConcentrationUnits()
    {
        // Arrange
        const string methodName = "sIgE";
        const string testName1 = "c1";
        const string testName2 = "c2";
        const string unit1 = "kUA/l";
        const string unit2 = "mg/l";

        var method = CreateMethod(methodName);
        var concentrationUnit1 = CreateConcentrationUnit(method.Id, 1, unit1);
        var concentrationUnit2 = CreateConcentrationUnit(method.Id, 2, unit2);
        var methodTest1 = CreateMethodTest(method.Id, testName1, concentrationUnit1.UnitId, method, concentrationUnit1);
        var methodTest2 = CreateMethodTest(method.Id, testName2, concentrationUnit2.UnitId, method, concentrationUnit2);

        SetupDatabase([method], [concentrationUnit1, concentrationUnit2], [methodTest1, methodTest2]);

        var methodTestPairs = new[] { (methodName, testName1), (methodName, testName2) };

        // Act
        var result = await _service.GetMethodTestConcentrationUnits(methodTestPairs);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result![(methodName, testName1)].Should().Be(unit1);
        result[(methodName, testName2)].Should().Be(unit2);
    }

    #endregion

    #region Helper Methods

    private void SetupDatabase(
        List<Method> methods,
        List<MethodConcentrationUnit> concentrationUnits,
        List<MethodTest> methodTests,
        List<MethodTestLabName>? methodTestLabNames = null)
    {
        var methodsDbSet = methods.BuildMockDbSet();
        var concentrationUnitsDbSet = concentrationUnits.BuildMockDbSet();
        var methodTestsDbSet = methodTests.BuildMockDbSet();
        var methodTestLabNamesDbSet = (methodTestLabNames ?? []).BuildMockDbSet();

        _mockContext.Setup(c => c.Methods).Returns(methodsDbSet.Object);
        _mockContext.Setup(c => c.MethodConcentrationUnits).Returns(concentrationUnitsDbSet.Object);
        _mockContext.Setup(c => c.MethodTests).Returns(methodTestsDbSet.Object);
        _mockContext.Setup(c => c.MethodTestLabNames).Returns(methodTestLabNamesDbSet.Object);
    }

    private Method CreateMethod(string methodName)
    {
        return new Method
        {
            Id = _fixture.Create<int>(),
            MethodName = methodName,
            FullMethodName = $"Full {methodName}",
            Enabled = true,
            IsTemplate = false,
            IsRuo = false,
            SortOrder = 1
        };
    }

    private static MethodConcentrationUnit CreateConcentrationUnit(int methodId, int unitId, string concentrationUnit)
    {
        return new MethodConcentrationUnit
        {
            MethodId = methodId,
            UnitId = unitId,
            ConcentrationUnit = concentrationUnit
        };
    }

    private MethodTest CreateMethodTest(int methodId, string testName, int concentrationUnitId, Method method, MethodConcentrationUnit concentrationUnit)
    {
        return new MethodTest
        {
            Id = _fixture.Create<int>(),
            MethodId = methodId,
            TestName = testName,
            FullTestName = $"Full {testName}",
            Active = true,
            DefaultMethodConcentrationUnitId = concentrationUnitId,
            DefaultLabTestName = testName,
            LabTestNames = [],
            Method = method,
            DefaultConcentrationUnit = concentrationUnit
        };
    }

    private MethodTestLabName CreateMethodTestLabName(MethodTest methodTest, string laboratoryTestName)
    {
        var methodTestLabName = new MethodTestLabName
        {
            Id = _fixture.Create<int>(),
            MethodTestId = methodTest.Id,
            TestName = laboratoryTestName,
            SortOrder = 1,
            MethodTest = methodTest
        };

        methodTest.LabTestNames.Add(methodTestLabName);
        return methodTestLabName;
    }

    #endregion
}
