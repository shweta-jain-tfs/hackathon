using Microsoft.Extensions.Options;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;

namespace Pharos.Server.Parameters.Application.Commands.MethodTestDetails;

public class UpdateMethodTestDetailsCommandHandlerTests
{
    private readonly Mock<IParametersDbContext> _contextMock = new();
    private readonly Mock<IOptionsSnapshot<LisSettings>> _lisSettingsMock = new();

    public UpdateMethodTestDetailsCommandHandlerTests()
    {
        _contextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _lisSettingsMock.Setup(x => x.Value).Returns(new LisSettings());
    }

    [Trait("ProductRequirement", "PR_2588")]
    [Trait("ModuleRequirement", "MR_5957")]
    [Fact]
    public async Task Handle_WhenReportSettingsChanged_ShouldPersistUserReportSettings()
    {
        var methodTest = CreateMethodTest();
        SetupMethodTests(methodTest);
        var sut = new UpdateMethodTestDetailsCommandHandler(_contextMock.Object, _lisSettingsMock.Object);
        var command = new UpdateMethodTestDetailsCommand
        {
            Id = methodTest.Id,
            QueriedAt = DateTimeOffset.UtcNow,
            Replicates = 2,
            DilutionMethod = DilutionMethod.InstrumentDilution,
            DilutionFactor = 2,
            ReportConcentration = true,
            MethodConcentrationUnitId = 2,
            ReportCutOff = true,
            CutOffConcentration = 0.35,
            ReportClass = true,
            ReportClassId = 1,
            ReportQuotient = QuotientReportMode.ReportAsPercent,
            TextQuantitativeBelow = "<0.1",
            TextQuantitativeAbove = ">100",
            TextQuantitativeBelowForDilutedTest = "<0.2",
            TextQuantitativeAboveForDilutedTest = ">200",
            MethodTestLabNames = []
        };

        await sut.Handle(command, TestContext.Current.CancellationToken);

        methodTest.UserReportConcentration.Should().BeTrue();
        methodTest.MethodConcentrationUnitId.Should().Be(2);
        methodTest.UserReportCutOff.Should().BeTrue();
        methodTest.UserCutOffConcentration.Should().Be(0.35);
        methodTest.UserReportClassId.Should().Be(1);
        methodTest.UserReportQuotient.Should().Be(QuotientReportMode.ReportAsPercent);
        methodTest.TextQuantitativeBelow.Should().Be("<0.1");
        methodTest.TextQuantitativeAbove.Should().Be(">100");
        methodTest.UserTextQuantitativeBelowForDilutedTest.Should().Be("<0.2");
        methodTest.UserTextQuantitativeAboveForDilutedTest.Should().Be(">200");

        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_2588")]
    [Trait("ModuleRequirement", "MR_5957")]
    [Fact]
    public async Task Handle_WhenReportClassCleared_ShouldPersistClassIdAsZero()
    {
        var methodTest = CreateMethodTest();
        methodTest.UserReportClassId = 1;
        SetupMethodTests(methodTest);
        var sut = new UpdateMethodTestDetailsCommandHandler(_contextMock.Object, _lisSettingsMock.Object);
        var command = CreateCommand(methodTest);
        command.ReportClass = false;
        command.ReportClassId = 0;

        await sut.Handle(command, TestContext.Current.CancellationToken);

        methodTest.UserReportClassId.Should().Be(0);
    }

    private void SetupMethodTests(MethodTest methodTest)
    {
        _contextMock.Setup(x => x.MethodTests).ReturnsDbSet([methodTest]);
        _contextMock.Setup(x => x.MethodTestLabNames).ReturnsDbSet([]);
        _contextMock.Setup(x => x.MethodConcentrationUnits).ReturnsDbSet(methodTest.Method.ConcentrationUnits);
        _contextMock.Setup(x => x.MethodClassTables).ReturnsDbSet(methodTest.Method.ClassTables);
        _contextMock.Setup(x => x.MethodClassTableBelows).ReturnsDbSet(methodTest.Method.ClassTableBelows);
    }

    private static UpdateMethodTestDetailsCommand CreateCommand(MethodTest methodTest)
    {
        return new UpdateMethodTestDetailsCommand
        {
            Id = methodTest.Id,
            QueriedAt = DateTimeOffset.UtcNow,
            MethodConcentrationUnitId = methodTest.MethodConcentrationUnitId,
            MethodTestLabNames = []
        };
    }

    private static MethodTest CreateMethodTest()
    {
        var method = new Method
        {
            Id = 10,
            MethodName = "m1",
            ConcentrationUnits =
            [
                new MethodConcentrationUnit { MethodId = 10, UnitId = 1, ConcentrationUnit = "kU/L" },
                new MethodConcentrationUnit { MethodId = 10, UnitId = 2, ConcentrationUnit = "PAU/L" }
            ],
            ClassTables =
            [
                new MethodClassTable { MethodId = 10, ClassTableId = 1, ClassTableRowId = 1, DefaultTextAboveConcentration = "A" }
            ],
            ClassTableBelows =
            [
                new MethodClassTableBelow { MethodId = 10, ClassTableId = 1, DefaultTextBelowConcentration = "B" }
            ]
        };

        return new MethodTest
        {
            Id = 20,
            MethodId = method.Id,
            Method = method,
            TestName = "t1",
            DefaultMethodConcentrationUnitId = 1,
            LabTestNames = []
        };
    }
}
