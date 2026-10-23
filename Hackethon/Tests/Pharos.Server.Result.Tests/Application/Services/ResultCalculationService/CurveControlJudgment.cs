using System.Reflection;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Security;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Services;
using Pharos.Server.Result.Application.Events.TestResultReady;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Services.ResultCalculationService;

public class CurveControlJudgmentTests
{
    private readonly Mock<IResultCalculationService> _resultCalculationServiceMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IResultDbContext> _dbContextMock = new();
    private readonly Mock<IParametersDbContext> _parametersContextMock = new();
    private readonly CurveControlResultGroupReadyEventHandler _sutInstance;
    private readonly MethodInfo? _sut;


    public CurveControlJudgmentTests()
    {
        var methodsService = new MethodsService(_parametersContextMock.Object, new Mock<IAuthorizationService>().Object);
        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(x => x.Now).Returns(DateTimeOffset.UtcNow);
        var loggerMock = new Mock<ILogger<CurveControlResultGroupReadyEventHandler>>();

        _sutInstance = new CurveControlResultGroupReadyEventHandler(_dbContextMock.Object, methodsService, _resultCalculationServiceMock.Object, _mediatorMock.Object, dateTimeMock.Object, loggerMock.Object);
        _sut = GetHandleCurveControlTestOrdersMethod();
    }

    [Theory]
    [InlineData(50, 55, false, ConcentrationStatus.Ok, CurveControlJudgment.OK, CurveControlJudgment.OK, CurveControlJudgment.OK)]
    [Trait("ModuleRequirement", "MR_3512")]
    public void CurveControlJudgmentInsideLimits(
        double concentrationReplicate1,
        double concentrationReplicate2,
        bool lowRu,
        ConcentrationStatus concentrationStatus,
        CurveControlJudgment judgmentSummary,
        CurveControlJudgment judgmentReplicate1,
        CurveControlJudgment judgmentReplicate2)
    {
        PrepareMethodParameters(); // DefinedConcentration = 50.0, ExpectedSD = 5, InnerSDLimit = 1, OuterSDLimit = 2
        var ccList = PrepareCurveControls(concentrationReplicate1, concentrationReplicate2, lowRu, concentrationStatus, concentrationStatus);

        _sut.Should().NotBeNull();
        _sut?.Invoke(_sutInstance, [ccList, "sIgE", null, false, false, TestContext.Current.CancellationToken]);

        CheckCurveControls(ccList.FirstOrDefault(), judgmentSummary, judgmentReplicate1, judgmentReplicate2);
    }

    [Theory]
    [InlineData(55.001, 50, false, ConcentrationStatus.Ok, CurveControlJudgment.FlaggedHigh, CurveControlJudgment.FlaggedHigh, CurveControlJudgment.OK)]
    [InlineData(50, 55.001, false, ConcentrationStatus.Ok, CurveControlJudgment.FlaggedHigh, CurveControlJudgment.OK, CurveControlJudgment.FlaggedHigh)]
    [Trait("ModuleRequirement", "MR_3513")]
    public async Task CurveControlJudgmentFlaggedHigh(
        double concentrationReplicate1,
        double concentrationReplicate2,
        bool lowRu,
        ConcentrationStatus concentrationStatus,
        CurveControlJudgment judgmentSummary,
        CurveControlJudgment judgmentReplicate1,
        CurveControlJudgment judgmentReplicate2)
    {
        PrepareMethodParameters(); // DefinedConcentration = 50.0, ExpectedSD = 5, InnerSDLimit = 1, OuterSDLimit = 2
        var ccList = PrepareCurveControls(concentrationReplicate1, concentrationReplicate2, lowRu, concentrationStatus, concentrationStatus);

        _sut.Should().NotBeNull();
        _sut?.Invoke(_sutInstance, [ccList, "sIgE", null, false, false, TestContext.Current.CancellationToken]);

        CheckCurveControls(ccList.FirstOrDefault(), judgmentSummary, judgmentReplicate1, judgmentReplicate2);

        await Task.CompletedTask;
    }

