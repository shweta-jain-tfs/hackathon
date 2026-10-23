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

namespace Pharos.Server.Result.Application.Services.ResultCalculationService;

public class ResultCalculationServiceTests
{
    private readonly ResultCalculationService _sut;
    private readonly Mock<IResultDbContext> _dbContextMock = new();
    private readonly ICurveFitterApi _curveFitterApi = new CurveFitterApi();
    private readonly Mock<ILogger<ResultCalculationService>> _loggerMock = new();
    private readonly Mock<IParametersDbContext> _parametersContextMock = new();
    private readonly Mock<IOptionsSnapshot<ResultSettings>> _resultSettingMock = new();
    private const string _instrumentNumber = "SN001";
    private const double _maxDoubleDifference = 0.000000001;

    public ResultCalculationServiceTests()
    {
        var methodService = new MethodsService(_parametersContextMock.Object, new Mock<IAuthorizationService>().Object);
        var barcodeServiceMock = new Mock<IReagentBarcodeService>();
        var resultFormatter = new ResultFormatter(methodService, barcodeServiceMock.Object, _resultSettingMock.Object);
        _sut = new ResultCalculationService(_dbContextMock.Object, _curveFitterApi, resultFormatter, _loggerMock.Object, methodService, barcodeServiceMock.Object);
    }

    [Theory]
    [InlineData(16, 0)]
    [InlineData(172, 0.352197269773873)]
    [InlineData(320, 0.694897917307389)]
    [InlineData(1480, 3.50712050085314)]
    [InlineData(6352, 17.4939181279756)]
    [InlineData(17123, 80.602844044)]
    [InlineData(18619, 100.0065992945)]
    [Trait("ModuleRequirement", "MR_3503")]
    public async Task ResultTypes(double response, double expectedConcentration)
    {
        var replicate = PrepareSampleResults(response);
        var curveId = PrepareCalibrators(replicate.TestResult.MethodName);
        PrepareMethodParameters();

        await _sut.CalculateReplicateConcentration(replicate, curveId, replicate.TestResult.PreDilutionFactor, replicate.TestResult.InstrumentDilutionFactor,
            replicate.TestResult.MethodName, replicate.TestResult.TestName, replicate.TestResult.ResultType, TestContext.Current.CancellationToken);
        replicate.Concentration.Should().NotBeNull();
        if (replicate.Concentration != null)
        {
            Math.Round(replicate.Concentration.Value, 5).Should().Be(Math.Round(expectedConcentration, 5));
        }

        // MR_3504
        replicate.Class.Should().NotBeNull();

        // MR_3505
        replicate.CutOff.Should().NotBeNull();

        // MR_3506
        replicate.Quotient.Should().NotBeNull();
    }

    [Theory]
    [InlineData(16, "<10")]
    [InlineData(172, "<10")]
    [InlineData(320, "<10")]
    [InlineData(1480, "<10")]
    [InlineData(6352, ">10")]
    [InlineData(17123, ">30")]
    [InlineData(18619, "")]
    [Trait("ModuleRequirement", "MR_3504")]
    public async Task ResultTypes_Class(double response, string expectedClass)
    {
        var replicate = PrepareSampleResults(response);
        var curveId = PrepareCalibrators(replicate.TestResult.MethodName);
        PrepareMethodParameters();

        await _sut.CalculateReplicateConcentration(replicate, curveId, replicate.TestResult.PreDilutionFactor, replicate.TestResult.InstrumentDilutionFactor,
            replicate.TestResult.MethodName, replicate.TestResult.TestName, replicate.TestResult.ResultType, TestContext.Current.CancellationToken);

        replicate.Class.Should().Be(expectedClass);
    }

    [Theory]
    [InlineData(16, "Negative")]
    [InlineData(172, "Positive")]
    [InlineData(320, "Positive")]
    [InlineData(1480, "Positive")]
    [InlineData(6352, "Positive")]
    [InlineData(17123, "Positive")]
    [InlineData(18619, "Positive")]
    [Trait("ModuleRequirement", "MR_3505")]
    public async Task ResultTypes_CutOff(double response, string expectedCutOff)
    {
        var replicate = PrepareSampleResults(response);
        var curveId = PrepareCalibrators(replicate.TestResult.MethodName);
        PrepareMethodParameters();

        await _sut.CalculateReplicateConcentration(replicate, curveId, replicate.TestResult.PreDilutionFactor, replicate.TestResult.InstrumentDilutionFactor,
            replicate.TestResult.MethodName, replicate.TestResult.TestName, replicate.TestResult.ResultType, TestContext.Current.CancellationToken);

        replicate.CutOff.Should().Be(expectedCutOff);
    }

