
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Reagents;
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

namespace Pharos.Server.Result.Application.Services;

public class ResponseCalculationServiceTests
{
    private readonly ResponseCalculationService _sut;
    private readonly Mock<IResultDbContext> _dbContextMock = new();
    private readonly Mock<IParametersDbContext> _parametersContextMock = new();
    private readonly Mock<ILogger<ResponseCalculationService>> _loggerMock = new();
    private readonly Mock<ILogger<ResultCalculationService.ResultCalculationService>> _loggerCalcMock = new();
    private readonly Mock<ILogger<ReagentBarcodeService>> _barcodeServiceLoggerMock = new();
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly Mock<IReagentBarcodeService> _barcodeServiceMock = new();
    private readonly Mock<IOptionsSnapshot<ResultSettings>> _resultSettingMock = new();
    private readonly ICurveFitterApi _curveFitterApi = new CurveFitterApi();
    private const int _lowResponseLimit = 5;
    private const string _developmentSolutionBarcode = "C4KXXXNZ00011";


    public ResponseCalculationServiceTests()
    {
        var methodService = new MethodsService(_parametersContextMock.Object, new Mock<IAuthorizationService>().Object);
        var barcodeService = new ReagentBarcodeService(new ArticlesService(_parametersContextMock.Object), _memoryCache, _barcodeServiceLoggerMock.Object);
        var resultFormatter = new ResultFormatter(methodService, barcodeService, _resultSettingMock.Object);

        var resultCalc = new ResultCalculationService.ResultCalculationService(_dbContextMock.Object, _curveFitterApi, resultFormatter, _loggerCalcMock.Object, methodService, _barcodeServiceMock.Object);
        _sut = new ResponseCalculationService(_dbContextMock.Object, methodService, barcodeService, resultCalc, resultFormatter, _loggerMock.Object);
    }