    [Theory]
    [InlineData(60.001, 60.001, false, ConcentrationStatus.Ok, CurveControlJudgment.High, CurveControlJudgment.High, CurveControlJudgment.High)]
    [InlineData(60.001, 50, false, ConcentrationStatus.Ok, CurveControlJudgment.High, CurveControlJudgment.High, CurveControlJudgment.OK)]
    [InlineData(55.001, 55.001, false, ConcentrationStatus.Ok, CurveControlJudgment.High, CurveControlJudgment.FlaggedHigh, CurveControlJudgment.FlaggedHigh)] // 2 Flagged high => High
    [Trait("ModuleRequirement", "MR_3514")]
    public async Task CurveControlJudgmentHigh(
        double concentrationReplicate1,
        double concentrationReplicate2,
        bool lowRu,
        ConcentrationStatus concentrationStatus,
        CurveControlJudgment judgmentSummary,
        CurveControlJudgment judgmentReplicate1,
        CurveControlJudgment judgmentReplicate2)
    {
        PrepareMethodParameters(); // DefinedConcentration = 50.0, ExpectedSD = 5, InnerSDLimit = 1, OuterSDLimit = 2
        var ccList = PrepareCurveControls(concentrationReplicate1, concentrationReplicate2, lowRu, concentrationStatus, concentrationStatus);

        _sut.Should().NotBeNull();
        _sut?.Invoke(_sutInstance, [ccList, "sIgE", null, false, false, TestContext.Current.CancellationToken]);

        CheckCurveControls(ccList.FirstOrDefault(), judgmentSummary, judgmentReplicate1, judgmentReplicate2);

        await Task.CompletedTask;
    }

    [Theory]
    [InlineData(44.999, 50, false, ConcentrationStatus.Ok, CurveControlJudgment.FlaggedLow, CurveControlJudgment.FlaggedLow, CurveControlJudgment.OK)]
    [InlineData(50, 44.999, false, ConcentrationStatus.Ok, CurveControlJudgment.FlaggedLow, CurveControlJudgment.OK, CurveControlJudgment.FlaggedLow)]
    [Trait("ModuleRequirement", "MR_3515")]
    public async Task CurveControlJudgmentFlaggedLow(
        double concentrationReplicate1,
        double concentrationReplicate2,
        bool lowRu,
        ConcentrationStatus concentrationStatus,
        CurveControlJudgment judgmentSummary,
        CurveControlJudgment judgmentReplicate1,
        CurveControlJudgment judgmentReplicate2)
    {
        PrepareMethodParameters(); // DefinedConcentration = 50.0, ExpectedSD = 5, InnerSDLimit = 1, OuterSDLimit = 2
        var ccList = PrepareCurveControls(concentrationReplicate1, concentrationReplicate2, lowRu, concentrationStatus, concentrationStatus);

        _sut.Should().NotBeNull();
        _sut?.Invoke(_sutInstance, [ccList, "sIgE", null, false, false, TestContext.Current.CancellationToken]);

        CheckCurveControls(ccList.FirstOrDefault(), judgmentSummary, judgmentReplicate1, judgmentReplicate2);

        await Task.CompletedTask;
    }

