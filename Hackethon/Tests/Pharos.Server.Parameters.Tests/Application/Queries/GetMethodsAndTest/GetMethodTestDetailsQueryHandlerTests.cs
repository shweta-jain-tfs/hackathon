using Microsoft.Extensions.Options;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure;
using Pharos.Server.Parameters.Application.Interfaces;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;

namespace Pharos.Server.Parameters.Application.Queries.GetMethodsAndTest;

public class GetMethodTestDetailsQueryHandlerTests
{
    private readonly Mock<IParametersDbContext> _dbMock = new();
    private readonly Mock<IDateTime> _dateTimeMock = new();
    private readonly Mock<IOptionsSnapshot<LisSettings>> _lisSettingsMock = new();

    public GetMethodTestDetailsQueryHandlerTests()
    {
        _dateTimeMock.Setup(x => x.Now).Returns(new DateTimeOffset(2026, 5, 8, 10, 0, 0, TimeSpan.Zero));
        _lisSettingsMock.Setup(x => x.Value).Returns(new LisSettings());
    }

    [Fact]
    [Trait("ProductRequirement", "PR_339")]
    [Trait("ModuleRequirement", "MR_5952")]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5875")]
    public async Task Handle_WhenLabNamesExist_Should_ReturnNamesOrderedBySortOrder()
    {
        var methodTest = CreateMethodTest();
        var labNames = new List<MethodTestLabName>
        {
            new() { Id = 1, MethodTest = methodTest, TestName = "B", SortOrder = 2 },
            new() { Id = 2, MethodTest = methodTest, TestName = "A", SortOrder = 1 }
        };
        SetupDb(methodTest, labNames);
        var sut = new GetMethodTestDetailsQueryHandler(_dbMock.Object, _dateTimeMock.Object, _lisSettingsMock.Object);

        var result = await sut.Handle(new GetMethodTestDetailsQuery(methodTest.Id), TestContext.Current.CancellationToken);

        result.MethodTestLabNames.Should().NotBeNull();
        result.MethodTestLabNames!.Select(x => x.TestName).Should().Equal("A", "B");
        result.QueriedAt.Should().Be(new DateTimeOffset(2026, 5, 8, 10, 0, 0, TimeSpan.Zero));
        result.MethodConcentrationUnitId.Should().Be(2);
        result.DefaultConcentrationUnit.Should().Be("PAU/L");
        result.ConcentrationUnits.Select(x => x.UnitId).Should().Equal(1, 2);
        result.ClassTables.Select(x => x.ClassTableId).Should().Equal(1);
        result.ReportConcentration.Should().BeTrue();
        result.DefaultReportConcentration.Should().BeTrue();
        result.ReportCutOff.Should().BeTrue();
        result.DefaultReportCutOff.Should().BeTrue();
        result.DefaultCutOffConcentration.Should().Be(0.35);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_348")]
    [Trait("ModuleRequirement", "MR_5952")]
    [Trait("ProductRequirement", "PR_570")]
    [Trait("ModuleRequirement", "MR_5875")]
    public async Task Handle_WhenNoLabNamesExist_Should_ReturnDefaultLabTestName()
    {
        var methodTest = CreateMethodTest();
        SetupDb(methodTest, []);
        var sut = new GetMethodTestDetailsQueryHandler(_dbMock.Object, _dateTimeMock.Object, _lisSettingsMock.Object);

        var result = await sut.Handle(new GetMethodTestDetailsQuery(methodTest.Id), TestContext.Current.CancellationToken);

        result.MethodTestLabNames.Should().ContainSingle();
        result.MethodTestLabNames![0].TestName.Should().Be(methodTest.DefaultLabTestName);
        result.MethodTestLabNames[0].SortOrder.Should().Be(1);
    }

    private void SetupDb(MethodTest methodTest, List<MethodTestLabName> labNames)
    {
        methodTest.LabTestNames = labNames;
        _dbMock.Setup(x => x.MethodTests).ReturnsDbSet([methodTest]);
        _dbMock.Setup(x => x.MethodTestLabNames).ReturnsDbSet(labNames);
    }

    private static MethodTest CreateMethodTest()
    {
        var method = new Method
        {
            Id = 10,
            MethodName = "m1",
            FullMethodName = "Method 1",
            ConcentrationUnits =
            [
                new MethodConcentrationUnit { MethodId = 10, UnitId = 2, ConcentrationUnit = "PAU/L" },
                new MethodConcentrationUnit { MethodId = 10, UnitId = 1, ConcentrationUnit = "kU/L" }
            ],
            ClassTables =
            [
                new MethodClassTable { MethodId = 10, ClassTableId = 1, ClassTableRowId = 1, DefaultTextAboveConcentration = "A" },
                new MethodClassTable { MethodId = 10, ClassTableId = 2, ClassTableRowId = 1, DefaultTextAboveConcentration = "C" }
            ],
            ClassTableBelows =
            [
                new MethodClassTableBelow { MethodId = 10, ClassTableId = 1, DefaultTextBelowConcentration = "B" },
                new MethodClassTableBelow { MethodId = 10, ClassTableId = 2, DefaultTextBelowConcentration = null }
            ]
        };

        return new MethodTest
        {
            Id = 20,
            MethodId = method.Id,
            Method = method,
            TestName = "t1",
            FullTestName = "Test 1",
            DefaultConcentrationUnit = method.ConcentrationUnits[0],
            DefaultMethodConcentrationUnitId = 2,
            DefaultLabTestName = "LAB1",
            DefaultReportConcentration = true,
            UserReportConcentration = true,
            DefaultReportCutOff = true,
            UserReportCutOff = true,
            DefaultCutOffConcentration = 0.35,
            DiluentIdentity = "DIL",
            LabTestNames = []
        };
    }
}
