using Moq.EntityFrameworkCore;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;
using MethodTest = Pharos.Server.Domain.Parameters.Methods.MethodTest;

namespace Pharos.Server.Parameters.Application.Queries.GetMethodsAndTest;

public class GetMethodTestsQueryHandlerTests
{
    private readonly Mock<IParametersDbContext> _dbMock;
    private readonly GetMethodTestsByMethodIdQueryHandler _sut;

    private const int _defaultMethodId = 1;

    public GetMethodTestsQueryHandlerTests()
    {
        _dbMock = new Mock<IParametersDbContext>();
        _sut = new GetMethodTestsByMethodIdQueryHandler(_dbMock.Object);
    }

    [Theory]
    [InlineData(false, false, 1)] // active, not disabled => returned
    [InlineData(false, true, 0)]  // disabled => excluded
    [InlineData(true, false, 1)]  // inactive but not disabled => returned
    public async Task Handle_Should_OnlyReturnNonDisabledTests(bool inactive, bool disabled, int expectedCount)
    {
        var method = CreateMethod();
        SetupDb([CreateTest(method, active: !inactive, disabled: disabled)]);

        var result = await _sut.Handle(CreateQuery(), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(expectedCount);
    }

    [Fact]
    public async Task Handle_WhenNoMethodTestsExist_Should_ReturnEmptyList()
    {
        SetupDb([]);

        var result = await _sut.Handle(CreateQuery(), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenMultipleTestsExist_Should_ReturnAllNonDisabled()
    {
        var method = CreateMethod();
        SetupDb([
            CreateTest(method, testName: "t1"),
            CreateTest(method, testName: "t2"),
            CreateTest(method, testName: "t3", disabled: true),
            CreateTest(method, testName: "t4", active: false),
        ]);

        var result = await _sut.Handle(CreateQuery(), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(3);
        result.Items.Select(x => x.TestName).Should().BeEquivalentTo(["t1", "t2", "t4"]);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_341")]
    [Trait("ProductRequirement", "PR_2764")]
    public async Task Handle_ShouldReturnRegularTests_WhenSpecializedActivationDefaultsToFalse()
    {
        var method = CreateMethod();
        SetupDb([
            CreateTest(method, testName: "regular"),
            CreateTest(method, testName: "inactiveRuo", isRuo: true),
            CreateTest(method, testName: "activeRuo", isRuo: true, isSpecializedTestActivated: true),
            CreateTest(method, testName: "inactiveOptional", specialMethodName: "Special"),
            CreateTest(method, testName: "activeOptional", specialMethodName: "Special", isSpecializedTestActivated: true)
        ]);

        var result = await _sut.Handle(CreateQuery(), TestContext.Current.CancellationToken);

        result.Items.Select(x => x.TestName).Should().BeEquivalentTo(["regular", "activeRuo", "activeOptional"]);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_341")]
    [Trait("ProductRequirement", "PR_2764")]
    public async Task Handle_WhenRuoTestBelongsToRuoMethod_ShouldReturnTest()
    {
        var ruoMethod = CreateMethod(isRuo: true);
        SetupDb([
            CreateTest(ruoMethod, testName: "activeRuoTest", isRuo: true, isSpecializedTestActivated: false)
        ]);

        var result = await _sut.Handle(CreateQuery(), TestContext.Current.CancellationToken);

        result.Items.Select(x => x.TestName).Should().BeEquivalentTo(["activeRuoTest"]);
    }

    [Fact]
    public async Task Handle_WhenMultipleMethodsPresent_Should_ReturnOnlyTestsForRequestedMethod()
    {
        var method1 = CreateMethod(id: _defaultMethodId);
        var method2 = CreateMethod(id: 2);
        SetupDb([CreateTest(method1, testName: "t1"), CreateTest(method2, testName: "t2")]);

        var result = await _sut.Handle(CreateQuery(methodId: _defaultMethodId), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(x => x.TestName == "t1");
    }

    [Theory]
    [InlineData("cat", "cat1", true)]   // prefix match on TestName
    [InlineData("cat", "CAT1", true)]   // case-insensitive match
    [InlineData("cat", "dog1", false)]  // no match
    [InlineData("", "cat1", true)]      // empty search returns all
    [InlineData("   ", "cat1", true)]   // whitespace search returns all
    public async Task Handle_SearchByTestName_Should_FilterCorrectly(string search, string testName, bool expectMatch)
    {
        var method = CreateMethod();
        SetupDb([CreateTest(method, testName: testName)]);

        var result = await _sut.Handle(CreateQuery(searchString: search), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(expectMatch ? 1 : 0);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5875")]
    public async Task Handle_WhenSearchStringMatchesLabTestNamePrefix_Should_ReturnMatchingTest()
    {
        var method = CreateMethod();
        var test1 = CreateTest(method, testName: "t1");
        test1.LabTestNames = [new MethodTestLabName { TestName = "AllergyA" }];
        var test2 = CreateTest(method, testName: "t2");
        test2.LabTestNames = [new MethodTestLabName { TestName = "Other" }];
        SetupDb([test1, test2]);

        var result = await _sut.Handle(CreateQuery(searchString: "Allergy"), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(x => x.TestName == "t1");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5875")]
    public async Task Handle_WhenLabTestNamesExist_Should_IncludeThemInResult()
    {
        var method = CreateMethod();
        var test = CreateTest(method, testName: "t1");
        test.LabTestNames =
        [
            new MethodTestLabName { Id = 10, TestName = "LabName1", SortOrder = 1 },
            new MethodTestLabName { Id = 11, TestName = "LabName2", SortOrder = 2 }
        ];
        SetupDb([test]);

        var result = await _sut.Handle(CreateQuery(), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(1);
        var dto = result.Items.Single();
        dto.MethodTestLabNames.Should().HaveCount(2);
        dto.MethodTestLabNames.Should().ContainSingle(x => x.TestName == "LabName1" && x.SortOrder == 1);
        dto.MethodTestLabNames.Should().ContainSingle(x => x.TestName == "LabName2" && x.SortOrder == 2);
    }

    [Theory]
    [InlineData("da", false)]  // < 3 chars: no FullTestName contains match
    [InlineData("dan", true)]  // >= 3 chars: FullTestName contains match
    public async Task Handle_FullTestNameContainsSearch_Should_OnlyApplyWhenSearchIsAtLeast3Chars(string search, bool expectMatch)
    {
        var method = CreateMethod();
        SetupDb([
            CreateTest(method, testName: "t1", fullTestName: "Cat dander"),
            CreateTest(method, testName: "t2", fullTestName: "Dog hair"),
        ]);

        var result = await _sut.Handle(CreateQuery(searchString: search), TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(expectMatch ? 1 : 0);
        if (expectMatch)
        {
            result.Items.Should().ContainSingle(x => x.TestName == "t1");
        }
    }

    private void SetupDb(List<MethodTest> tests)
    {
        _dbMock.Setup(x => x.MethodTests).ReturnsDbSet(tests);
    }

    private static GetMethodTestsQuery CreateQuery(
        int methodId = _defaultMethodId,
        string? searchString = null,
        bool ascending = true) =>
        new()
        {
            MethodId = methodId,
            SearchString = searchString,
            SortByAscending = ascending,
            Count = 100
        };

    private static Method CreateMethod(int id = _defaultMethodId, int sortOrder = 0, bool isRuo = false) =>
        new()
        {
            Id = id,
            MethodName = $"Method{id}",
            FullMethodName = $"Full Method {id}",
            SortOrder = sortOrder,
            IsRuo = isRuo
        };

    private static MethodTest CreateTest(
        Method method,
        string testName = "test",
        string fullTestName = "Full Test Name",
        bool active = true,
        bool disabled = false,
        bool isRuo = false,
        string? specialMethodName = null,
        bool isSpecializedTestActivated = false)
    {
        var concentrationUnit = new MethodConcentrationUnit
        {
            UnitId = 1,
            ConcentrationUnit = "kU/L",
        };

        return new()
        {
            Method = method,
            MethodId = method.Id,
            TestName = testName,
            FullTestName = fullTestName,
            Active = active,
            Disabled = disabled,
            IsRuo = isRuo,
            SpecialMethodName = specialMethodName,
            IsSpecializedTestActivated = isSpecializedTestActivated,
            LabTestNames = [],
            DefaultMethodConcentrationUnitId = concentrationUnit.UnitId,
            DefaultConcentrationUnit = concentrationUnit
        };
    }
}