    [Theory]
    [InlineData(39.999, 39.999, false, ConcentrationStatus.Ok, ConcentrationStatus.Ok, CurveControlJudgment.Low, CurveControlJudgment.Low, CurveControlJudgment.Low)]
    [InlineData(39.999, 50, false, ConcentrationStatus.Ok, ConcentrationStatus.Ok, CurveControlJudgment.Low, CurveControlJudgment.Low, CurveControlJudgment.OK)]
    [InlineData(44.999, 44.999, false, ConcentrationStatus.Ok, ConcentrationStatus.Ok, CurveControlJudgment.Low, CurveControlJudgment.FlaggedLow, CurveControlJudgment.FlaggedLow)]
    [InlineData(50, 50, true, ConcentrationStatus.Ok, ConcentrationStatus.NotAvailable, CurveControlJudgment.Low, CurveControlJudgment.OK, CurveControlJudgment.Low)] // One low replicate and one lowRU replicate
    [InlineData(50, 50, true, ConcentrationStatus.NotAvailable, ConcentrationStatus.NotAvailable, CurveControlJudgment.NotDefined, CurveControlJudgment.Low, CurveControlJudgment.Low)] // Two lowRU replicate
    [InlineData(39.999, 39.999, false, ConcentrationStatus.NotAvailable, ConcentrationStatus.NotAvailable, CurveControlJudgment.NotDefined, CurveControlJudgment.NotDefined, CurveControlJudgment.NotDefined)]
    [Trait("ModuleRequirement", "MR_3516")]
    public async Task CurveControlJudgmentLow(
        double concentrationReplicate1,
        double concentrationReplicate2,
        bool lowRu,
        ConcentrationStatus concentrationStatus1,
        ConcentrationStatus concentrationStatus2,
        CurveControlJudgment judgmentSummary,
        CurveControlJudgment judgmentReplicate1,
        CurveControlJudgment judgmentReplicate2)
    {
        PrepareMethodParameters(); // DefinedConcentration = 50.0, ExpectedSD = 5, InnerSDLimit = 1, OuterSDLimit = 2
        var ccList = PrepareCurveControls(concentrationReplicate1, concentrationReplicate2, lowRu, concentrationStatus1, concentrationStatus2);

        _sut.Should().NotBeNull();
        _sut?.Invoke(_sutInstance, [ccList, "sIgE", null, false, false, TestContext.Current.CancellationToken]);

        CheckCurveControls(ccList.FirstOrDefault(), judgmentSummary, judgmentReplicate1, judgmentReplicate2);

        await Task.CompletedTask;
    }

