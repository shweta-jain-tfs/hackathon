using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Result;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Services;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.CurveFit;
using Pharos.Server.Result.Domain;
using ConcentrationStatus = Pharos.Server.Result.Domain.ConcentrationStatus;

namespace Pharos.Server.Result.Application.Services.ResultCalculationService;

public class ViewAllergyTests
{
    private readonly ResultCalculationService _sut;
    private readonly Mock<IResultDbContext> _dbContextMock = new();
    private readonly ICurveFitterApi _curveFitterApi = new CurveFitterApi();
    private readonly Mock<ILogger<ResultCalculationService>> _loggerMock = new();
    private readonly Mock<IParametersDbContext> _parametersDbContextMock = new();
    private readonly Mock<IReagentBarcodeService> _barcodeServiceMock = new();
    private readonly Mock<IOptionsSnapshot<ResultSettings>> _resultSettingMock = new();

    public ViewAllergyTests()
    {
        var methodsService = new MethodsService(_parametersDbContextMock.Object, new Mock<IAuthorizationService>().Object);
        ResultFormatter resultFormatter = new(methodsService, _barcodeServiceMock.Object, _resultSettingMock.Object);
        PrepareMiscDbSets();

        _sut = new ResultCalculationService(_dbContextMock.Object, _curveFitterApi, resultFormatter, _loggerMock.Object, methodsService, _barcodeServiceMock.Object);
    }

    [Theory]
    [InlineData(330, 1)]
    [InlineData(1000, 3.03030303030303)]
    [InlineData(6943.5, 21.0409090909091)]
    [Trait("ModuleRequirement", "MR_3540")]
    public async Task ViewAllergy_CalculateConcentration(double response, double expectedConcentration)
    {
        var replicate = PrepareSampleResults(response);
        var curveId = PrepareCalibratorTestResultsForConcCalculation(replicate.TestResult.MethodName);
        PrepareMethodParameters();

        await _sut.CalculateReplicateConcentration(replicate, curveId, replicate.TestResult.PreDilutionFactor, replicate.TestResult.InstrumentDilutionFactor,
            replicate.TestResult.MethodName, replicate.TestResult.TestName, replicate.TestResult.ResultType, TestContext.Current.CancellationToken);
        replicate.Concentration.Should().NotBeNull();
        if (replicate.Concentration.HasValue)
        {
            Math.Round(replicate.Concentration.Value, 5).Should().Be(Math.Round(expectedConcentration, 5));
        }
    }