    [Theory]
    [InlineData(16, 0.0930232558139535)]
    [InlineData(172, 1)]
    [InlineData(320, 1.86046511627907)]
    [InlineData(1480, 8.6046511627907)]
    [InlineData(6352, 36.9302325581395)]
    [InlineData(17123, 99.552325581)]
    [InlineData(18619, 108.25)]
    [Trait("ModuleRequirement", "MR_3506")]
    public async Task ResultTypes_Quotient(double response, double expectedQuotient)
    {
        var replicate = PrepareSampleResults(response);
        var curveId = PrepareCalibrators(replicate.TestResult.MethodName);
        PrepareMethodParameters();

        await _sut.CalculateReplicateConcentration(replicate, curveId, replicate.TestResult.PreDilutionFactor, replicate.TestResult.InstrumentDilutionFactor,
            replicate.TestResult.MethodName, replicate.TestResult.TestName, replicate.TestResult.ResultType, TestContext.Current.CancellationToken);

        replicate.Quotient.Should().NotBeNull();
        if (replicate.Quotient != null)
        {
            Math.Round(replicate.Quotient.Value, 5).Should().Be(Math.Round(expectedQuotient, 5));
        }
    }

    [Theory]
    [InlineData(16, 0, 1, 2)]
    [InlineData(16, 0, 2, 1)]
    [InlineData(16, 0, 2, 2)]
    [InlineData(172, 0.352197269773873 * 10, 1, 10)]
    [InlineData(172, 0.352197269773873 * 5, 5, 1)]
    [InlineData(172, 0.352197269773873 * 50, 10, 5)]
    [InlineData(320, 0.694897917307389 * 10, 1, 10)]
    [InlineData(320, 0.694897917307389 * 5, 5, 1)]
    [InlineData(320, 0.694897917307389 * 50, 10, 5)]
    [InlineData(1480, 3.50712050085314 * 10, 1, 10)]
    [InlineData(1480, 3.50712050085314 * 5, 5, 1)]
    [InlineData(1480, 3.50712050085314 * 50, 10, 5)]
    [InlineData(6352, 17.4939181279756 * 10, 1, 10)]
    [InlineData(6352, 17.4939181279756 * 5, 5, 1)]
    [InlineData(6352, 17.4939181279756 * 50, 10, 5)]
    [InlineData(17123, 80.602844044 * 10, 1, 10)]
    [InlineData(17123, 80.602844044 * 5, 5, 1)]
    [InlineData(17123, 80.602844044 * 50, 10, 5)]
    [InlineData(18619, 100.0065992945 * 10, 1, 10)]
    [InlineData(18619, 100.0065992945 * 5, 5, 1)]
    [InlineData(18619, 100.0065992945 * 50, 10, 5)]
    [Trait("ModuleRequirement", "MR_3524")]
    public async Task CalculationOfResults_DilutionFactors(double response, double expectedConcentration, int preDilutionFactor, int instrumentDilutionFactor)
    {
        var replicate = PrepareSampleResults(response, preDilutionFactor, instrumentDilutionFactor);
        var curveId = PrepareCalibrators(replicate.TestResult.MethodName);
        PrepareMethodParameters();

        await _sut.CalculateReplicateConcentration(replicate, curveId, replicate.TestResult.PreDilutionFactor, replicate.TestResult.InstrumentDilutionFactor,
            replicate.TestResult.MethodName, replicate.TestResult.TestName, replicate.TestResult.ResultType, TestContext.Current.CancellationToken);
        replicate.Concentration.Should().NotBeNull();
        if (replicate.Concentration != null)
        {
            Math.Round(replicate.Concentration.Value, 5).Should().Be(Math.Round(expectedConcentration, 5));
        }
    }

    [Theory]
    [InlineData(16, 0, 1.1)]
    [InlineData(16, 0, 2.2)]
    [InlineData(172, 0.352197269773873 * 0.3, 0.3)]
    [InlineData(320, 0.694897917307389 * 4.4, 4.4)]
    [InlineData(1480, 3.50712050085314 * 5.5, 5.5)]
    [InlineData(6352, 17.4939181279756 * 6.6, 6.6)]
    [InlineData(17123, 80.602844044 * 111.1, 111.1)]
    [InlineData(18619, 100.0065992945 * 1111.2, 1111.2)]
    [Trait("ModuleRequirement", "MR_3524")]
    public async Task CalculationOfResults_ConcentrationFactor(double response, double expectedConcentration, double concentrationFactor)
    {
        var replicate = PrepareSampleResults(response);
        var curveId = PrepareCalibrators(replicate.TestResult.MethodName);
        PrepareMethodParameters(concentrationFactor);

        await _sut.CalculateReplicateConcentration(replicate, curveId, replicate.TestResult.PreDilutionFactor, replicate.TestResult.InstrumentDilutionFactor,
            replicate.TestResult.MethodName, replicate.TestResult.TestName, replicate.TestResult.ResultType, TestContext.Current.CancellationToken);
        replicate.Concentration.Should().NotBeNull();
        if (replicate.Concentration != null)
        {
            Math.Round(replicate.Concentration.Value, 5).Should().Be(Math.Round(expectedConcentration, 5));
        }
    }

