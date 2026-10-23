using System.Reflection;
using MediatR;
using Microsoft.Extensions.Options;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Services;
using Pharos.Server.Result.Application.Events.TestResultReady;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Mapping;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Services.ResultCalculationService;

public class QualityControlJudgmentTests
{
    private readonly Mock<IResultCalculationService> _resultServiceMock = new();
    private readonly Mock<IResultDbContext> _dbContextMock = new();
    private readonly Mock<IParametersDbContext> _parametersContextMock = new();
    private readonly QualityControlResultGroupReadyEventHandler _sutInstance;
    private readonly MethodInfo? _sut;

    public QualityControlJudgmentTests()
    {
        var methodsService = new MethodsService(_parametersContextMock.Object, new Mock<IAuthorizationService>().Object);

        _sutInstance = new QualityControlResultGroupReadyEventHandler(
            _dbContextMock.Object,
            methodsService,
            _resultServiceMock.Object,
            Mock.Of<IOptionsSnapshot<LisSettings>>(),
            Mock.Of<IMediator>(),
            new LisTestResultMapper(Mock.Of<IInstrumentManager>(), Mock.Of<IMethodsService>()));
        _sut = GetHandleQualityControlTestOrdersMethod();
    }

    [Theory]
    [InlineData(99.0, 110.0, false, false, null, null, null, 100.0, 111.0, 89.0, QualityControlType.Concentration, QualityControlJudgement.HighCv)] // Above limit
    [InlineData(99.0, 110.0, false, false, null, null, null, 100.0, 110.0, 90.0, QualityControlType.Concentration, QualityControlJudgement.Ok)]     // Equal to limit
    [InlineData(99.0, 110.0, false, false, null, null, null, 100.0, 111.0, 89.0, QualityControlType.Qualitative, QualityControlJudgement.HighCv)]
    [InlineData(99.0, 110.0, false, false, null, null, null, 100.0, 111.0, 89.0, QualityControlType.Quotient, QualityControlJudgement.HighCv)]
    [InlineData(99.0, 110.0, false, false, null, null, null, 100.0, 111.0, 89.0, QualityControlType.DilutionControl, QualityControlJudgement.HighCv)]
    [InlineData(1.0, 1000.0, false, false, 105.0, 5.0, 2.0, 100.0, 111.0, 89.0, QualityControlType.Concentration, QualityControlJudgement.HighCv)]
    [InlineData(1.0, 1000.0, false, false, 105.0, 5.0, 2.0, 100.0, 111.0, 89.0, QualityControlType.Qualitative, QualityControlJudgement.HighCv)]
    [InlineData(1.0, 1000.0, false, false, 105.0, 5.0, 2.0, 100.0, 111.0, 89.0, QualityControlType.Quotient, QualityControlJudgement.HighCv)]
    [InlineData(1.0, 1000.0, false, false, 105.0, 5.0, 2.0, 100.0, 111.0, 89.0, QualityControlType.DilutionControl, QualityControlJudgement.HighCv)]
    [Trait("ModuleRequirement", "MR_4013")]
    public async Task QualityControlJudgement_HighCV(
        double? minConcentration,
        double? maxConcentration,
        bool excludeMin,
        bool excludeMax,
        double? expectedConcentration,
        double? expectedStdDev,
        double? stdDevLimit,
        double meanConcentration,
        double concentrationReplicate1,
        double concentrationReplicate2,
        QualityControlType qualityControlType,
        QualityControlJudgement qcJudgement)
    {
        PrepareMethodParameters();
        var qc = PrepareQualityControl(minConcentration, maxConcentration, excludeMin, excludeMax, expectedConcentration, expectedStdDev, stdDevLimit, meanConcentration, concentrationReplicate1, concentrationReplicate2, qualityControlType);

        _sut.Should().NotBeNull();
        var invocationResult = _sut?.Invoke(_sutInstance, [qc, TestContext.Current.CancellationToken]);
        invocationResult.Should().BeAssignableTo<Task>();
        await (Task)invocationResult!;

        qc.Judgment.Should().Be(qcJudgement);
    }