    [Theory]
    [InlineData(true, 0.25)]
    [InlineData(false, 0.3)] //Equals 5 RU
    [InlineData(false, 0.35)]
    [Trait("ModuleRequirement", "MR_3502")]
    public async Task LowRUSampleTestReplicate(bool isLowResponse, double measurementValue)
    {
        var blank = AddBlank(_developmentSolutionBarcode);

        var sampleTestReplicate = new SampleTestReplicate
        {
            FluorometerValue = measurementValue
        };

        await _sut.CalculateResponseForTestReplicate(sampleTestReplicate, blank, TestResultType.Sample, "sIgE", "");

        if (isLowResponse)
        {
            sampleTestReplicate.IsLowResponse.Should().BeTrue();
            sampleTestReplicate.Response.Should().BeLessThan(_lowResponseLimit);
        }
        else
        {
            sampleTestReplicate.IsLowResponse.Should().BeFalse();
            sampleTestReplicate.Response.Should().BeGreaterOrEqualTo(_lowResponseLimit);
        }
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3502")]
    public async Task LowRUQualityControlTestReplicate()
    {
        var blank = AddBlank(_developmentSolutionBarcode);

        var qualityControlReplicate = new QualityControlTestReplicate
        {
            FluorometerValue = .25 // Mean signal equals less than 5 RU
        };

        await _sut.CalculateResponseForTestReplicate(qualityControlReplicate, blank, TestResultType.QualityControl, "sIgE", "");

        //Low RU check shall not be done for Quality Controls
        qualityControlReplicate.IsLowResponse.Should().BeFalse();
        qualityControlReplicate.Response.Should().BeLessThan(_lowResponseLimit);
    }

    [Theory]
    [InlineData(false, 0.0049, false)]
    [InlineData(true, 0.0049, true)]
    [InlineData(true, 0.0051, true)]
    [InlineData(true, 0.0051, false)]
    [Trait("ModuleRequirement", "MR_3502")]
    public async Task LowRUZeroCalibratorTestReplicate(bool lowRUShallBeChecked, double definedConcentration, bool reportLowRUForZeroCalibratorsFlag)
    {
        const string carrierBarcode = "818XXSS";

        AddArticleParametersData();

        //Add Blank & MethodCalibrationPoint
        AddBlank(_developmentSolutionBarcode);
        AddMethodCalibrationPoint(definedConcentration, reportLowRUForZeroCalibratorsFlag);
        var blank = AddBlank(_developmentSolutionBarcode);

        var calibratorTestReplicate = new CalibratorTestReplicate
        {
            CarrierBarcode = carrierBarcode,
            FluorometerValue = 0.25 // Mean signal equals less than 5 RU
        };

        await _sut.CalculateResponseForTestReplicate(calibratorTestReplicate, blank, TestResultType.Calibrator, "sIgE", "CAL-0.00");

        //Low RU check shall not be done for a calibrator point having a defined
        //concentration below 0.005 if the Method parameter for Reporting Low RU For Zero Calibrator is set to false.
        calibratorTestReplicate.Response.Should().BeLessThan(_lowResponseLimit);

        if (lowRUShallBeChecked)
        {
            calibratorTestReplicate.IsLowResponse.Should().BeTrue();
        }
        else
        {
            calibratorTestReplicate.IsLowResponse.Should().BeFalse();
        }
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3498")]
    public async Task CalculationOfRu()
    {
        const double meanVoltageSample = 1; // V: Mean voltage for sample
        const double meanVoltageBlank = 0.1; // AbV: Mean reagent Blank Voltage

        var sampleTestReplicate = new SampleTestReplicate
        {
            FluorometerValue = meanVoltageSample
        };

        // Formula: RU = (V – AbV) * FQ
        const double expectedResponseUnit = (meanVoltageSample - meanVoltageBlank) * ResponseCalculationService.FluorometerQuotient;

        var blank = AddBlank(_developmentSolutionBarcode);

        //Calculate response
        await _sut.CalculateResponseForTestReplicate(sampleTestReplicate, blank, TestResultType.Sample, "sIgE", "");

        //Assert response
        sampleTestReplicate.Response.Should().Be(expectedResponseUnit);
    }

    [Theory]
    [InlineData(260.0, 260.0, TestResultType.Sample, "El-G", "")] // No response adjustment for sample
    [InlineData(260.0, 260.0, TestResultType.QualityControl, "El-G", "")] // No response adjustment for QC
    [InlineData(260.0, 260.0, TestResultType.Calibrator, "El-G", "CAL-0.0")] // No response adjustment for calibrator point with expected concentration = 0.0
    [InlineData(260.0, 243.76502939061282, TestResultType.Calibrator, "El-G", "CAL-4.0")] // Response adjustment for calibrator point having a AdjustmentForAlfaBeta1Beta2 = 1
    [InlineData(260.0, 241.44498240627988, TestResultType.Calibrator, "El-G", "CAL-10.0")] // Response adjustment for calibrator point having a AdjustmentForAlfaBeta1Beta2 = 10
    [InlineData(260.0, 239.971851492483, TestResultType.CurveControl, "El-G", "CC-1")] // Response adjustment for curve control point having a AdjustmentForAlfaBeta1Beta2 = 10
    [Trait("ModuleRequirement", "MR_3533")]
    public async Task CalibratorAndCurveControlRuAdjustment_MR3533(double originalResponse, double expectedResponse, TestResultType testOrderType, string methodName, string calibratorPointName)
    {
        AddArticleParametersData();
        AddMethodParametersData();
        var blank = AddBlank(_developmentSolutionBarcode);

        TestReplicate testReplicate = new CalibratorTestReplicate
        {
            FluorometerValue = 10.5,
            CarrierBarcode = "BFNEJESE2B6345",
        };

        await _sut.CalculateResponseForTestReplicate(testReplicate, blank, testOrderType, methodName, calibratorPointName);
        CheckAdjustedResponse(originalResponse, expectedResponse, testReplicate);
    }

    [Theory]
    [InlineData(260.0, 260.0, TestResultType.Calibrator, "El-G", "CAL-4.0")] // No response adjustment if Alfa, Beta1 and Beta2 from carrier barcode are all 0
    [InlineData(260.0, 260.0, TestResultType.CurveControl, "El-G", "CC-1")] // No response adjustment if Alfa, Beta1 and Beta2 from carrier barcode are all 0
    [Trait("ModuleRequirement", "MR_3535")]
    public async Task CalibratorAndCurveControlRuAdjustment_MR3535(double originalResponse, double expectedResponse, TestResultType testOrderType, string methodName, string calibratorPointName)
    {
        AddArticleParametersData();
        AddMethodParametersData();
        var blank = AddBlank(_developmentSolutionBarcode);

        TestReplicate testReplicate = new CalibratorTestReplicate
        {
            FluorometerValue = 10.5,
            CarrierBarcode = "BFNEJ000000045",
        };

        await _sut.CalculateResponseForTestReplicate(testReplicate, blank, testOrderType, methodName, calibratorPointName);
        CheckAdjustedResponse(originalResponse, expectedResponse, testReplicate);
    }

    [Theory]
    [InlineData(260.0, 260.0, TestResultType.Calibrator, "El-G", "CAL-0.0")] // No response adjustment for calibrator point with expected concentration = 0.0
    [Trait("ModuleRequirement", "MR_3536")]
    public async Task CalibratorAndCurveControlRuAdjustment_MR3536(double originalResponse, double expectedResponse, TestResultType testOrderType, string methodName, string calibratorPointName)
    {
        AddArticleParametersData();
        AddMethodParametersData();
        var blank = AddBlank(_developmentSolutionBarcode);

        TestReplicate testReplicate = new CalibratorTestReplicate
        {
            FluorometerValue = 10.5,
            CarrierBarcode = "BFNEJESE2B6345",
        };

        await _sut.CalculateResponseForTestReplicate(testReplicate, blank, testOrderType, methodName, calibratorPointName);
        CheckAdjustedResponse(originalResponse, expectedResponse, testReplicate);
    }

    [Theory]
    [InlineData(260.0, 243.76502939061282, TestResultType.Calibrator, "El-G", "CAL-4.0")] // Response adjustment for calibrator point having a AdjustmentForAlfaBeta1Beta2 = 1
    [InlineData(260.0, 241.44498240627988, TestResultType.Calibrator, "El-G", "CAL-10.0")] // Response adjustment for calibrator point having a AdjustmentForAlfaBeta1Beta2 = 10
    [InlineData(260.0, 239.971851492483, TestResultType.CurveControl, "El-G", "CC-1")] // Response adjustment for curve control point having a AdjustmentForAlfaBeta1Beta2 = 10
    [Trait("ModuleRequirement", "MR_3537")]
    public async Task CalibratorAndCurveControlRuAdjustment_MR3537(double originalResponse, double expectedResponse, TestResultType testOrderType, string methodName, string calibratorPointName)
    {
        AddArticleParametersData();
        AddMethodParametersData();
        var blank = AddBlank(_developmentSolutionBarcode);

        TestReplicate testReplicate = new CalibratorTestReplicate
        {
            FluorometerValue = 10.5,
            CarrierBarcode = "BFNEJESE2B6345",
        };

        await _sut.CalculateResponseForTestReplicate(testReplicate, blank, testOrderType, methodName, calibratorPointName);
        CheckAdjustedResponse(originalResponse, expectedResponse, testReplicate);
    }

    private static void CheckAdjustedResponse(double originalResponse, double expectedResponse, TestReplicate testReplicate)
    {
        testReplicate.Response.Should().NotBeNull();
        if (testReplicate.Response != null)
        {
            Math.Round(testReplicate.Response.Value, 5).Should().Be(Math.Round(expectedResponse, 5));
        }
        testReplicate.ResponseFromInstrument.Should().NotBeNull();
        if (testReplicate.ResponseFromInstrument != null)
        {
            Math.Round(testReplicate.ResponseFromInstrument.Value, 5).Should().Be(Math.Round(originalResponse, 5));
        }
    }

    private void AddArticleParametersData()
    {
        var articles = new List<Article>()
        {
            new()
            {
                ArticleNumber = "BFN",
                Identity = "Gcal",
                FullName = "EliA IgG Calibrator Well",
                ArticleType = ArticleType.EliaCalibratorWellCarrier,
            },
            new()
            {
                ArticleNumber = "818",
                Identity = "a_IgE",
                FullName = "a_IgE",
                ArticleType = ArticleType.ImmunoCapCarrier,
            },
        };
        _parametersContextMock.Setup(db => db.Articles).ReturnsDbSet(articles);
    }

    private void AddMethodParametersData()
    {
        var methods = new List<Method>()
        {
            new()
            {
                MethodName = "El-G",
                CurveControls = new[]
                {
                    new MethodCurveControl()
                    {
                        CurveControlName = "CC-1",
                        ExpectedConcentration = 20,
                        AdjustmentForAlfaBeta1Beta2 = 10,
                    },
                },
                CalibrationCurve = new()
                {
                    CalibrationPoints = new[]
                    {
                        new MethodCalibrationPoint()
                        {
                            CalibratorName = "CAL-0.0",
                            ExpectedConcentration = 0.0,
                            AdjustmentForAlfaBeta1Beta2 = 1,
                        },
                        new MethodCalibrationPoint()
                        {
                            CalibratorName = "CAL-4.0",
                            ExpectedConcentration = 4.0,
                            AdjustmentForAlfaBeta1Beta2 = 1,
                        },
                        new MethodCalibrationPoint()
                        {
                            CalibratorName = "CAL-10.0",
                            ExpectedConcentration = 10.0,
                            AdjustmentForAlfaBeta1Beta2 = 10,
                        },
                        new MethodCalibrationPoint()
                        {
                            CalibratorName = "CAL-20.0",
                            ExpectedConcentration = 20.0,
                            AdjustmentForAlfaBeta1Beta2 = 1,
                        },
                        new MethodCalibrationPoint()
                        {
                            CalibratorName = "CAL-100",
                            ExpectedConcentration = 100.0,
                            AdjustmentForAlfaBeta1Beta2 = 1,
                        },
                        new MethodCalibrationPoint()
                        {
                            CalibratorName = "CAL-600",
                            ExpectedConcentration = 600.0,
                            AdjustmentForAlfaBeta1Beta2 = 1,
                        },
                    }
                }
            }
        };

        _parametersContextMock.Setup(x => x.Methods).ReturnsDbSet(methods);

        var methodCalibratorPoints = (from m in methods
                                      select m.CalibrationCurve.CalibrationPoints).SelectMany(p => p).ToList();
        _parametersContextMock.Setup(x => x.MethodCalibrationPoints).ReturnsDbSet(methodCalibratorPoints);

        foreach (var point in methodCalibratorPoints)
        {
            point.CalibrationCurve = (from m in methods
                                      let mc = m.CalibrationCurve
                                      where mc.CalibrationPoints.Contains(point)
                                      select mc).First();
            point.CalibrationCurve.Method = (from m in methods
                                             let mc = m.CalibrationCurve
                                             where mc.CalibrationPoints.Contains(point)
                                             select m).First();
        }

    }

    private void AddMethodCalibrationPoint(double definedConcentration, bool reportLowRUForZeroCalibratorsFlag)
    {
        var methodCalibratorPoints = new List<MethodCalibrationPoint>
        {
            new()
            {
                Id = 1,
                CalibratorName = "CAL-0.00",
                ExpectedConcentration = definedConcentration,
                CalibrationCurve = new MethodCalibrationCurve
                {
                    Id = 1,
                    ReportLowRuForZeroCalibrators = reportLowRUForZeroCalibratorsFlag,
                    Method = new Method
                    {
                        MethodName = "sIgE"
                    }
                }
            }
        };

        _parametersContextMock.Setup(x => x.MethodCalibrationPoints).ReturnsDbSet(methodCalibratorPoints);
    }

    private static Blank AddBlank(string devSolBarcode)
    {
        return new Blank
        {
            InstrumentNumber = "1",
            DevSolutionBarcode = devSolBarcode,
            DevSolutionCellPosition = 1,
            ResultOk = true,
            MeanVoltage = 0.1
        };
    }
}