    [Theory]
    [InlineData("sIgE", 15.51324382521594800000, 25738.51581929066000000000, 46.20552935690346900000, 0.98333011601701820000, 1.42195006529108330000)] // Data from Prime using CAST AS decimal(30, 20)
    [InlineData("El-G", 41.58043923573151100000, 23887.70218147974100000000, 331.57785084148207000000, 1.06431610597886510000, 0.00000000000000000000)] // Data from Prime using CAST AS decimal(30, 20)
    [Trait("ModuleRequirement", "MR_3524")]
    public async Task CalculationOfResults_CurveFit(string methodName, double parameterA, double parameterB, double parameterC, double parameterD, double parameterE)
    {
        var groupId = Guid.NewGuid();
        var calibratorList = PrepareCalibratorsForCurveFit(methodName, groupId);
        PrepareMethodParametersForCurveFit();

        var curve = await _sut.CheckAndFitCurve(_instrumentNumber, groupId, calibratorList, TestContext.Current.CancellationToken);
        curve.Should().NotBeNull();
        CheckCurveFit(curve, parameterA, parameterB, parameterC, parameterD, parameterE);
    }

    [Theory]
    [InlineData(true, false, null)]
    [InlineData(false, true, 999999d)]
    public async Task CalculationOfResults_RejectedReplicateIsExcludedAndCurveIsSuppressed(bool isAborted, bool isLowResponse, double? response)
    {
        var groupId = Guid.NewGuid();
        var calibratorList = PrepareCalibratorsForCurveFit("sIgE", groupId);
        var rejectedReplicate = new CalibratorTestReplicate
        {
            Response = response,
            IsAborted = isAborted,
            IsLowResponse = isLowResponse,
            TestResult = calibratorList[0]
        };
        calibratorList[0].Replicates!.Insert(0, rejectedReplicate);
        PrepareMethodParametersForCurveFit();

        var curve = await _sut.CheckAndFitCurve(_instrumentNumber, groupId, calibratorList, TestContext.Current.CancellationToken);

        curve.Should().NotBeNull();
        CheckCurveFit(curve, 15.513243825215948, 25738.51581929066, 46.20552935690347, 0.9833301160170182, 1.4219500652910833);
        curve!.Judgment.Should().Be(CurveJudgment.NotOK);
        curve.CurveFittedFlag.Should().BeFalse();
        rejectedReplicate.NormDoseCheckResult.Should().Be(NormDoseCheckResult.NotDefined);
        calibratorList.SelectMany(result => result.Replicates!)
            .Where(replicate => replicate != rejectedReplicate)
            .Cast<CalibratorTestReplicate>()
            .Should().OnlyContain(replicate => replicate.NormDoseCheckResult != NormDoseCheckResult.NotDefined);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task CalculationOfResults_AllReplicatesAtLevelRejectedKeepsResultMappingAligned(int rejectedLevelIndex)
    {
        var groupId = Guid.NewGuid();
        var calibratorList = PrepareCalibratorsForCurveFit("sIgE", groupId);
        var rejectedResult = calibratorList[rejectedLevelIndex];
        var rejectedReplicate = (CalibratorTestReplicate)rejectedResult.Replicates!.Single();
        rejectedReplicate.IsLowResponse = true;
        PrepareMethodParametersForCurveFit();

        var curve = await _sut.CheckAndFitCurve(_instrumentNumber, groupId, calibratorList, TestContext.Current.CancellationToken);

        curve.Should().NotBeNull();
        curve!.Judgment.Should().Be(CurveJudgment.NotOK);
        curve.CurveFittedFlag.Should().BeFalse();
        rejectedReplicate.NormDoseCheckResult.Should().Be(NormDoseCheckResult.NotDefined);
        calibratorList.Where(result => result != rejectedResult)
            .SelectMany(result => result.Replicates!)
            .Cast<CalibratorTestReplicate>()
            .Should().OnlyContain(replicate => replicate.NormDoseCheckResult != NormDoseCheckResult.NotDefined);
    }

    [Fact]
    public async Task CalculationOfResults_RejectedZeroPointSwitchLevelAdjustsMetadataAndSuppressesCurve()
    {
        var groupId = Guid.NewGuid();
        var calibratorList = PrepareCalibratorsForCurveFit("El-G", groupId);
        var rejectedReplicate = (CalibratorTestReplicate)calibratorList[1].Replicates!.Single();
        rejectedReplicate.IsLowResponse = true;
        PrepareMethodParametersForCurveFit();

        var curve = await _sut.CheckAndFitCurve(_instrumentNumber, groupId, calibratorList, TestContext.Current.CancellationToken);

        curve.Judgment.Should().Be(CurveJudgment.NotOK);
        curve.CurveFittedFlag.Should().BeFalse();
        curve.ZeroPointCalculationNumberOfIntervals.Should().Be(1);
        curve.NumberOfCalibrationPointsInZeroPointCalculation.Should().Be(3);
        curve.ZeroPointSwitchConcentration.Should().Be(10);
        // Three nonlinear levels remain, which is insufficient for the four-parameter fit.
        curve.ZeroPointCalculationCoefficientBeta.Should().Be(0);
        curve.ZeroPointCalculationCoefficientAlfa.Should().Be(0);
        rejectedReplicate.NormDoseCheckResult.Should().Be(NormDoseCheckResult.NotDefined);
    }

    [Theory]
    [InlineData(58.11013215859031100000, 23.71266580135809900000, 200.0, 3.39771752305835140000)] // Data from Prime using CAST AS decimal(30, 20)
    [Trait("ModuleRequirement", "MR_3542")]
    public async Task ZeroPointCalculation(double slope, double intercept, double sampleResponse, double expectedSampleConcentration)
    {
        var groupId = Guid.NewGuid();
        var calibratorList = PrepareCalibratorsForCurveFit("El-G", groupId);
        PrepareMethodParametersForCurveFit();

        var curve = await _sut.CheckAndFitCurve(_instrumentNumber, groupId, calibratorList, TestContext.Current.CancellationToken);
        curve.Should().NotBeNull();
        CheckSlopeAndIntercept(curve, slope, intercept);

        var replicate = PrepareSampleReplicateForZeroCalculation(sampleResponse, "El-G", "ce", 1, 100);
        await _sut.CalculateReplicateConcentration(
            replicate, curve.Id, replicate.TestResult.PreDilutionFactor, replicate.TestResult.InstrumentDilutionFactor,
            replicate.TestResult.MethodName, replicate.TestResult.TestName, replicate.TestResult.ResultType, TestContext.Current.CancellationToken);

        replicate.Concentration.Should().NotBeNull();
        if (replicate.Concentration != null)
        {
            var diff = replicate.Concentration.Value - expectedSampleConcentration;
            Math.Abs(diff).Should().BeLessOrEqualTo(_maxDoubleDifference);
        }
    }

    private static void CheckSlopeAndIntercept(Curve? curve, double slope, double intercept)
    {
        if (curve == null)
        {
            return;
        }

        curve.ZeroPointCalculationCoefficientBeta.Should().NotBeNull();
        if (curve.ZeroPointCalculationCoefficientBeta != null)
        {
            var diff1 = curve.ZeroPointCalculationCoefficientBeta.Value - slope;
            Math.Abs(diff1).Should().BeLessOrEqualTo(_maxDoubleDifference);
        }

        curve.ZeroPointCalculationCoefficientAlfa.Should().NotBeNull();
        if (curve.ZeroPointCalculationCoefficientAlfa == null)
        {
            return;
        }

        var diff2 = curve.ZeroPointCalculationCoefficientAlfa.Value - intercept;
        Math.Abs(diff2).Should().BeLessOrEqualTo(_maxDoubleDifference);
    }

    private static SampleTestReplicate PrepareSampleReplicateForZeroCalculation(double response, string methodName, string testName, int preDilution, int instrumentDilution)
    {
        return new SampleTestReplicate()
        {
            Response = response,
            InstrumentReplicateId = Guid.NewGuid(),
            TestResult = new SampleTestResult()
            {
                MethodName = methodName,
                TestName = testName,
                InstrumentDilutionFactor = instrumentDilution,
                PreDilutionFactor = preDilution
            }
        };
    }

    private List<CalibratorTestResult> PrepareCalibratorsForCurveFit(string methodName, Guid groupId)
    {
        _dbContextMock.Setup(db => db.AnalyticalRuns).ReturnsDbSet(new List<AnalyticalRun>());

        var curves = PrepareCurvesForCurveFit(groupId, methodName);
        var curve = curves.First(c => c.MethodName == methodName);

        var calibratorTestResults = CalibratorTestResultsForSpecficIgE(groupId, curve);
        if (methodName != "sIgE")
        {
            calibratorTestResults = CalibratorTestResultsForEliAIgG(groupId, curve);
        }
        _dbContextMock.Setup(db => db.CalibratorTestResults).ReturnsDbSet(calibratorTestResults);

        foreach (var calibratorTestResult in calibratorTestResults)
        {
            if (calibratorTestResult.Replicates != null)
            {
                foreach (var testReplicate in calibratorTestResult.Replicates)
                {
                    testReplicate.TestResult = calibratorTestResult;
                }
            }
        }

        return calibratorTestResults;
    }

    private List<Curve> PrepareCurvesForCurveFit(Guid groupId, string methodName)
    {
        var curveList = new List<Curve>()
        {
            new()
            {
                Id = 10,
                MethodName = "sIgE",
                GroupId = methodName == "sIgE" ? groupId : Guid.NewGuid(),
                CalculationType = CalculationType.Rodbard5Par,
                InstrumentNumber = _instrumentNumber,
                InstrumentRunId = Guid.NewGuid()
            },
            new()
            {
                Id = 20,
                MethodName = "El-G",
                GroupId = methodName == "El-G" ? groupId : Guid.NewGuid(),
                CalculationType = CalculationType.Rodbard4Par,
                InstrumentNumber = _instrumentNumber,
                InstrumentRunId = Guid.NewGuid()
            },
        };
        _dbContextMock.Setup(db => db.Curves).ReturnsDbSet(curveList);

        return curveList;
    }

    private static List<CalibratorTestResult> CalibratorTestResultsForSpecficIgE(Guid groupId, Curve curve)
    {
        var calibratorTestResults = new List<CalibratorTestResult>()
        {
            new()
            {
                CurveId = curve.Id,
                MethodName = "sIgE",
                TestName = "a_IgE",
                GroupId = groupId,
                MeanResponse = 16,
                ExpectedConcentration = 0.001,
                Replicates = new List<TestReplicate>()
                {
                    new CalibratorTestReplicate() { Response = 16 }
                }
            },
            new()
            {
                CurveId = curve.Id,
                MethodName = "sIgE",
                TestName = "a_IgE",
                GroupId = groupId,
                MeanResponse = 172,
                ExpectedConcentration = 0.35,
                Replicates = new List<TestReplicate>()
                {
                    new CalibratorTestReplicate() { Response = 172 }
                }
            },
            new()
            {
                CurveId = curve.Id,
                MethodName = "sIgE",
                TestName = "a_IgE",
                GroupId = groupId,
                MeanResponse = 320,
                ExpectedConcentration = 0.7,
                Replicates = new List<TestReplicate>()
                {
                    new CalibratorTestReplicate() { Response = 320 }
                }
            },
            new()
            {
                CurveId = curve.Id,
                MethodName = "sIgE",
                TestName = "a_IgE",
                GroupId = groupId,
                MeanResponse = 1480,
                ExpectedConcentration = 3.5,
                Replicates = new List<TestReplicate>()
                {
                    new CalibratorTestReplicate() { Response = 1480 }
                }
            },
            new()
            {
                CurveId = curve.Id,
                MethodName = "sIgE",
                TestName = "a_IgE",
                GroupId = groupId,
                MeanResponse = 6352,
                ExpectedConcentration = 17.5,
                Replicates = new List<TestReplicate>()
                {
                    new CalibratorTestReplicate() { Response = 6352 }
                }
            },
            new()
            {
                CurveId = curve.Id,
                MethodName = "sIgE",
                TestName = "a_IgE",
                GroupId = groupId,
                MeanResponse = 18619,
                ExpectedConcentration = 100,
                Replicates = new List<TestReplicate>()
                {
                    new CalibratorTestReplicate() { Response = 18619 }
                }
            },
        };
        return calibratorTestResults;
    }

    private static List<CalibratorTestResult> CalibratorTestResultsForEliAIgG(Guid groupId, Curve curve)
    {
        var calibratorTestResults = new List<CalibratorTestResult>()
        {
            new()
            {
                CurveId = curve.Id,
                MethodName = "El-G",
                TestName = "Gcal",
                GroupId = groupId,
                MeanResponse = 17,
                ExpectedConcentration = 0.0,
                Replicates = new List<TestReplicate>()
                {
                    new CalibratorTestReplicate() { Response = 17 }
                }
            },
            new()
            {
                CurveId = curve.Id,
                MethodName = "El-G",
                TestName = "Gcal",
                GroupId = groupId,
                MeanResponse = 256,
                ExpectedConcentration = 4.0,
                Replicates = new List<TestReplicate>()
                {
                    new CalibratorTestReplicate() { Response = 256 }
                }
            },
            new()
            {
                CurveId = curve.Id,
                MethodName = "El-G",
                TestName = "Gcal",
                GroupId = groupId,
                MeanResponse = 604,
                ExpectedConcentration = 10.0,
                Replicates = new List<TestReplicate>()
                {
                    new CalibratorTestReplicate() { Response = 604 }
                }
            },
            new()
            {
                CurveId = curve.Id,
                MethodName = "El-G",
                TestName = "Gcal",
                GroupId = groupId,
                MeanResponse = 1181,
                ExpectedConcentration = 20.0,
                Replicates = new List<TestReplicate>()
                {
                    new CalibratorTestReplicate() { Response = 1181 }
                }
            },
            new()
            {
                CurveId = curve.Id,
                MethodName = "El-G",
                TestName = "Gcal",
                GroupId = groupId,
                MeanResponse = 5252,
                ExpectedConcentration = 100.0,
                Replicates = new List<TestReplicate>()
                {
                    new CalibratorTestReplicate() { Response = 5252 }
                }
            },
            new()
            {
                CurveId = curve.Id,
                MethodName = "El-G",
                TestName = "Gcal",
                GroupId = groupId,
                MeanResponse = 15604,
                ExpectedConcentration = 600,
                Replicates = new List<TestReplicate>()
                {
                    new CalibratorTestReplicate() { Response = 15604 }
                }
            },
        };
        return calibratorTestResults;
    }

    private void PrepareMethodParametersForCurveFit()
    {
        var methodTestList = new List<MethodTest>()
        {
            new()
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
            },
            new()
            {
                Active = true,
                ConcentrationFactor = 1.0,
                DefaultConcentrationUnit = new MethodConcentrationUnit()
                {
                    ConcentrationUnit = "ug/l",
                    UnitId = 3
                },
                DefaultLabTestName = "Gcal",
                TestName = "Gcal",
                DefaultDilutionFactor = 1,
                DefaultDilutionMethod = DilutionMethod.PreDiluted,
                DefaultReplicates = 1,
                DefaultReportConcentration = true,
                DiluentIdentity = "Diluent",
                DefaultReportQuotient = QuotientReportMode.NoReport,
                FullTestName = "EliA IgG Calibrator Well",
                AdjustmentForAlfaBeta1Beta2 = 1.0,
                NumberOfDecimalsForConcentrationAbove100 = 0,
                NumberOfDecimalsForConcentrationBelow10 = 1,
                NumberOfDecimalsForConcentrationBetween10And100 = 0,
            },
            new()
            {
                Active = true,
                ConcentrationFactor = 0.0112,
                DefaultConcentrationUnit = new MethodConcentrationUnit()
                {
                    ConcentrationUnit = "u/ml",
                    UnitId = 2
                },
                DefaultLabTestName = "ce",
                TestName = "ce",
                DefaultDilutionFactor = 100,
                DefaultDilutionMethod = DilutionMethod.InstrumentDilution,
                DefaultReplicates = 1,
                DefaultReportConcentration = true,
                DiluentIdentity = "Diluent",
                DefaultReportQuotient = QuotientReportMode.NoReport,
                FullTestName = "EliA CENP",
                AdjustmentForAlfaBeta1Beta2 = 1.0,
                NumberOfDecimalsForConcentrationAbove100 = 0,
                NumberOfDecimalsForConcentrationBelow10 = 1,
                NumberOfDecimalsForConcentrationBetween10And100 = 0,
            },

        };
        var methodList = new List<Method>()
        {
            new()
            {
                MethodName = "sIgE",
                FullMethodName = "ImmunoCAP Specific IgE 0-100",
                Calculation = new MethodCalculation
                {
                    CalculationType = CalculationType.Rodbard5Par,
                    CalibratorStartingPoint = 0,
                    CalibratorsForLeastSquareCalc = 4,
                    MinCoefficientOfCorrelation = 0,
                    NormalWeight = NormalWeight.Predicted,
                    RobustWeight = 0,
                    PredictedVarianceA = 1.72783,
                    PredictedVarianceB = 0.002021
                },
                CalibrationCurve = new MethodCalibrationCurve()
                {
                    ExcludeLowestCalibratorFromMeanDiff = true,
                    MaxBadReplicates = 3,
                    MethodTest = methodTestList[0],
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
                            LowerLogarithmicLimit = -1.4,
                            StdDevPrecisionLimit = 3,
                            UpperLogarithmicLimit = 0.871,
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
                            LowerLogarithmicLimit = -0.2,
                            StdDevPrecisionLimit = 3,
                            UpperLogarithmicLimit = 0.2,
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
                            LowerLogarithmicLimit = -0.2,
                            StdDevPrecisionLimit = 3,
                            UpperLogarithmicLimit = 0.2,
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
                            LowerLogarithmicLimit = -0.2,
                            StdDevPrecisionLimit = 3,
                            UpperLogarithmicLimit = 0.2,
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
                            LowerLogarithmicLimit = -0.2,
                            StdDevPrecisionLimit = 3,
                            UpperLogarithmicLimit = 0.2,
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
                            LowerLogarithmicLimit = -0.2,
                            StdDevPrecisionLimit = 3,
                            UpperLogarithmicLimit = 0.2,
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
                    methodTestList[0]
                }
            },
            new()
            {
                MethodName = "El-G",
                FullMethodName = "EliA IgG",
                Calculation = new MethodCalculation
                {
                    CalculationType = CalculationType.Rodbard4Par,
                    CalibratorStartingPoint = 1,
                    CalibratorsForLeastSquareCalc = 4,
                    MinCoefficientOfCorrelation = 0,
                    NormalWeight = NormalWeight.Predicted,
                    RobustWeight = 0,
                    PredictedVarianceA = 2.025673,
                    PredictedVarianceB = 0.000458
                },
                CalibrationCurve = new MethodCalibrationCurve()
                {
                    ExcludeLowestCalibratorFromMeanDiff = true,
                    MaxBadReplicates = 3,
                    MethodTest = methodTestList[1],
                    CalibrationPoints = new List<MethodCalibrationPoint>()
                    {
                        new()
                        {
                            CalibratorName = "CAL-0.0",
                            ExpectedConcentration = 0.0,
                            Replicates = 2,
                            IdealResponse = 33,
                            StdDevLimit = 4.2,
                            ExpectedStdDev = 3,
                            LowerResponseLimit = 0,
                            LowerLogarithmicLimit = 0,
                            StdDevPrecisionLimit = 19.0,
                            UpperLogarithmicLimit = 0.0,
                            UpperResponseLimit = 200,
                        },
                        new()
                        {
                            CalibratorName = "CAL-4.0",
                            ExpectedConcentration = 4.0,
                            Replicates = 2,
                            IdealResponse = 309,
                            StdDevLimit = 4.2,
                            ExpectedStdDev = 10,
                            LowerResponseLimit = 124,
                            LowerLogarithmicLimit = -0.2,
                            StdDevPrecisionLimit = 3,
                            UpperLogarithmicLimit = 0.2,
                            UpperResponseLimit = 9288,
                        },
                        new()
                        {
                            CalibratorName = "CAL-10.0",
                            ExpectedConcentration = 10.0,
                            Replicates = 2,
                            IdealResponse = 705,
                            StdDevLimit = 4.2,
                            ExpectedStdDev = 19,
                            LowerResponseLimit = 124,
                            LowerLogarithmicLimit = -0.2,
                            StdDevPrecisionLimit = 3,
                            UpperLogarithmicLimit = 0.2,
                            UpperResponseLimit = 9288,
                        },
                        new()
                        {
                            CalibratorName = "CAL-20.0",
                            ExpectedConcentration = 20.0,
                            Replicates = 2,
                            IdealResponse = 1396,
                            StdDevLimit = 4.2,
                            ExpectedStdDev = 46,
                            LowerResponseLimit = 124,
                            LowerLogarithmicLimit = -0.2,
                            StdDevPrecisionLimit = 3,
                            UpperLogarithmicLimit = 0.2,
                            UpperResponseLimit = 9288,
                        },
                        new()
                        {
                            CalibratorName = "CAL-100",
                            ExpectedConcentration = 100.0,
                            Replicates = 2,
                            IdealResponse = 6192,
                            StdDevLimit = 4.2,
                            ExpectedStdDev = 180,
                            LowerResponseLimit = 124,
                            LowerLogarithmicLimit = -0.2,
                            StdDevPrecisionLimit = 3,
                            UpperLogarithmicLimit = 0.2,
                            UpperResponseLimit = 10688,
                        },
                        new()
                        {
                            CalibratorName = "CAL-600",
                            ExpectedConcentration = 600.0,
                            Replicates = 2,
                            IdealResponse = 18355,
                            StdDevLimit = 4.2,
                            ExpectedStdDev = 616,
                            LowerResponseLimit = 12000,
                            LowerLogarithmicLimit = -0.2,
                            StdDevPrecisionLimit = 3,
                            UpperLogarithmicLimit = 0.2,
                            UpperResponseLimit = 31663,
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
                    DefaultTextQuantitativeAbove = "Above",
                    DefaultTextQuantitativeBelow = "Below",
                    DefaultTextAboveCutOff = "Positive",
                    DefaultTextBelowCutOff = "Negative",
                    DefaultTextQuantitativeDoubtful = "",
                },
                Tests = new List<MethodTest>()
                {
                    methodTestList[1],
                    methodTestList[2]
                }
            }
        };
        methodTestList[0].DefaultConcentrationUnit.Method = methodList[0];
        methodTestList[1].DefaultConcentrationUnit.Method = methodList[1];
        methodTestList[2].DefaultConcentrationUnit.Method = methodList[1];
        _parametersContextMock.Setup(db => db.Methods).ReturnsDbSet(methodList);

        foreach (var method in methodList)
        {
            method.CalibrationCurve.Method = method;
            foreach (var calibrationPoint in method.CalibrationCurve.CalibrationPoints)
            {
                calibrationPoint.CalibrationCurve = method.CalibrationCurve;
            }

            foreach (var methodTest in method.Tests)
            {
                methodTest.Method = method;
            }
        }

        var methodTests = methodList.SelectMany(m => m.Tests).ToList();
        _parametersContextMock.Setup(db => db.MethodTests).ReturnsDbSet(methodTests);

        var methodCalibrationPoints = methodList.SelectMany(m => m.CalibrationCurve.CalibrationPoints).ToList();
        _parametersContextMock.Setup(db => db.MethodCalibrationPoints).ReturnsDbSet(methodCalibrationPoints);

    }

    private static void CheckCurveFit(Curve? curve, double parameterA, double parameterB, double parameterC, double parameterD, double parameterE)
    {
        if (curve == null)
        {
            return;
        }

        CheckParameter(curve.CalculationParameterA, parameterA);
        CheckParameter(curve.CalculationParameterB, parameterB);
        CheckParameter(curve.CalculationParameterC, parameterC);
        CheckParameter(curve.CalculationParameterD, parameterD);
        CheckParameter(curve.CalculationParameterE, parameterE);
    }

    private static void CheckParameter(double? storedParameter, double expectedParameter)
    {
        storedParameter.Should().NotBeNull();
        var diff = storedParameter.Value - expectedParameter;
        Math.Abs(diff).Should().BeLessOrEqualTo(_maxDoubleDifference);
    }

    /// <summary>
    /// Prepare a sample test order replicate
    /// </summary>
    /// <param name="response">Response value</param>
    /// <param name="preDilutionFactor">Pre-dilution factor</param>
    /// <param name="instrumentDilutionFactor">Instrument dilution</param>
    /// <returns>Sample test order replicate</returns>
    private static SampleTestReplicate PrepareSampleResults(double response, int preDilutionFactor = 1, int instrumentDilutionFactor = 1)
    {
        var replicate = new SampleTestReplicate()
        {
            Response = response,
            CarrierBarcode = "409XX01",
            TestResult = new SampleTestResult()
            {
                TestName = "c1",
                MethodName = "sIgE",
                InstrumentDilutionFactor = instrumentDilutionFactor,
                NumberOfReplicates = 1,
                PreDilutionFactor = preDilutionFactor,
            },
        };

        replicate.TestResult.Replicates = new List<TestReplicate>()
        {
            replicate,
        };

        return replicate;
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
                MeanResponse = 16,
                ExpectedConcentration = 0.001,
            },
            new()
            {
                MeanResponse = 172,
                ExpectedConcentration = 0.35,
            },
            new()
            {
                MeanResponse = 320,
                ExpectedConcentration = 0.7,
            }
        };
        curve.TestResults = calibratorTestResults;
        _dbContextMock.Setup(db => db.CalibratorTestResults).ReturnsDbSet(calibratorTestResults);

        return curve.Id;
    }

    /// <summary>
    /// Prepare method parameters
    /// </summary>
    private void PrepareMethodParameters(double concentrationFactor = 1.0)
    {
        var methodTests = new List<MethodTest>()
        {
            new()
            {
                TestName = "c1",
                ConcentrationFactor = concentrationFactor,
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
        _parametersContextMock.Setup(db => db.MethodTests).ReturnsDbSet(methodTests);

        var methods = methodTests.Select(x => x.Method).ToList();
        _parametersContextMock.Setup(db => db.Methods).ReturnsDbSet(methods);

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
        _parametersContextMock.Setup(db => db.MethodCalibrationCurves).ReturnsDbSet(methodCalibrators);
        _parametersContextMock.Setup(db => db.MethodCalibrationPoints).ReturnsDbSet(calibratorPoints);
    }
}