    [Theory]
    [InlineData(99.0, 110.0, false, false, null, null, null, 100.0, 100.0, 100.0, QualityControlType.Concentration, QualityControlJudgement.Ok)]
    [InlineData(99.0, 110.0, false, false, null, null, null, 99.0, 99, 99.0, QualityControlType.Concentration, QualityControlJudgement.Ok)]
    [InlineData(99.0, 110.0, false, false, null, null, null, 110.0, 110.0, 110.0, QualityControlType.Concentration, QualityControlJudgement.Ok)]
    [InlineData(99.0, 110.0, true, true, null, null, null, 99.0, 99, 99.0, QualityControlType.Concentration, QualityControlJudgement.Low)]
    [InlineData(99.0, 110.0, true, true, null, null, null, 110.0, 110.0, 110.0, QualityControlType.Concentration, QualityControlJudgement.High)]
    [InlineData(99.0, 110.0, false, false, null, null, null, 111.0, 112.0, 110.0, QualityControlType.Concentration, QualityControlJudgement.High)]
    [InlineData(99.0, 110.0, false, false, null, null, null, 98.0, 97.0, 99.0, QualityControlType.Concentration, QualityControlJudgement.Low)]
    [InlineData(99.0, 110.0, false, false, null, null, null, 100.0, 100.0, 100.0, QualityControlType.Qualitative, QualityControlJudgement.Ok)]
    [InlineData(99.0, 110.0, false, false, null, null, null, 111.0, 112.0, 110.0, QualityControlType.Qualitative, QualityControlJudgement.High)]
    [InlineData(99.0, 110.0, false, false, null, null, null, 98.0, 97.0, 99.0, QualityControlType.Qualitative, QualityControlJudgement.Low)]
    [InlineData(99.0, 110.0, false, false, null, null, null, 100.0, 100.0, 100.0, QualityControlType.Quotient, QualityControlJudgement.Ok)]
    [InlineData(99.0, 110.0, false, false, null, null, null, 111.0, 112.0, 110.0, QualityControlType.Quotient, QualityControlJudgement.High)]
    [InlineData(99.0, 110.0, false, false, null, null, null, 98.0, 97.0, 99.0, QualityControlType.Quotient, QualityControlJudgement.Low)]
    [InlineData(99.0, 110.0, false, false, null, null, null, 100.0, 100.0, 100.0, QualityControlType.DilutionControl, QualityControlJudgement.Ok)]
    [InlineData(99.0, 110.0, false, false, null, null, null, 111.0, 112.0, 110.0, QualityControlType.DilutionControl, QualityControlJudgement.High)]
    [InlineData(99.0, 110.0, false, false, null, null, null, 98.0, 97.0, 99.0, QualityControlType.DilutionControl, QualityControlJudgement.Low)]
    [Trait("ModuleRequirement", "MR_4013")]
    public async Task QualityControlJudgement_MinMax(
        double? minConcentration,
        double? maxConcentration,
        bool excludeMin,
        bool excludeMax,
        double? expectedConcentration,
        double? expectedStdDev,
        double? stdDevLimit,
        double meanConcentration,
        double concentrationReplicate1,
        double concentrationReplicate2,
        QualityControlType qualityControlType,
        QualityControlJudgement qcJudgement)
    {
        PrepareMethodParameters();
        var qc = PrepareQualityControl(minConcentration, maxConcentration, excludeMin, excludeMax, expectedConcentration, expectedStdDev, stdDevLimit, meanConcentration, concentrationReplicate1, concentrationReplicate2, qualityControlType);

        _sut.Should().NotBeNull();
        var invocationResult = _sut?.Invoke(_sutInstance, [qc, TestContext.Current.CancellationToken]);
        invocationResult.Should().BeAssignableTo<Task>();
        await (Task)invocationResult!;

        qc.Judgment.Should().Be(qcJudgement);
    }

