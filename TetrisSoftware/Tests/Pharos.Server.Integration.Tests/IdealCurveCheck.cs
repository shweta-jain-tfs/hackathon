using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Application.Settings.Result;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Services;
using Pharos.Server.Result.Application.Services.ResultCalculationService;
using Pharos.Server.Result.CurveFit;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class IdealCurveCheckTests(IntegrationTestsWaf factory) : IntegrationTestBase(factory)
{
    private readonly ICurveFitterApi _curveFitterApi = new CurveFitterApi();
    private readonly Mock<ILogger<ResultCalculationService>> _loggerMock = new();
    private readonly Guid _calibratorGroupId = Guid.NewGuid();
    private const string _instrumentNumber = "SN001";

    [Theory]
    [InlineData(new[] { 15.0, 16, 171, 172, 320, 322, 1480, 1486, 6352, 6358, 18519, 18619 }, CurveJudgment.OK, "A good curve")]
    [InlineData(new[] { 15.0, 16, 90, 172, 320, 322, 1480, 1486, 6352, 6358, 18519, 18619 }, CurveJudgment.OK, "1 replicate outside response limits")]
    [InlineData(new[] { 15.0, 16, 90, 172, 320, 322, 1480, 1486, 6352, 6358, 18519, 28000 }, CurveJudgment.Flagged, "2 replicate outside response limits")]
    [InlineData(new[] { 15.0, 16, 90, 172, 320, 90, 1480, 1486, 6352, 6358, 18519, 28000 }, CurveJudgment.Flagged, "3 replicate outside response limits")]
    [InlineData(new[] { 15.0, 16, 90, 90, 320, 90, 1480, 1486, 6352, 6358, 18519, 28000 }, CurveJudgment.NotOK, "4 replicate outside response limits")]
    [InlineData(new[] { 15.0, 16, 171, 172, 320, 322, 1480, 1486, 6000, 8000, 18519, 18619 }, CurveJudgment.OK, "1 replicates outside log response limits")]
    [InlineData(new[] { 15.0, 16, 171, 172, 320, 322, 1480, 1486, 5000, 8000, 18519, 18619 }, CurveJudgment.Flagged, "2 replicates outside log response limits")]
    [InlineData(new[] { 15.0, 16, 171, 172, 320, 322, 1480, 2486, 5000, 8000, 18519, 18619 }, CurveJudgment.Flagged, "3 replicates outside log response limits")]
    [InlineData(new[] { 15.0, 16, 171, 172, 200, 800, 1480, 2486, 5000, 8000, 18519, 18619 }, CurveJudgment.NotOK, "4 replicates outside log response limits")]
    [InlineData(new[] { 15.0, 16, 171, 172, 320, 322, 1480, 1486, 6000, 8000, 18519, 18619 }, CurveJudgment.OK, "1 replicates having deviations", true)]
    [InlineData(new[] { 15.0, 16, 171, 172, 320, 322, 1200, 1500, 6000, 8000, 18519, 18619 }, CurveJudgment.Flagged, "2 replicates having deviations", true)]
    [InlineData(new[] { 15.0, 16, 171, 172, 320, 322, 1200, 1500, 6000, 8000, 15000, 19000 }, CurveJudgment.Flagged, "3 replicates having deviations", true)]
    [InlineData(new[] { 15.0, 16, 171, 172, 200, 400, 1200, 1500, 6000, 8000, 15000, 19000 }, CurveJudgment.NotOK, "4 replicates having deviations", true)]
    [InlineData(new[] { 10.0, 50, 100, 200, 300, 500, 1200, 1500, 6000, 8000, 15000, 19000 }, CurveJudgment.OK, "All points have bad precision, and that is OK (same as Prime)", true)]
    [InlineData(new[] { 15.0, 16, 171, 172, 320, 322, 1480, 1486, 6352, 172, 172, 18619 }, CurveJudgment.NotOK, "Number of excluded replicates exceed method dependent limit", false, 1)]
    [InlineData(new[] { 20021.0, 20021, 171, 172, 320, 322, 1480, 1486, 6352, 6358, 18519, 18619 }, CurveJudgment.NotOK, "All replicates of the lowest calibration point are eliminated")]
    [Trait("ModuleRequirement", "MR_3507")]
    public async Task IdealCurveCheckTest(double[] response, CurveJudgment curveJudgment, string comment, bool logResponseLimitOk = false, int maxBadReplicates = 3)
    {
        // Setup services
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var parametersContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var methodService = ServiceProvider.GetRequiredService<IMethodsService>();
        var barcodeServiceMock = new Mock<IReagentBarcodeService>();
        var resultSettingMock = new Mock<IOptionsSnapshot<ResultSettings>>();
        var resultFormatter = new ResultFormatter(methodService, barcodeServiceMock.Object, resultSettingMock.Object);

        // Prepare data
        await CreateCurves(dbContext);
        var testCaseData = new TestCaseData(response, curveJudgment, comment, logResponseLimitOk, maxBadReplicates);
        var calibrators = await CreateCalibrators(dbContext, testCaseData);
        await CreateMethods(parametersContext, testCaseData);

        // Run test
        var sut = new ResultCalculationService(dbContext, _curveFitterApi, resultFormatter, _loggerMock.Object, methodService, barcodeServiceMock.Object);
        await sut.CheckAndFitCurve(_instrumentNumber, _calibratorGroupId, calibrators, TestContext.Current.CancellationToken);

        // Check results
        await CheckCurve(dbContext, testCaseData);
    }

    [Theory]
    [InlineData(new[] { 15.0, 16, 171, 172, 320, 322, 1480, 1486, 6352, 6358, 18519, 18619 }, CurveJudgment.OK, "A good curve")]
    [InlineData(new[] { 15.0, 4.0, 171, 172, 320, 322, 1480, 1486, 6352, 6358, 18519, 18619 }, CurveJudgment.NotOK, "Low RU")]
    [Trait("ModuleRequirement", "MR_3508")]
    public async Task JudgementOfCalibrationCurve_LowRu(double[] response, CurveJudgment curveJudgment, string comment)
    {
        // Setup services
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var parametersContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var methodService = ServiceProvider.GetRequiredService<IMethodsService>();
        var barcodeServiceMock = new Mock<IReagentBarcodeService>();
        var resultSettingMock = new Mock<IOptionsSnapshot<ResultSettings>>();
        var resultFormatter = new ResultFormatter(methodService, barcodeServiceMock.Object, resultSettingMock.Object);

        // Prepare data
        await CreateCurves(dbContext);
        var testCaseData = new TestCaseData(response, curveJudgment, comment);
        var calibrators = await CreateCalibrators(dbContext, testCaseData);
        await CreateMethods(parametersContext, testCaseData);

        // Run test
        var sut = new ResultCalculationService(dbContext, _curveFitterApi, resultFormatter, _loggerMock.Object, methodService, barcodeServiceMock.Object);
        await sut.CheckAndFitCurve(_instrumentNumber, _calibratorGroupId, calibrators, TestContext.Current.CancellationToken);

        // Check results
        await CheckCurve(dbContext, testCaseData);
    }

    [Theory]
    [InlineData(new[] { 15.0, 16, 171, 172, 320, 322, 1480, 1486, 6352, 6358, 18519, 18619 }, CurveJudgment.OK, "A good curve")]
    [InlineData(new[] { 15.0, 16, 171, 172, 320, 322, 1480, 1486, 6352, 6358, 18519, 18619 }, CurveJudgment.NotOK, "Replicate aborted", 11)]
    [Trait("ModuleRequirement", "MR_3508")]
    public async Task JudgementOfCalibrationCurve_ReplicateAborted(double[] response, CurveJudgment curveJudgment, string comment, int replicateAborted = -1)
    {
        // Setup services
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var parametersContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var methodService = ServiceProvider.GetRequiredService<IMethodsService>();
        var barcodeServiceMock = new Mock<IReagentBarcodeService>();
        var resultSettingMock = new Mock<IOptionsSnapshot<ResultSettings>>();
        var resultFormatter = new ResultFormatter(methodService, barcodeServiceMock.Object, resultSettingMock.Object);

        // Prepare data
        await CreateCurves(dbContext);
        var testCaseData = new TestCaseData(response, curveJudgment, comment, replicateAborted: replicateAborted);
        var calibrators = await CreateCalibrators(dbContext, testCaseData);
        await CreateMethods(parametersContext, testCaseData);

        // Run test
        var sut = new ResultCalculationService(dbContext, _curveFitterApi, resultFormatter, _loggerMock.Object, methodService, barcodeServiceMock.Object);
        await sut.CheckAndFitCurve(_instrumentNumber, _calibratorGroupId, calibrators, TestContext.Current.CancellationToken);

        // Check results
        await CheckCurve(dbContext, testCaseData);
    }

    [Theory]
    [InlineData(new[] { 15.0, 16, 171, 172, 320, 322, 1480, 1486, 6352, 6358, 18519, 18619 }, CurveJudgment.OK, "A good curve, no correlation criteria set (default)", 0.0)]
    [InlineData(new[] { 15.0, 16, 171, 172, 320, 322, 1480, 1486, 6352, 6358, 18519, 18619 }, CurveJudgment.OK, "A good curve, correlation ok", 0.9)]
    [InlineData(new[] { 15.0, 16, 171, 172, 320, 322, 1480, 1486, 6352, 6358, 18519, 18619 }, CurveJudgment.NotOK, "Coefficient of correlation too low", 1.0)]
    [Trait("ModuleRequirement", "MR_3508")]
    public async Task JudgementOfCalibrationCurve_MinCoefficientOfCorrelation(double[] response, CurveJudgment curveJudgment, string comment, double minCoefficientOfCorrelation)
    {
        // Setup services
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var parametersContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var methodService = ServiceProvider.GetRequiredService<IMethodsService>();
        var barcodeServiceMock = new Mock<IReagentBarcodeService>();
        var resultSettingMock = new Mock<IOptionsSnapshot<ResultSettings>>();
        var resultFormatter = new ResultFormatter(methodService, barcodeServiceMock.Object, resultSettingMock.Object);

        // Prepare data
        await CreateCurves(dbContext);
        var testCaseData = new TestCaseData(response, curveJudgment, comment, minCoefficientOfCorrelation: minCoefficientOfCorrelation);
        var calibrators = await CreateCalibrators(dbContext, testCaseData);
        await CreateMethods(parametersContext, testCaseData);

        // Run test
        var sut = new ResultCalculationService(dbContext, _curveFitterApi, resultFormatter, _loggerMock.Object, methodService, barcodeServiceMock.Object);
        await sut.CheckAndFitCurve(_instrumentNumber, _calibratorGroupId, calibrators, TestContext.Current.CancellationToken);

        // Check results
        await CheckCurve(dbContext, testCaseData);
    }

    private async Task CreateCurves(IResultDbContext dbContext)
    {
        var curve = new Curve
        {
            MethodName = "sIgE",
            CalculationType = CalculationType.Rodbard5Par,
            InstrumentNumber = _instrumentNumber,
            GroupId = _calibratorGroupId,
            InstrumentRunId = Guid.NewGuid()
        };

        await dbContext.Curves.AddAsync(curve);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<List<CalibratorTestResult>> CreateCalibrators(IResultDbContext dbContext, TestCaseData testCaseData)
    {
        var analyticalRun = new AnalyticalRun()
        {
            MethodName = "sIgE",
            InstrumentNumber = _instrumentNumber,
            InstrumentAnalyticalRunId = Guid.NewGuid()
        };
        await dbContext.AnalyticalRuns.AddAsync(analyticalRun);

        var calibratorTestResults = new List<CalibratorTestResult>()
        {
            new()
            {
                MethodName = "sIgE",
                TestName = "a_IgE",
                ExpectedConcentration = 0.001,
                InstrumentNumber = _instrumentNumber,
                AnalyticalRun = analyticalRun,
                Replicates = new List<TestReplicate>()
                {
                    new CalibratorTestReplicate() { Response = testCaseData.Response[0], InstrumentReplicateId = Guid.NewGuid(), IsLowResponse = testCaseData.Response[0] < 5.0, IsAborted = testCaseData.ReplicateAborted == 0},
                    new CalibratorTestReplicate() { Response = testCaseData.Response[1], InstrumentReplicateId = Guid.NewGuid(), IsLowResponse = testCaseData.Response[1] < 5.0, IsAborted = testCaseData.ReplicateAborted == 1 },
                }
            },
            new()
            {
                MethodName = "sIgE",
                TestName = "a_IgE",
                ExpectedConcentration = 0.35,
                InstrumentNumber = _instrumentNumber,
                AnalyticalRun = analyticalRun,
                Replicates = new List<TestReplicate>()
                {
                    new CalibratorTestReplicate() { Response = testCaseData.Response[2], InstrumentReplicateId = Guid.NewGuid(), IsLowResponse = testCaseData.Response[2] < 5.0, IsAborted = testCaseData.ReplicateAborted == 2 },
                    new CalibratorTestReplicate() { Response = testCaseData.Response[3], InstrumentReplicateId = Guid.NewGuid() , IsLowResponse = testCaseData.Response[3] < 5.0, IsAborted = testCaseData.ReplicateAborted == 3 },
                }
            },
            new()
            {
                MethodName = "sIgE",
                TestName = "a_IgE",
                ExpectedConcentration = 0.7,
                InstrumentNumber = _instrumentNumber,
                AnalyticalRun = analyticalRun,
                Replicates = new List<TestReplicate>()
                {
                    new CalibratorTestReplicate() { Response = testCaseData.Response[4], InstrumentReplicateId = Guid.NewGuid() , IsLowResponse = testCaseData.Response[4] < 5.0, IsAborted = testCaseData.ReplicateAborted == 4 },
                    new CalibratorTestReplicate() { Response = testCaseData.Response[5], InstrumentReplicateId = Guid.NewGuid() , IsLowResponse = testCaseData.Response[5] < 5.0, IsAborted = testCaseData.ReplicateAborted == 5 },
                }
            },
            new()
            {
                MethodName = "sIgE",
                TestName = "a_IgE",
                ExpectedConcentration = 3.5,
                InstrumentNumber = _instrumentNumber,
                AnalyticalRun = analyticalRun,
                Replicates = new List<TestReplicate>()
                {
                    new CalibratorTestReplicate() { Response = testCaseData.Response[6], InstrumentReplicateId = Guid.NewGuid() , IsLowResponse = testCaseData.Response[6] < 5.0, IsAborted = testCaseData.ReplicateAborted == 6 },
                    new CalibratorTestReplicate() { Response = testCaseData.Response[7], InstrumentReplicateId = Guid.NewGuid() , IsLowResponse = testCaseData.Response[7] < 5.0, IsAborted = testCaseData.ReplicateAborted == 7},
                }
            },
            new()
            {
                MethodName = "sIgE",
                TestName = "a_IgE",
                ExpectedConcentration = 17.5,
                InstrumentNumber = _instrumentNumber,
                AnalyticalRun = analyticalRun,
                Replicates = new List<TestReplicate>()
                {
                    new CalibratorTestReplicate() { Response = testCaseData.Response[8], InstrumentReplicateId = Guid.NewGuid() , IsLowResponse = testCaseData.Response[8] < 5.0, IsAborted = testCaseData.ReplicateAborted == 8 },
                    new CalibratorTestReplicate() { Response = testCaseData.Response[9], InstrumentReplicateId = Guid.NewGuid() , IsLowResponse = testCaseData.Response[9] < 5.0, IsAborted = testCaseData.ReplicateAborted == 9 },
                }
            },
            new()
            {
                MethodName = "sIgE",
                TestName = "a_IgE",
                ExpectedConcentration = 100,
                InstrumentNumber = _instrumentNumber,
                AnalyticalRun = analyticalRun,
                Replicates = new List<TestReplicate>()
                {
                    new CalibratorTestReplicate() { Response = testCaseData.Response[10], InstrumentReplicateId = Guid.NewGuid() , IsLowResponse = testCaseData.Response[10] < 5.0, IsAborted = testCaseData.ReplicateAborted == 10 },
                    new CalibratorTestReplicate() { Response = testCaseData.Response[11], InstrumentReplicateId = Guid.NewGuid() , IsLowResponse = testCaseData.Response[11] < 5.0, IsAborted = testCaseData.ReplicateAborted == 11 },
                }
            }
        };

        await dbContext.CalibratorTestResults.AddRangeAsync(calibratorTestResults);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return calibratorTestResults;
    }

    private static async Task CreateMethods(IParametersDbContext parametersContext, TestCaseData testCaseData)
    {
        var method = MethodSpecificIgE(testCaseData);

        await parametersContext.Methods.AddAsync(method);

        await parametersContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static Method MethodSpecificIgE(TestCaseData testCaseData)
    {
        var test = new MethodTest()
        {
            Active = true,
            ConcentrationFactor = 1.0,
            DefaultConcentrationUnit = new MethodConcentrationUnit()
            {
                ConcentrationUnit = "kU/l",
                UnitId = 1
            },
            DefaultLabTestName = "a_IgE",
            TestName = "a_IgE",
            DefaultDilutionFactor = 1,
            DefaultDilutionMethod = DilutionMethod.PreDiluted,
            DefaultReplicates = 1,
            DefaultReportConcentration = true,
            DiluentIdentity = "Diluent",
            DefaultReportQuotient = QuotientReportMode.NoReport,
            FullTestName = "Total IgE",
            AdjustmentForAlfaBeta1Beta2 = 1.0,
            NumberOfDecimalsForConcentrationAbove100 = 0,
            NumberOfDecimalsForConcentrationBelow10 = 2,
            NumberOfDecimalsForConcentrationBetween10And100 = 1,
        };
        var method = new Method()
        {
            MethodName = "sIgE",
            FullMethodName = "ImmunoCAP Specific IgE 0-100",
            Calculation = new MethodCalculation
            {
                CalculationType = CalculationType.Rodbard5Par,
                CalibratorStartingPoint = 0,
                CalibratorsForLeastSquareCalc = 4,
                MinCoefficientOfCorrelation = testCaseData.MinCoefficientOfCorrelation,
                NormalWeight = NormalWeight.Predicted,
                RobustWeight = 0,
                PredictedVarianceA = 1.72783,
                PredictedVarianceB = 0.002021
            },
            CalibrationCurve = new MethodCalibrationCurve()
            {
                ExcludeLowestCalibratorFromMeanDiff = true,
                MaxBadReplicates = testCaseData.MaxBadReplicates,
                MethodTest = test,
                CalibrationPoints = new List<MethodCalibrationPoint>()
                {
                    new()
                    {
                        CalibratorName = "CAL-0.00",
                        ExpectedConcentration = 0.001,
                        Replicates = 2,
                        IdealResponse = 16,
                        StdDevLimit = 4.2,
                        ExpectedStdDev = 4,
                        LowerResponseLimit = 5,
                        LowerLogarithmicLimit = testCaseData.LogResponseLimitOk ? -1000.0 : -1.4,
                        StdDevPrecisionLimit = 3,
                        UpperLogarithmicLimit = testCaseData.LogResponseLimitOk ? 1000.0 : 0.871,
                        UpperResponseLimit = 20021,
                    },
                    new()
                    {
                        CalibratorName = "CAL-0.35",
                        ExpectedConcentration = 0.35,
                        Replicates = 2,
                        IdealResponse = 172,
                        StdDevLimit = 4.2,
                        ExpectedStdDev = 7,
                        LowerResponseLimit = 95,
                        LowerLogarithmicLimit = testCaseData.LogResponseLimitOk ? -1000.0 : -0.2,
                        StdDevPrecisionLimit = 3,
                        UpperLogarithmicLimit = testCaseData.LogResponseLimitOk ? 1000.0 : 0.2,
                        UpperResponseLimit = 20021,
                    },
                    new()
                    {
                        CalibratorName = "CAL-0.70",
                        ExpectedConcentration = 0.7,
                        Replicates = 2,
                        IdealResponse = 320,
                        StdDevLimit = 4.2,
                        ExpectedStdDev = 8,
                        LowerResponseLimit = 95,
                        LowerLogarithmicLimit = testCaseData.LogResponseLimitOk ? -1000.0 : -0.2,
                        StdDevPrecisionLimit = 3,
                        UpperLogarithmicLimit = testCaseData.LogResponseLimitOk ? 1000.0 : 0.2,
                        UpperResponseLimit = 20021,
                    },
                    new()
                    {
                        CalibratorName = "CAL-3.50",
                        ExpectedConcentration = 3.5,
                        Replicates = 2,
                        IdealResponse = 1480,
                        StdDevLimit = 4.2,
                        ExpectedStdDev = 34,
                        LowerResponseLimit = 95,
                        LowerLogarithmicLimit = testCaseData.LogResponseLimitOk ? -1000.0 : -0.2,
                        StdDevPrecisionLimit = 3,
                        UpperLogarithmicLimit = testCaseData.LogResponseLimitOk ? 1000.0 : 0.2,
                        UpperResponseLimit = 20021,
                    },
                    new()
                    {
                        CalibratorName = "CAL-17.5",
                        ExpectedConcentration = 17.5,
                        Replicates = 2,
                        IdealResponse = 6352,
                        StdDevLimit = 4.2,
                        ExpectedStdDev = 149,
                        LowerResponseLimit = 95,
                        LowerLogarithmicLimit = testCaseData.LogResponseLimitOk ? -1000.0 : -0.2,
                        StdDevPrecisionLimit = 3,
                        UpperLogarithmicLimit = testCaseData.LogResponseLimitOk ? 1000.0 : 0.2,
                        UpperResponseLimit = 20021,
                    },
                    new()
                    {
                        CalibratorName = "CAL-100",
                        ExpectedConcentration = 100,
                        Replicates = 2,
                        IdealResponse = 18619,
                        StdDevLimit = 4.2,
                        ExpectedStdDev = 374,
                        LowerResponseLimit = 95,
                        LowerLogarithmicLimit = testCaseData.LogResponseLimitOk ? -1000.0 : -0.2,
                        StdDevPrecisionLimit = 3,
                        UpperLogarithmicLimit = testCaseData.LogResponseLimitOk ? 1000.0 : 0.2,
                        UpperResponseLimit = 27929,
                    },
                }
            },
            AnalyticalRun = new MethodAnalyticalRun()
            {
                DefaultAllowMonthlyCalibration = true,
            },
            Presentation = new MethodPresentation()
            {
                XAxisScale = AxisScale.Log,
                XAxisUnit = "kU/l",
                XAxisText = "Concentration",
                YAxisScale = AxisScale.Log,
                YAxisText = "Response",
                YAxisUnit = "RU"
            },
            Reporting = new MethodReporting()
            {
                DefaultTextQuantitativeAbove = "> 100",
                DefaultTextQuantitativeBelow = "0.00",
                DefaultTextAboveCutOff = "Positive",
                DefaultTextBelowCutOff = "Negative",
                DefaultTextQuantitativeDoubtful = "Examine",
            },
            Tests = new List<MethodTest>()
            {
                test
            }
        };
        test.DefaultConcentrationUnit.Method = method;
        return method;
    }

    private static async Task CheckCurve(IResultDbContext dbContext, TestCaseData testCaseData)
    {
        var curve = await dbContext.Curves.FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        curve.Should().NotBeNull();
        curve.Judgment.Should().Be(testCaseData.CurveJudgment);
    }

    private class TestCaseData(
        double[] response, CurveJudgment curveJudgment, string comment,
        bool logResponseLimitOk = false, int maxBadReplicates = 3, int replicateAborted = -1, double minCoefficientOfCorrelation = 0.0)
    {
        public double[] Response { get; init; } = response;
        public CurveJudgment CurveJudgment { get; init; } = curveJudgment;
        public bool LogResponseLimitOk { get; init; } = logResponseLimitOk;
        public int MaxBadReplicates { get; init; } = maxBadReplicates;
        public int ReplicateAborted { get; } = replicateAborted;
        public double MinCoefficientOfCorrelation { get; } = minCoefficientOfCorrelation;

        // ReSharper disable once UnusedMember.Local
        public string Comment { get; init; } = comment;
    }

}