    [Theory]
    [InlineData(CurveControlJudgment.NotDefined, CurveControlJudgment.NotDefined, CurveControlJudgment.NotDefined, true, true)]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.OK, CurveControlJudgment.NotDefined, false, true)]
    [InlineData(CurveControlJudgment.OK, CurveControlJudgment.NotDefined, CurveControlJudgment.OK, true, false)]
    [Trait("ModuleRequirement", "MR_4053")]
    public void AbortedCurveControlReplicates(
        CurveControlJudgment judgmentSummary,
        CurveControlJudgment judgmentReplicate1,
        CurveControlJudgment judgmentReplicate2,
        bool replicate1Aborted,
        bool replicate2Aborted)
    {
        var concentrationStatus1 = ConcentrationStatus.Ok;
        var concentrationStatus2 = ConcentrationStatus.Ok;

        if (replicate1Aborted)
        {
            concentrationStatus1 = ConcentrationStatus.NotAvailable;
        }
        if (replicate2Aborted)
        {
            concentrationStatus2 = ConcentrationStatus.NotAvailable;
        }

        PrepareMethodParameters(); // DefinedConcentration = 50.0, ExpectedSD = 5, InnerSDLimit = 1, OuterSDLimit = 2
        var ccList = PrepareCurveControls(50, 50, false, concentrationStatus1, concentrationStatus2);

        foreach (var cc in ccList)
        {
            foreach (var repl in cc.Replicates!)
            {
                repl.IsAborted = replicate1Aborted;
                if (!replicate2Aborted)
                {
                    break;
                }
            }
        }

        _sut.Should().NotBeNull();
        _sut?.Invoke(_sutInstance, [ccList, "sIgE", null, false, false, TestContext.Current.CancellationToken]);

        CheckCurveControls(ccList.FirstOrDefault(), judgmentSummary, judgmentReplicate1, judgmentReplicate2);
    }

    [Theory]
    [InlineData(39.999)] // High
    [InlineData(40.0)]   // Flagged high
    [InlineData(44.999)] // Flagged high
    [InlineData(45.0)]   // OK
    [InlineData(50.0)]   // OK
    [InlineData(55.0)]   // OK
    [InlineData(55.001)] // Flagged low
    [InlineData(60.0)]   // Flagged low
    [InlineData(60.001)] // Low
    [Trait("ModuleRequirement", "MR_3517")]
    public async Task CurveControlJudgment_CalculateLimits(
        double expectedConcentration)
    {
        PrepareMethodParameters(expectedConcentration); // DefinedConcentration = inline data, ExpectedSD = 5, InnerSDLimit = 1, OuterSDLimit = 2
        var ccList = PrepareCurveControls(50.0, 50.0);

        _sut.Should().NotBeNull();
        _sut?.Invoke(_sutInstance, [ccList, "sIgE", null, false, false, TestContext.Current.CancellationToken]);

        var expectedJudgement = GetExpectedJudgement(50.0, expectedConcentration, 5.0, 1.0, 2.0);

        var cc = ccList.First();
        var replicate = cc.Replicates!.First() as CurveControlTestReplicate;
        replicate.Should().NotBeNull();
        replicate!.Judgment.Should().Be(expectedJudgement);

        await Task.CompletedTask;
    }

    [Fact]
    public void HandleCurveControlTestOrders_WhenSuppressCurveControlChartUpdates_IsTrue_ShouldStillSetJudgments()
    {
        PrepareMethodParameters();
        var ccList = PrepareCurveControls(55.001, 39.999, false, ConcentrationStatus.Ok, ConcentrationStatus.Ok);

        _sut.Should().NotBeNull();
        _sut?.Invoke(_sutInstance, [ccList, "sIgE", null, false, true, TestContext.Current.CancellationToken]);

        var cc = ccList.First();
        cc.JudgmentSummary.Should().NotBe(CurveControlJudgment.NotDefined);
        (cc.Replicates![0] as CurveControlTestReplicate)!.Judgment.Should().NotBe(CurveControlJudgment.NotDefined);
        (cc.Replicates![1] as CurveControlTestReplicate)!.Judgment.Should().NotBe(CurveControlJudgment.NotDefined);
    }

    private static CurveControlJudgment GetExpectedJudgement(double actualConcentration, double expectedConcentration, double expectedSD, double innerSDLimit, double outerSDLimit)
    {
        if (actualConcentration > expectedConcentration + (expectedSD * outerSDLimit))
        {
            return CurveControlJudgment.High;
        }
        if (actualConcentration > expectedConcentration + (expectedSD * innerSDLimit))
        {
            return CurveControlJudgment.FlaggedHigh;
        }
        if (actualConcentration < expectedConcentration - (expectedSD * outerSDLimit))
        {
            return CurveControlJudgment.Low;
        }
        if (actualConcentration < expectedConcentration - (expectedSD * innerSDLimit))
        {
            return CurveControlJudgment.FlaggedLow;
        }

        return CurveControlJudgment.OK;
    }

    private static void CheckCurveControls(CurveControlTestResult? cc, CurveControlJudgment judgmentSummary, CurveControlJudgment? judgmentReplicate1 = null, CurveControlJudgment? judgmentReplicate2 = null)
    {
        cc.Should().NotBeNull();
        if (cc != null)
        {
            cc.JudgmentSummary.Should().Be(judgmentSummary);

            if (cc.Replicates![0] is CurveControlTestReplicate replicate1 && judgmentReplicate1.HasValue)
            {
                replicate1.Judgment.Should().Be(judgmentReplicate1);
            }
            if (cc.Replicates![1] is CurveControlTestReplicate replicate2 && judgmentReplicate2.HasValue)
            {
                replicate2.Judgment.Should().Be(judgmentReplicate2);
            }
        }
    }

    private static List<CurveControlTestResult> PrepareCurveControls(double concentrationReplicate1, double concentrationReplicate2, bool lowRu = false, ConcentrationStatus concentrationStatus1 = ConcentrationStatus.Ok, ConcentrationStatus concentrationStatus2 = ConcentrationStatus.Ok)
    {
        return new List<CurveControlTestResult>()
        {
            new()
            {
                CurveControlPointName = "CC-1",
                InstrumentDilutionFactor = 1,
                JudgmentSummary = CurveControlJudgment.NotDefined,
                GroupSize = 1,
                MeanConcentration = 1,
                ExpectedConcentration = 1,
                NumberOfReplicates = 1,
                MethodName = "sIgE",
                PreDilutionFactor = 1,
                TestName = "a_IgE",
                Replicates = new List<TestReplicate>()
                {
                    new CurveControlTestReplicate()
                    {
                        Concentration = concentrationReplicate1,
                        ConcentrationStatus = concentrationStatus1,
                        IsLowResponse = lowRu,
                        Judgment = CurveControlJudgment.NotDefined,
                    },
                    new CurveControlTestReplicate()
                    {
                        Concentration = concentrationReplicate2,
                        ConcentrationStatus = concentrationStatus2,
                        IsLowResponse = lowRu,
                        Judgment = CurveControlJudgment.NotDefined,
                    },
                }
            }
        };
    }

    private static MethodInfo? GetHandleCurveControlTestOrdersMethod()
    {
        var type = typeof(CurveControlResultGroupReadyEventHandler);
        var methodInfo = type.GetMethod("HandleCurveControlTestOrders", BindingFlags.NonPublic | BindingFlags.Instance);
        return methodInfo;
    }


    /// <summary>
    /// Prepare method parameters
    /// </summary>
    private void PrepareMethodParameters(double expectedConcentration = 50.0)
    {
        var methodTests = new List<MethodTest>()
        {
            new()
            {
                TestName = "c1",
                ConcentrationFactor = 1,
                UseLotSpecificCode = false,
                IterativeAlfaBeta1Beta2Flag = false,
                AdjustmentForAlfaBeta1Beta2 = 1,
                HighestConcentrationToReport = null,
                LowerDetectionLimit = 0,
                ReportConcentrationBelowCurveFlag = false,
                ReportConcentrationAboveCurveFlag = false,
                DefaultCutOffConcentration = 0.35,
                DefaultReportCutOff = true,
                DefaultReportConcentration = true,
                DefaultReportClassId = 2,
                DefaultReportQuotient = QuotientReportMode.ReportAsFraction,
                DefaultReplicates = 1,

                Method = new Method()
                {
                    MethodName = "sIgE",
                    Calculation = new MethodCalculation()
                    {
                        CalculationType = CalculationType.Rodbard5Par,
                    },
                    Reporting = new MethodReporting()
                    {
                        DefaultMinConcentrationToReport = 0,
                        DefaultTextBelowCutOff = "Negative",
                        DefaultTextAboveCutOff = "Positive",
                    },
                    CalibrationCurve = new MethodCalibrationCurve()
                    {
                        QuotientIndex = 2,
                    },
                    CurveControls = new List<MethodCurveControl>()
                    {
                        new()
                        {
                            CurveControlName = "CC-1",
                            ExpectedConcentration = expectedConcentration,
                            InnerStdDevLimit = 1,
                            ExpectedStdDev = 5,
                            OuterStdDevLimit = 2,
                        }
                    },
                    ClassTableBelows = new List<MethodClassTableBelow>()
                    {
                        new() { ClassTableId = 1, DefaultTextBelowConcentration ="<1000", },
                        new() { ClassTableId = 2, DefaultTextBelowConcentration = "<10", },
                    },
                    ClassTables = new List<MethodClassTable>()
                    {
                        new() { ClassTableId= 1, ClassTableRowId = 1, Concentration = 1000, DefaultTextAboveConcentration = ">1000" },
                        new() { ClassTableId= 2, ClassTableRowId = 1, Concentration = 10, DefaultTextAboveConcentration = ">10" },
                        new() { ClassTableId= 2, ClassTableRowId = 2, Concentration = 20, DefaultTextAboveConcentration = ">20" },
                        new() { ClassTableId= 2, ClassTableRowId = 3, Concentration = 30, DefaultTextAboveConcentration = ">30" },
                    },
                },
            }
        };
        _parametersContextMock.Setup(db => db.MethodTests).ReturnsDbSet(methodTests);

        var methods = methodTests.Select(x => x.Method).ToList();
        _parametersContextMock.Setup(db => db.Methods).ReturnsDbSet(methods);
    }
}