    [Theory]
    [InlineData(1.0, 1000.0, 105.0, 5.0, 2.0, 100.0, 100.0, 100.0, QualityControlType.Concentration, QualityControlJudgement.Ok)]
    [InlineData(1.0, 1000.0, 105.0, 5.0, 2.0, 121.0, 122.0, 120.0, QualityControlType.Concentration, QualityControlJudgement.High)]
    [InlineData(1.0, 1000.0, 105.0, 5.0, 2.0, 88.0, 87.0, 89.0, QualityControlType.Concentration, QualityControlJudgement.Low)]
    [InlineData(1.0, 1000.0, 105.0, 5.0, 2.0, 100.0, 100.0, 100.0, QualityControlType.Qualitative, QualityControlJudgement.Ok)]
    [InlineData(1.0, 1000.0, 105.0, 5.0, 2.0, 121.0, 122.0, 120.0, QualityControlType.Qualitative, QualityControlJudgement.High)]
    [InlineData(1.0, 1000.0, 105.0, 5.0, 2.0, 88.0, 87.0, 89.0, QualityControlType.Qualitative, QualityControlJudgement.Low)]
    [InlineData(1.0, 1000.0, 105.0, 5.0, 2.0, 100.0, 100.0, 100.0, QualityControlType.Quotient, QualityControlJudgement.Ok)]
    [InlineData(1.0, 1000.0, 105.0, 5.0, 2.0, 121.0, 122.0, 120.0, QualityControlType.Quotient, QualityControlJudgement.High)]
    [InlineData(1.0, 1000.0, 105.0, 5.0, 2.0, 88.0, 87.0, 89.0, QualityControlType.Quotient, QualityControlJudgement.Low)]
    [InlineData(1.0, 1000.0, 105.0, 5.0, 2.0, 100.0, 100.0, 100.0, QualityControlType.DilutionControl, QualityControlJudgement.Ok)]
    [InlineData(1.0, 1000.0, 105.0, 5.0, 2.0, 121.0, 122.0, 120.0, QualityControlType.DilutionControl, QualityControlJudgement.High)]
    [InlineData(1.0, 1000.0, 105.0, 5.0, 2.0, 88.0, 87.0, 89.0, QualityControlType.DilutionControl, QualityControlJudgement.Low)]
    [Trait("ModuleRequirement", "MR_4013")]
    // ReSharper disable once IdentifierTypo
#pragma warning disable VSSpell001
    public async Task QualityControlJudgement_RiliBAK(
#pragma warning restore VSSpell001
        double? minConcentration,
        double? maxConcentration,
        double? expectedConcentration,
        double? expectedStdDev,
        double? stdDevLimit,
        double meanConcentration,
        double concentrationReplicate1,
        double concentrationReplicate2,
        QualityControlType qualityControlType,
        QualityControlJudgement qcJudgement)
    {
        PrepareMethodParameters();
        var qc = PrepareQualityControl(minConcentration, maxConcentration, false, false, expectedConcentration, expectedStdDev, stdDevLimit, meanConcentration, concentrationReplicate1, concentrationReplicate2, qualityControlType);

        _sut.Should().NotBeNull();
        var invocationResult = _sut?.Invoke(_sutInstance, [qc, TestContext.Current.CancellationToken]);
        invocationResult.Should().BeAssignableTo<Task>();
        await (Task)invocationResult!;

        qc.Judgment.Should().Be(qcJudgement);
    }

    [Theory]
    [InlineData(null, null, false, false, null, null, null, 100.0, 100.0, 100.0, QualityControlType.Concentration, QualityControlJudgement.Ok)] // No limits + CV OK
    [InlineData(null, null, false, false, null, null, null, 100.0, 111.0, 89.0, QualityControlType.Concentration, QualityControlJudgement.HighCv)] // No limits + High CV
    [Trait("ModuleRequirement", "MR_4013")]
    public async Task QualityControlJudgement_NoLimitsDefined(
        double? minConcentration,
        double? maxConcentration,
        bool excludeMin,
        bool excludeMax,
        double? expectedConcentration,
        double? expectedStdDev,
        double? stdDevLimit,
        double meanConcentration,
        double concentrationReplicate1,
        double concentrationReplicate2,
        QualityControlType qualityControlType,
        QualityControlJudgement qcJudgement)
    {
        PrepareMethodParameters();
        var qc = PrepareQualityControl(minConcentration, maxConcentration, excludeMin, excludeMax, expectedConcentration, expectedStdDev, stdDevLimit, meanConcentration, concentrationReplicate1, concentrationReplicate2, qualityControlType);

        _sut.Should().NotBeNull();
        var invocationResult = _sut?.Invoke(_sutInstance, [qc, TestContext.Current.CancellationToken]);
        invocationResult.Should().BeAssignableTo<Task>();
        await (Task)invocationResult!;

        qc.Judgment.Should().Be(qcJudgement);
    }