    [Theory]
    [InlineData(CurveJudgment.OK, new double[] { 6695, 7192, 6695, 7192 }, new double[] { 322, 338, 322, 338 })] // OK
    [InlineData(CurveJudgment.OK, new double[] { 6695, 7192, 6695, 3000 }, new double[] { 322, 338, 322, 338 })] // One replicates flagged - too low response: OK
    [InlineData(CurveJudgment.OK, new double[] { 6695, 7192, 6695, 7192 }, new double[] { 322, 338, 322, 700 })] // One replicates flagged - too high response: OK
    [InlineData(CurveJudgment.OK, new double[] { 14900, 7000, 6000, 6000 }, new double[] { 322, 338, 322, 338 })] // CV too high, no replicate flagged, 1 outliers (highest) detected but CV is OK after outliers removal: OK
    [InlineData(CurveJudgment.OK, new double[] { 14900, 14900, 14900, 3210 }, new double[] { 639, 639, 639, 639 })] // CV too high, no replicate flagged, 1 outliers (lowest) detected but CV is OK after outliers removal: OK
    [InlineData(CurveJudgment.NotOK, new double[] { 6695, 7192, 3000, 3000 }, new double[] { 322, 338, 322, 338 })] // Two replicates flagged, too low response: Not OK
    [InlineData(CurveJudgment.NotOK, new double[] { 6695, 7192, 6695, 7192 }, new double[] { 322, 338, 700, 700 })] // Two replicates flagged, too high response: Not OK
    [InlineData(CurveJudgment.NotOK, new double[] { 9000, 7000, 6000, 3000 }, new double[] { 322, 338, 322, 338 })] // CV too high and at least one replicate flagged: Not OK
    [InlineData(CurveJudgment.NotOK, new double[] { 6000, 6000, 6000, 6000 }, new double[] { 200, 200, 200, 200 })] // Replicates OK, but quotient of cont and ref is too low: Not OK
    [InlineData(CurveJudgment.NotOK, new double[] { 4000, 4000, 4000, 4000 }, new double[] { 300, 300, 300, 300 })] // Replicates OK, but quotient of cont and ref is too high: Not OK
    [InlineData(CurveJudgment.NotOK, new double[] { 9000, 7000, 6000, 6000 }, new double[] { 322, 338, 322, 338 })] // CV too high, no replicate flagged, no outliers: Not OK
    [InlineData(CurveJudgment.NotOK, new double[] { 14900, 9055, 9055, 9055, 9055, 9055, 9055, 3210 }, new double[] { 322, 338, 322, 338 })] // CV too high, no replicate flagged, 2 outliers (lowest and highest) detected: Not OK
    [Trait("ModuleRequirement", "MR_3539")]
    public async Task ViewAllergy_CalibratorCheck(CurveJudgment judgement, double[] contResponses, double[] refResponses)
    {
        var curves = ResultCalculationServiceTestsHelpers.PrepareCurves(_dbContextMock);

        var curve = curves.First(c => c.MethodName == "View");
        var calibratorTestResults = PrepareCalibratorResultsForCalibratorCheck(curve.Id, contResponses, refResponses);

        PrepareMethodParameters();

        await _sut.CheckAndFitCurve(curve.InstrumentNumber, curve.GroupId, calibratorTestResults, TestContext.Current.CancellationToken);

        curve.Judgment.Should().Be(judgement);
        if (judgement == CurveJudgment.OK)
        {
            foreach (var calResult in calibratorTestResults)
            {
                calResult.MeanConcentrationStatus.Should().Be(ConcentrationStatus.Ok);
                foreach (var repl in calResult.Replicates!)
                {
                    repl.ConcentrationStatus.Should().Be(ConcentrationStatus.Ok);
                }
            }
        }
    }

    /// <summary>
    /// Prepare calibrator results
    /// </summary>
    /// <param name="curveId">Curve id</param>
    /// <param name="contResponses">Response values for control replicates</param>
    /// <param name="refResponses">Response values for reference replicates</param>
    /// <returns></returns>
    private List<CalibratorTestResult> PrepareCalibratorResultsForCalibratorCheck(int curveId, double[] contResponses, double[] refResponses)
    {
        var calibratorTestResults = new List<CalibratorTestResult>();

        var cal = new CalibratorTestResult()
        {
            CurveId = curveId,
            ExpectedConcentration = 100,
            CalibratorPointName = "Cont-1",
            MethodName = "View",
            Replicates = new List<TestReplicate>()
        };
        foreach (var response in contResponses)
        {
            cal.Replicates.Add(new CalibratorTestReplicate() { Response = response });
        }
        cal.MeanResponse = cal.Replicates.Average(r => r.Response);
        calibratorTestResults.Add(cal);

        cal = new CalibratorTestResult()
        {
            CurveId = curveId,
            ExpectedConcentration = 3.5,
            CalibratorPointName = "Ref-1",
            MethodName = "View",
            Replicates = new List<TestReplicate>()
        };
        foreach (var response in refResponses)
        {
            cal.Replicates.Add(new CalibratorTestReplicate() { Response = response });
        }
        cal.MeanResponse = cal.Replicates.Average(r => r.Response);
        calibratorTestResults.Add(cal);

        _dbContextMock.Setup(db => db.CalibratorTestResults).ReturnsDbSet(calibratorTestResults);

        return calibratorTestResults;
    }



    /// <summary>
    /// Prepare a sample test order replicate
    /// </summary>
    /// <param name="response">Response value</param>
    /// <returns>Sample test order replicate</returns>
    private static SampleTestReplicate PrepareSampleResults(double response)
    {
        return new SampleTestReplicate()
        {
            Response = response,
            CarrierBarcode = "DHCXXF0",
            TestResult = new SampleTestResult()
            {
                TestName = "Vt17",
                MethodName = "View",
                InstrumentDilutionFactor = 1,
                NumberOfReplicates = 1,
                PreDilutionFactor = 1,
            },
        };
    }

