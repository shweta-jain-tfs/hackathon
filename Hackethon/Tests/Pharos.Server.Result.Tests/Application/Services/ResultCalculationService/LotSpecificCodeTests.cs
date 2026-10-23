using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Result;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Services;
using Pharos.Server.Reagents.Application.Services.ReagentBarcodeService;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.CurveFit;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Services.ResultCalculationService;

public class LotSpecificCodeTests
{
    private readonly ResultCalculationService _sut;
    private readonly Mock<IResultDbContext> _dbContextMock = new();
    private readonly ICurveFitterApi _curveFitterApi = new CurveFitterApi();
    private readonly Mock<ILogger<ResultCalculationService>> _loggerMock = new();
    private readonly Mock<ILogger<ReagentBarcodeService>> _barcodeServiceLoggerMock = new();
    private readonly Mock<IParametersDbContext> _parametersDbContextMock = new();
    private readonly Mock<IOptionsSnapshot<ResultSettings>> _resultSettingMock = new();
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());

    public LotSpecificCodeTests()
    {
        var methodsService = new MethodsService(_parametersDbContextMock.Object, new Mock<IAuthorizationService>().Object);
        var barcodeService = new ReagentBarcodeService(new ArticlesService(_parametersDbContextMock.Object), _memoryCache, _barcodeServiceLoggerMock.Object);
        var resultFormatter = new ResultFormatter(methodsService, barcodeService, _resultSettingMock.Object);
        _sut = new ResultCalculationService(_dbContextMock.Object, _curveFitterApi, resultFormatter, _loggerMock.Object, methodsService, barcodeService);
    }

    [Theory]
    [InlineData(300, 3.8146168055858864, 3.7001783014183092)]
    [Trait("ModuleRequirement", "MR_3532")]
    public async Task LotSpecificCode_CalculateConcentration(double response, double concNoLotSpecificCode, double concLotSpecificCode)
    {
        var replicateList = PrepareSampleResults(response);
        var curveId = PrepareCalibrators(replicateList[0].TestResult.MethodName);
        PrepareMethodParameters();
        PrepareArticleParameters();

        SampleTestReplicate replicate;

        // No Lot Specific Code
        replicate = replicateList[0];
        await _sut.CalculateReplicateConcentration(replicate, curveId, replicate.TestResult.PreDilutionFactor, replicate.TestResult.InstrumentDilutionFactor,
            replicate.TestResult.MethodName, replicate.TestResult.TestName, replicate.TestResult.ResultType, TestContext.Current.CancellationToken);
        replicate.Concentration.Should().NotBeNull();
        if (replicate.Concentration.HasValue)
        {
            Math.Round(replicate.Concentration.Value, 5).Should().Be(Math.Round(concNoLotSpecificCode, 5));
        }

        // Lot Specific Code
        replicate = replicateList[1];
        await _sut.CalculateReplicateConcentration(replicate, curveId, replicate.TestResult.PreDilutionFactor, replicate.TestResult.InstrumentDilutionFactor,
            replicate.TestResult.MethodName, replicate.TestResult.TestName, replicate.TestResult.ResultType, TestContext.Current.CancellationToken);
        replicate.Concentration.Should().NotBeNull();
        if (replicate.Concentration.HasValue)
        {
            Math.Round(replicate.Concentration.Value, 5).Should().Be(Math.Round(concLotSpecificCode, 5));
        }
    }

    /// <summary>
    /// Prepare a sample test order replicate
    /// </summary>
    /// <param name="response">Response value</param>
    /// <returns>Sample test order replicate</returns>
    private static List<SampleTestReplicate> PrepareSampleResults(double response)
    {
        var list = new List<SampleTestReplicate>
        {
            new()
            {
                Response = response,
                CarrierBarcode = "BFAEX004K", // No Lot specific code
                TestResult = new SampleTestResult()
                {
                    TestName = "dn",
                    MethodName = "El-G",
                    InstrumentDilutionFactor = 10,
                    NumberOfReplicates = 1,
                    PreDilutionFactor = 1,
                },
            },
            new()
            {
                Response = response,
                CarrierBarcode = "BFAEX2P4K", // Lot specific code 0.970
                TestResult = new SampleTestResult()
                {
                    TestName = "dn",
                    MethodName = "El-G",
                    InstrumentDilutionFactor = 10,
                    NumberOfReplicates = 1,
                    PreDilutionFactor = 1,
                },
            }
        };

        return list;
    }

    /// <summary>
    /// Prepare calibrators and curve
    /// </summary>
    /// <param name="methodName">Curve to return curve id for</param>
    /// <returns>Curve ID</returns>
    private int PrepareCalibrators(string methodName)
    {
        var curves = ResultCalculationServiceTestsHelpers.PrepareCurves(_dbContextMock);
        var curve = curves.First(c => c.MethodName == methodName);

        var calibratorTestResults = new List<CalibratorTestResult>()
        {
            new()
            {
                MeanResponse = 33,
                ExpectedConcentration = 0,
            },
            new()
            {
                MeanResponse = 312,
                ExpectedConcentration = 4,
            },
            new()
            {
                MeanResponse = 707,
                ExpectedConcentration = 10,
            },
            new()
            {
                MeanResponse = 1382,
                ExpectedConcentration = 20,
            },
            new()
            {
                MeanResponse = 6189,
                ExpectedConcentration = 100,
            },
            new()
            {
                MeanResponse = 18375,
                ExpectedConcentration = 600,
            },
        };
        curve.TestResults = calibratorTestResults;
        _dbContextMock.Setup(db => db.CalibratorTestResults).ReturnsDbSet(calibratorTestResults);

        return curve.Id;
    }

    /// <summary>
    /// Prepare method parameters
    /// </summary>
    private void PrepareMethodParameters()
    {
        var methodTests = new List<MethodTest>()
        {
            new()
            {
                TestName = "dn",
                ConcentrationFactor = 0.1,
                UseLotSpecificCode = true,
                IterativeAlfaBeta1Beta2Flag = false,
                AdjustmentForAlfaBeta1Beta2 = 1,
                HighestConcentrationToReport = null,
                LowerDetectionLimit = 0,
                ReportConcentrationBelowCurveFlag = false,
                ReportConcentrationAboveCurveFlag = false,
                DefaultCutOffConcentration = 4,
                DefaultReportCutOff = false,
                DefaultReportConcentration = true,
                DefaultReportClassId = 0,
                DefaultReportQuotient = QuotientReportMode.NoReport,
                DefaultReplicates = 1,

                Method = new Method()
                {
                    MethodName = "El-G",
                    Technology = AssayTechnology.EliA,
                    Calculation = new MethodCalculation()
                    {
                        CalculationType = CalculationType.Rodbard4Par,
                    },
                    Reporting = new MethodReporting()
                    {
                        DefaultMinConcentrationToReport = 0,
                        DefaultTextBelowCutOff = "Negative",
                        DefaultTextAboveCutOff = "Positive",
                    },
                    CalibrationCurve = new MethodCalibrationCurve()
                    {
                        QuotientIndex = 1,
                        CalibrationPoints = new List<MethodCalibrationPoint>()
                        {
                            new() { CalibratorName = "CAL-1.0", ExpectedConcentration = 1.0 },
                            new() { CalibratorName = "CAL-2.0", ExpectedConcentration = 2.0 }
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
        _parametersDbContextMock.Setup(db => db.MethodTests).ReturnsDbSet(methodTests);

        var methods = methodTests.Select(x => x.Method).ToList();
        _parametersDbContextMock.Setup(db => db.Methods).ReturnsDbSet(methods);

        var methodCalibrators = methods.Select(x => x.CalibrationCurve).ToList();
        var calibratorPoints = new List<MethodCalibrationPoint>();
        foreach (var mc in methodCalibrators)
        {
            mc.Method = methods.First(m => m.CalibrationCurve == mc);
            foreach (var p in mc.CalibrationPoints)
            {
                p.CalibrationCurve = mc;
                calibratorPoints.Add(p);
            }
        }
        _parametersDbContextMock.Setup(db => db.MethodCalibrationCurves).ReturnsDbSet(methodCalibrators);
        _parametersDbContextMock.Setup(db => db.MethodCalibrationPoints).ReturnsDbSet(calibratorPoints);
    }

    /// <summary>
    /// Prepare articles
    /// </summary>
    private void PrepareArticleParameters()
    {
        var articleList = new List<Article>()
        {
            new()
            {
                Identity = "dn",
                FullName = "EliA dsDNA Well",
                ArticleNumber = "BFA",
                ArticleType = ArticleType.EliaWellCarrier,
                Quantity = 12,
                MaxDaysInInstrument = 28,
            },
        };

        _parametersDbContextMock.Setup(db => db.Articles).ReturnsDbSet(articleList);
    }
}