    private static QualityControlTestResult PrepareQualityControl(double? minConcentration, double? maxConcentration, bool excludeMin, bool excludeMax, double? expectedConcentration, double? expectedStdDev, double? stdDevLimit, double meanConcentration, double resultReplicate1, double resultReplicate2, QualityControlType qualityControlType)
    {
        var useConcentration = qualityControlType is QualityControlType.Concentration or QualityControlType.Qualitative;
        var useQuotient = qualityControlType is QualityControlType.Quotient or QualityControlType.DilutionControl;

        return new QualityControlTestResult
        {
            QualityControlName = "f1 Pos",
            MethodName = "sIgE",
            TestName = "f1",
            QualityControlType = qualityControlType,
            GroupId = Guid.NewGuid(),

            ExpectedConcentration = expectedConcentration,
            ExpectedStdDev = expectedStdDev,
            StdDevLimit = stdDevLimit,

            MinValue = minConcentration,
            MaxValue = maxConcentration,
            ExcludeMinValue = excludeMin,
            ExcludeMaxValue = excludeMax,

            MeanConcentration = useConcentration ? meanConcentration : null,
            MeanQuotient = useQuotient ? meanConcentration / 100.0 : null,

            Replicates = new List<TestReplicate>
            {
                new QualityControlTestReplicate
                {
                    Concentration = useConcentration ? resultReplicate1 : null,
                    Quotient = useQuotient ? resultReplicate1 / 100.0: null
                },
                new QualityControlTestReplicate
                {
                    Concentration = useConcentration ? resultReplicate2 : null,
                    Quotient = useQuotient ? resultReplicate2 / 100.0 : null
                }
            }
        };
    }

    private static MethodInfo? GetHandleQualityControlTestOrdersMethod()
    {
        var type = typeof(QualityControlResultGroupReadyEventHandler);
        var methodInfo = type.GetMethod("CalculateJudgement", BindingFlags.NonPublic | BindingFlags.Instance);
        return methodInfo;
    }


    /// <summary>
    /// Prepare method parameters
    /// </summary>
    private void PrepareMethodParameters()
    {
        var methodTests = new List<MethodTest>
        {
            new()
            {
                TestName = "f1",
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

                Method = new Method
                {
                    MethodName = "sIgE",
                    Calculation = new MethodCalculation
                    {
                        CalculationType = CalculationType.Rodbard5Par
                    },
                    Reporting = new MethodReporting
                    {
                        DefaultMinConcentrationToReport = 0,
                        DefaultTextBelowCutOff = "Negative",
                        DefaultTextAboveCutOff = "Positive"
                    },
                    CalibrationCurve = new MethodCalibrationCurve
                    {
                        QuotientIndex = 2
                    },
                    CurveControls = new List<MethodCurveControl>
                    {
                        new()
                        {
                            CurveControlName = "CC-1",
                            ExpectedConcentration = 50.0,
                            InnerStdDevLimit = 1,
                            ExpectedStdDev = 5,
                            OuterStdDevLimit = 2
                        }
                    },
                    ClassTableBelows = new List<MethodClassTableBelow>
                    {
                        new() { ClassTableId = 1, DefaultTextBelowConcentration ="<1000", },
                        new() { ClassTableId = 2, DefaultTextBelowConcentration = "<10" }
                    },
                    ClassTables = new List<MethodClassTable>
                    {
                        new() { ClassTableId= 1, ClassTableRowId = 1, Concentration = 1000, DefaultTextAboveConcentration = ">1000" },
                        new() { ClassTableId= 2, ClassTableRowId = 1, Concentration = 10, DefaultTextAboveConcentration = ">10" },
                        new() { ClassTableId= 2, ClassTableRowId = 2, Concentration = 20, DefaultTextAboveConcentration = ">20" },
                        new() { ClassTableId= 2, ClassTableRowId = 3, Concentration = 30, DefaultTextAboveConcentration = ">30" }
                    },
                },
            }
        };
        _parametersContextMock.Setup(db => db.MethodTests).ReturnsDbSet(methodTests);

        var methods = methodTests.Select(x => x.Method).ToList();
        _parametersContextMock.Setup(db => db.Methods).ReturnsDbSet(methods);

        var qualityControls = new List<PhadiaQualityControlTest>
        {
            new()
            {
                QualityControl = new PhadiaQualityControl { Name = "f1 Pos" },
                ReplicateCvLimit = 10
            }
        };

        _parametersContextMock.Setup(db => db.PhadiaQualityControlTests).ReturnsDbSet(qualityControls);

        foreach (var qc in qualityControls)
        {
            qc.Test = methodTests.First();
        }
    }
}