    /// <summary>
    /// Prepare calibrators and curve
    /// </summary>
    /// <param name="methodName">Curve to return curve id for</param>
    /// <returns>Curve ID</returns>
    private int PrepareCalibratorTestResultsForConcCalculation(string methodName)
    {
        var curves = ResultCalculationServiceTestsHelpers.PrepareCurves(_dbContextMock);
        var curve = curves.First(c => c.MethodName == methodName);

        var calibratorTestResults = new List<CalibratorTestResult>()
        {
            new()
            {
                MeanResponse = 6943.5,
                ExpectedConcentration = 100,
            },
            new()
            {
                MeanResponse = 330,
                ExpectedConcentration = 3.5,
            },
        };
        curve.TestResults = calibratorTestResults;
        _dbContextMock.Setup(db => db.CalibratorTestResults).ReturnsDbSet(calibratorTestResults);

        return curve.Id;
    }

    private void PrepareMiscDbSets()
    {
        var currentCurves = new List<CurrentCurve>();
        _dbContextMock.Setup(db => db.CurrentCurves).ReturnsDbSet(currentCurves);
        var analyticalRuns = new List<AnalyticalRun>();
        _dbContextMock.Setup(db => db.AnalyticalRuns).ReturnsDbSet(analyticalRuns);
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
                TestName = "Vt17",
                ConcentrationFactor = 1,
                UseLotSpecificCode = false,
                IterativeAlfaBeta1Beta2Flag = false,
                AdjustmentForAlfaBeta1Beta2 = 1,
                HighestConcentrationToReport = null,
                LowerDetectionLimit = 0,
                ReportConcentrationBelowCurveFlag = false,
                ReportConcentrationAboveCurveFlag = false,
                DefaultCutOffConcentration = 0.35,
                DefaultReportCutOff = false,
                DefaultReportConcentration = true,
                DefaultReportClassId = 0,
                DefaultReportQuotient = QuotientReportMode.ReportAsFraction,
                DefaultReplicates = 1,

                Method = new Method()
                {
                    MethodName = "View",
                    Technology = AssayTechnology.ViewAllergy,
                    AnalyticalRun = new MethodAnalyticalRun{ RunCalibrationOnCarrierLotChange = false },
                    Calculation = new MethodCalculation()
                    {
                        CalculationType = CalculationType.Quotient,
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
                            new()
                            {
                                CalibratorName = "Cont-1",
                                ExpectedStdDev = 15,
                                StdDevPrecisionLimit = 14.81,
                                LowerLogarithmicLimit = 0,
                                UpperLogarithmicLimit = 0,
                                LowerResponseLimit = 3200,
                                UpperResponseLimit = 15000,
                                ExpectedConcentration = 100,
                            },
                            new()
                            {
                                CalibratorName = "Ref-1",
                                ExpectedStdDev = 15,
                                StdDevPrecisionLimit = 14.81,
                                LowerLogarithmicLimit = 0.036,
                                UpperLogarithmicLimit = 0.065,
                                LowerResponseLimit = 140,
                                UpperResponseLimit = 640,
                                ExpectedConcentration = 3.5,
                            },
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

        var methodCalibratorPoints = new List<MethodCalibrationPoint>();
        var methodCalibrators = methods.Select(x => x.CalibrationCurve).ToList();
        foreach (var mc in methodCalibrators)
        {
            mc.Method = methods.First(m => m.CalibrationCurve == mc);
            foreach (var p in mc.CalibrationPoints)
            {
                methodCalibratorPoints.Add(p);
                p.CalibrationCurve = mc;
            }
        }
        _parametersDbContextMock.Setup(db => db.MethodCalibrationCurves).ReturnsDbSet(methodCalibrators);
        _parametersDbContextMock.Setup(db => db.MethodCalibrationPoints).ReturnsDbSet(methodCalibratorPoints);
    }


}
