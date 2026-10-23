using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Commands;
using Pharos.Server.Result.Application.Events;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Queries.GetCalibrationCurve;
using Pharos.Server.Result.CurveFit;
using Pharos.Server.Result.CurveFit.PiePuf;
using Pharos.Server.Result.Domain;
using ConcentrationStatus = Pharos.Server.Result.Domain.ConcentrationStatus;

namespace Pharos.Server.Result.Application.Services.ResultCalculationService;

internal class ResultCalculationService : IResultCalculationService
{
    private readonly IResultDbContext _dbContext;
    private readonly ICurveFitterApi _curveFitterApi;
    private readonly IResultFormatter _resultFormatter;
    private readonly ILogger<ResultCalculationService> _logger;
    private readonly IMethodsService _methodsService;
    private readonly IReagentBarcodeService _barcodeService;

    public ResultCalculationService(IResultDbContext dbContext,
        ICurveFitterApi curveFitterApi,
        IResultFormatter resultFormatter,
        ILogger<ResultCalculationService> logger,
        IMethodsService methodsService,
        IReagentBarcodeService barcodeService)
    {
        _dbContext = dbContext;
        _curveFitterApi = curveFitterApi;
        _resultFormatter = resultFormatter;
        _logger = logger;
        _methodsService = methodsService;
        _barcodeService = barcodeService;
        Logger.LogText = logger.LogInformation;
    }

    private const double _concentrationLimitOn = 0.005;
    private const double _maxConcentrationValue = 999999999;

    /// <summary>
    /// Check and fit a curve from list of calibrator test orders.
    /// NOTE! The calibrator test orders are expected to have their calibrator test order replicates included!
    /// </summary>
    /// <param name="instrumentNumber">Instrument ID</param>
    /// <param name="groupId">Group id of the group of calibrators</param>
    /// <param name="calibratorTestResults">List of calibrator test orders (with replicates)</param>
    /// <param name="cancellationToken"></param>
    public async Task<Curve> CheckAndFitCurve(
        string instrumentNumber,
        Guid groupId,
        List<CalibratorTestResult> calibratorTestResults,
        CancellationToken cancellationToken)
    {
        var curve = await GetCurve(instrumentNumber, groupId, cancellationToken);
        await CheckAndFitCurve(curve, calibratorTestResults);
        return curve;
    }

    /// <summary>
    /// Check and fit a curve from list of calibrator test orders.
    /// </summary>
    /// <param name="curve">Curve in database</param>
    /// <param name="calibratorTestResults">List of calibrator test orders (with replicates)</param>
    private async Task CheckAndFitCurve(Curve curve, List<CalibratorTestResult> calibratorTestResults)
    {
        var testResultWithRun = calibratorTestResults
            .FirstOrDefault(r => r.AnalyticalRunId != null);

        switch (curve.CalculationType)
        {
            case CalculationType.Quotient:
                await CheckAndCalculateCalibratorsForQuotient(curve, calibratorTestResults);
                break;

            case CalculationType.CutOff:
            case CalculationType.Spline:
            case CalculationType.SplineNonSmooth:
            case CalculationType.Rodbard4Par:
            case CalculationType.Rodbard5Par:
            case CalculationType.Richards:
            default:
                await CheckAndCurveFit(curve, calibratorTestResults);
                break;
        }
        var analyticalRunId = testResultWithRun?.AnalyticalRunId;
        if (analyticalRunId != null)
        {
            curve.AddDomainEvent(new NewCurve(curve, analyticalRunId.Value));
        }
    }

    private async Task CheckAndCalculateCalibratorsForQuotient(Curve curve, List<CalibratorTestResult> testResults)
    {
        var quotientCurveCheckData = await QuotientCurveCheck.Init(_methodsService, curve.MethodName, testResults);
        if (QuotientCalibratorCheck(curve, quotientCurveCheckData))
        {
            curve.Judgment = CurveJudgment.OK;
        }
        await StoreQuotientCalibratorCheck(quotientCurveCheckData);

        if (curve.Judgment == CurveJudgment.OK && !await QuotientRatioCheck(quotientCurveCheckData))
        {
            curve.Judgment = CurveJudgment.NotOK;
        }

        var quotientIndex = -1;
        double? referenceResponse = null;

        if (testResults.Count > 0)
        {
            quotientIndex = await GetCalibratorsQuotientIndex(testResults.First().MethodName);
            if (quotientIndex <= testResults.Count)
            {
                referenceResponse = testResults.OrderBy(tr => tr.ExpectedConcentration).Skip(quotientIndex - 1).First().MeanResponse;
            }

        }
        foreach (var testResult in testResults)
        {
            await CalculateResultsForOneQuotientCalibratorPoint(curve.Id, quotientIndex, testResult, referenceResponse);
        }
    }

    /// <summary>
    /// Calculate results for one calibrator point when quotient is the calculation type
    /// </summary>
    /// <param name="curveId">Id of the curve</param>
    /// <param name="quotientIndex">Index of calibrator used for quotient</param>
    /// <param name="testResult">Test order</param>
    /// <param name="referenceResponse">Reference response</param>
    /// <returns></returns>
    private async Task CalculateResultsForOneQuotientCalibratorPoint(int curveId, int quotientIndex, CalibratorTestResult testResult, double? referenceResponse)
    {
        if (testResult.Replicates != null && testResult.Replicates.Count > 0)
        {
            var totalDilutionFactor = testResult.PreDilutionFactor * testResult.InstrumentDilutionFactor;

            foreach (var replicate in testResult.Replicates)
            {
                if (replicate.Response != null)
                {
                    (replicate.Concentration, replicate.ConcentrationAsText) = await CalculateQuotient(replicate.Response, QuotientReportMode.ReportAsFraction, quotientIndex, curveId, true, false, totalDilutionFactor, referenceResponse);
                    if (replicate.Concentration != null)
                    {
                        replicate.ConcentrationStatus = ConcentrationStatus.Ok;
                    }

                }
            }
            await CalculateSummaryResult(testResult);
        }
    }

    /// <summary>
    /// Check each calibrator point against limits in method parameters
    /// </summary>
    /// <param name="curve">Curve</param>
    /// <param name="quotientCurveCheckData">Quotient check data</param>
    /// <returns>True if all is ok</returns>
    private bool QuotientCalibratorCheck(Curve curve, QuotientCurveCheck quotientCurveCheckData)
    {
        _logger.LogInformation("Calculating calibrators with Quotient calculation type");

        var result = true;

        foreach (var point in quotientCurveCheckData.Points)
        {
            if (!QuotientCalibratorCheckOnePoint(point))
            {
                result = false;
            }
        }

        // Store "curve" data
        curve.CurveFittedFlag = true;
        curve.CalculationParameterA = 0;
        curve.CalculationParameterB = 0;
        curve.CalculationParameterC = 0;
        curve.CalculationParameterD = 0;
        curve.CalculationParameterE = 0;
        curve.ZeroPointCalculationNumberOfIntervals = 0;
        curve.ZeroPointSwitchConcentration = 0;
        curve.NumberOfCalibrationPointsInZeroPointCalculation = 0;
        curve.ZeroPointCalculationCoefficientAlfa = 0;
        curve.ZeroPointCalculationCoefficientBeta = 0;
        curve.IterationsUsed = 0;
        curve.MaxIterationsAllowed = 150;
        curve.CurveMinConcentration = 0;
        curve.CurveMaxConcentration = 0;
        curve.ErrorPrecisionFlag = false;
        curve.ErrorFlatCurveFlag = false;
        curve.ErrorLowerPartTooHighFlag = false;
        curve.ErrorSingleCalibratorAboveRangeFlag = false;
        curve.ErrorSingleCalibratorBelowRangeFlag = false;
        curve.ErrorNoRisingTrendFlag = false;

        curve.Judgment = result ? CurveJudgment.OK : CurveJudgment.NotOK;

        return result;
    }

    /// <summary>
    /// Check one calibrator point against limits in method parameters
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    private bool QuotientCalibratorCheckOnePoint(QuotientCurveCheck.CurvePoint point)
    {
        var result = true;
        var nFlaggedReplicates = 0;
        var calc = new StatisticsCalculator();
        var indexHighestReplicate = 0;
        var indexLowestReplicate = 0;
        double highestResponse = 0;
        double lowestResponse = 0;

        _logger.LogInformation("Checking calibrator quotients for defined concentration {DefinedConcentration}", point.DefinedConcentration);

        for (var nReplicate = 0; nReplicate < point.Replicates.Count; nReplicate++)
        {
            var response = point.Replicates[nReplicate].Response;

            if (response == null)
            {
                point.ReplicateFlagged.Add(true);
                nFlaggedReplicates = 2; // => Calibration is Not OK
            }
            else if (response > point.LowerResponseLimit && response < point.UpperResponseLimit)
            {
                _logger.LogInformation("- Replicate {Replicate} with response {Response} is within Lower/Upper response limit", nReplicate, response);
                if (nReplicate == 0 || response > highestResponse)
                {
                    indexHighestReplicate = nReplicate;
                    highestResponse = response.Value;
                }

                if (nReplicate == 0 || response < lowestResponse)
                {
                    indexLowestReplicate = nReplicate;
                    lowestResponse = response.Value;
                }

                calc.Add(response);
                point.ReplicateFlagged.Add(false);
            }
            else
            {
                _logger.LogInformation("- Replicate {Replicate} with response {Response} is outside Lower/Upper response limit", nReplicate, response);
                point.ReplicateFlagged.Add(true);
                nFlaggedReplicates++;
            }
        }

        if (nFlaggedReplicates > 1)
        {
            _logger.LogInformation("* More than 1 point flagged, calibration is Not OK");
            return false;
        }

        if (calc.CV >= point.ExpectedSD)
        {
            if (nFlaggedReplicates > 0)
            {
                _logger.LogInformation("* CV is too high and >0 points are flagged, Calibration is Not OK");
                return false;
            }
            // Do "Grubbs"
            else
            {
                return CheckOutliersUsingGrubbs(point, calc.SD, calc.Mean, point.PrecisionSDLimit, indexHighestReplicate, indexLowestReplicate);
            }
        }

        return result;
    }

    /// <summary>
    /// Check outliers using grubbs
    /// </summary>
    /// <param name="point"></param>
    /// <param name="replicateSD"></param>
    /// <param name="replicateMean"></param>
    /// <param name="precisionSDLimit"></param>
    /// <param name="indexHighestReplicate"></param>
    /// <param name="indexLowestReplicate"></param>
    /// <returns></returns>
    private bool CheckOutliersUsingGrubbs(QuotientCurveCheck.CurvePoint point, double replicateSD, double replicateMean, double precisionSDLimit, int indexHighestReplicate, int indexLowestReplicate)
    {
        _logger.LogInformation("* CV is too high but 0 points is flagged, do Outliers detection");

        var nHighOutliers = 0;
        var highestIsOutlier = Grubbs(point.Replicates[indexHighestReplicate].Response, replicateSD, replicateMean, precisionSDLimit / 10);
        if (highestIsOutlier)
        {
            // Could be more than one replicate with the same high response: Loop and flag them all
            for (var nReplicate = 0; nReplicate < point.Replicates.Count; nReplicate++)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (point.Replicates[nReplicate].Response == point.Replicates[indexHighestReplicate].Response)
                {
                    point.ReplicateFlagged[nReplicate] = true;
                    nHighOutliers++;
                }
            }
        }

        var nLowOutliers = 0;
        var lowestIsOutlier = Grubbs(point.Replicates[indexLowestReplicate].Response, replicateSD, replicateMean, precisionSDLimit / 10);
        if (lowestIsOutlier)
        {
            // Could be more than one replicate with the same low response: Loop and flag them all
            for (var nReplicate = 0; nReplicate < point.Replicates.Count; nReplicate++)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (point.Replicates[nReplicate].Response == point.Replicates[indexLowestReplicate].Response)
                {
                    point.ReplicateFlagged[nReplicate] = true;
                    nLowOutliers++;
                }
            }
        }

        // No point is outliers
        if (!highestIsOutlier && !lowestIsOutlier)
        {
            _logger.LogInformation("* No outliers detected, Calibration Not OK");
            for (var i = 0; i < point.Replicates.Count; i++)  // Flag all the replicates in this level
            {
                point.ReplicateFlagged[i] = true;
            }
            return false;
        }

        // >1 point is outliers
        if (nHighOutliers + nLowOutliers > 1)
        {
            _logger.LogInformation("* Two or more outliers detected, Calibration Not OK");
            return false;
        }

        // 1 outlier, recalculate statistics on non-flagged points
        var statisticsCalculatorNonFlagged = new StatisticsCalculator();
        for (var i = 0; i < point.Replicates.Count; i++)
        {
            if (!point.ReplicateFlagged[i])
            {
                statisticsCalculatorNonFlagged.Add(point.Replicates[i].Response);
            }
        }
        _logger.LogInformation("* CV is {CV}, limit is {ExpectedSD}", statisticsCalculatorNonFlagged.CV, point.ExpectedSD);

        if (statisticsCalculatorNonFlagged.CV >= point.ExpectedSD)
        {
            _logger.LogInformation("1 outliers detected and CV is too high, Calibration Not OK");
            return false;
        }
        _logger.LogInformation("1 outliers detected but CV is OK, Calibration is OK");

        return true;
    }

    /// <summary>
    /// Perform a Grubbs outliers detection.
    /// </summary>        
    /// <param name="replicateValue">Value of replicate to check</param>    
    /// <param name="stdDev">Standard deviation</param>        
    /// <param name="meanValue">Mean value</param>
    /// <param name="outlierLimit">The outliers limit</param>      
    /// <returns>True if it is an outliers, False if not</returns>
    private bool Grubbs(double? replicateValue, double stdDev, double meanValue, double outlierLimit)
    {
        if (replicateValue == null)
        {
            return false;
        }

        var gDiff = Math.Abs(replicateValue.Value - meanValue) / stdDev;

        _logger.LogInformation("Grubbs Value={Response}, GDiff={GDiff} (Limit={Limit})", replicateValue, gDiff, outlierLimit);

        if (gDiff > outlierLimit)
        {
            _logger.LogInformation("Outliers detected");
            return true;
        }
        else
        {
            _logger.LogInformation("Outliers not detected");
            return false;
        }
    }

    /// <summary>
    /// Store outcome of quotient calibrator check in replicates and result
    /// </summary>
    /// <param name="quotientCurveCheckData"></param>
    private async Task StoreQuotientCalibratorCheck(QuotientCurveCheck quotientCurveCheckData)
    {
        foreach (var point in quotientCurveCheckData.Points)
        {
            CalibratorTestResult? testResult = null;
            for (var nReplicate = 0; nReplicate < point.Replicates.Count; nReplicate++)
            {
                var replicate = point.Replicates[nReplicate];

                if (testResult == null && replicate.TestResult is CalibratorTestResult calResult)
                {
                    testResult = calResult;
                }
                if (point.ReplicateFlagged[nReplicate])
                {
                    replicate.NormDoseCheckResult = NormDoseCheckResult.NotOk;
                    replicate.Active = false;
                }
                else
                {
                    replicate.NormDoseCheckResult = NormDoseCheckResult.Ok;
                    replicate.Active = true;
                }
            }
            if (testResult != null)
            {
                await CalculateSummaryResult(testResult);
            }
            else
            {
                _logger.LogInformation("Missing calibrator test result");
            }
        }
    }

    public void CalculateMeanResponse(TestResult result)
    {
        if (result.Replicates == null || result.Replicates.Count == 0)
        {
            return;
        }

        var anyLowResponse = false;
        var anyNonLowResponse = false;
        var calcResp = new StatisticsCalculator();

        foreach (var replicate in result.Replicates)
        {
            if (replicate is { Response: not null, IsLowResponse: false, Active: true, IsAborted: false })
            {
                calcResp.Add(replicate.Response.Value);
            }
            if (replicate.IsLowResponse)
            {
                anyLowResponse = true;
            }
            else
            {
                anyNonLowResponse = true;
            }
        }

        if (anyLowResponse && anyNonLowResponse && result is CalibratorTestResult)
        {
            anyLowResponse = false;
        }

        if (calcResp.NumberOfData > 0 && !anyLowResponse)
        {
            result.MeanResponse = calcResp.Mean;
            result.MeanResponseAsText = _resultFormatter.ResponseAsText(result.MeanResponse.Value);
            result.PercentCvResponse = calcResp.CV;
        }
        else if (anyLowResponse)
        {
            result.MeanResponseAsText = _resultFormatter.LowRU();
        }
        else
        {
            result.MeanResponseAsText = _resultFormatter.NoDataAvailable();
        }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="quotientCurveCheckData"></param>
    /// <returns></returns>
    private async Task<bool> QuotientRatioCheck(QuotientCurveCheck quotientCurveCheckData)
    {
        var statisticsCalculator_R = new StatisticsCalculator();
        var statisticsCalculator_H = new StatisticsCalculator();

        _logger.LogInformation("QuotientRatioCheck");

        // Do statistics on all non-flagged points in Reference
        for (var i = 0; i < quotientCurveCheckData.Points[0].Replicates.Count; i++)
        {
            if (!quotientCurveCheckData.Points[0].ReplicateFlagged[i])
            {
                statisticsCalculator_R.Add(quotientCurveCheckData.Points[0].Replicates[i].Response);
            }
        }

        // Do statistics on all non-flagged points in Control
        for (var i = 0; i < quotientCurveCheckData.Points[1].Replicates.Count; i++)
        {
            if (!quotientCurveCheckData.Points[1].ReplicateFlagged[i])
            {
                statisticsCalculator_H.Add(quotientCurveCheckData.Points[1].Replicates[i].Response);
            }
        }

        _logger.LogInformation("MeanValue_R={Mean}", statisticsCalculator_R.Mean);
        _logger.LogInformation("MeanValue_H={Mean}", statisticsCalculator_H.Mean);

        var ratio = statisticsCalculator_R.Mean / statisticsCalculator_H.Mean;

        _logger.LogInformation("Ratio={Ratio}", ratio);
        _logger.LogInformation("Limit={Limit1}-{Limit2}", quotientCurveCheckData.Points[0].LowerLogarithmicLimit, quotientCurveCheckData.Points[0].UpperLogarithmicLimit);

        if (ratio > quotientCurveCheckData.Points[0].LowerLogarithmicLimit && ratio < quotientCurveCheckData.Points[0].UpperLogarithmicLimit)
        {
            _logger.LogInformation("Ratio is OK");
            return true;
        }

        // Else flag all replicates in Reference level, store it, and return false
        _logger.LogInformation("Ratio is Not OK");
        for (var i = 0; i < quotientCurveCheckData.Points[0].Replicates.Count; i++)
        {
            quotientCurveCheckData.Points[0].ReplicateFlagged[i] = true;
        }
        await StoreQuotientCalibratorCheck(quotientCurveCheckData);

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="methodName"></param>
    /// <returns></returns>
    private async Task<int> GetCalibratorsQuotientIndex(string methodName)
    {
        return await _methodsService.GetMethod(methodName, m => m.CalibrationCurve.QuotientIndex);
    }

    /// <summary>
    /// Check and fit a curve from list of calibrator test orders.
    /// </summary>
    private async Task CheckAndCurveFit(Curve curve, List<CalibratorTestResult> calibratorTestResults)
    {
        var curveFitter = new CurveFitter(_curveFitterApi);
        var curveData = GetCurveData(calibratorTestResults);
        if (curveData.Responses.Length == 0)
        {
            _logger.LogInformation("Curve has no response data, skipping Curve Fit");
            return;
        }

        var methodData = await GetMethodData(curve.MethodName);
        if (methodData != null)
        {
            var alignedMethodData = AlignMethodDataWithCurveData(methodData, curveData);
            var fittedCurve = curveFitter.FitCurve(curveData, alignedMethodData);
            _logger.LogInformation(
                "Curve fit result for method {MethodName}, group {GroupId}: Fitted={CurveFittedFlag}, FlaggedPoints={FlaggedPoints}, ErrorPrecision={ErrorPrecisionFlag}, ErrorFlat={ErrorFlatCurveFlag}, ErrorNoRising={ErrorNoRisingTrendFlag}",
                curve.MethodName,
                curve.GroupId,
                fittedCurve.CurveFittedFlag,
                fittedCurve.NumberOfFlaggedPoints,
                fittedCurve.ErrorPrecisionFlag,
                fittedCurve.ErrorFlatCurveFlag,
                fittedCurve.ErrorNoRisingTrendFlag);
            MapToCurve(fittedCurve, curve);

            StoreCurveCheckResults(fittedCurve, calibratorTestResults);

            await CalculateConcentrationsAfterCurveFit(curve.CalculationType, fittedCurve, calibratorTestResults);

            // Keep the fitted result available for calculations above, but do not expose a rejected calibration as a fitted curve.
            if (HasAbortedOrLowRu(calibratorTestResults))
            {
                _logger.LogInformation("Curve has aborted or Low RU results, forcing judgment to NotOK and suppressing the fitted curve");
                curve.Judgment = CurveJudgment.NotOK;
                curve.CurveFittedFlag = false;
            }
        }
    }

    /// <summary>
    /// Retrieve curve data needed for curve fit
    /// </summary>
    /// <param name="calibratorTestResults">List of calibrator test orders</param>
    /// <returns>Curve data</returns>
    private static CurveData GetCurveData(List<CalibratorTestResult> calibratorTestResults)
    {
        var concentrations = new List<double>();
        var responses = new List<double>();

        foreach (var calibratorTestResult in calibratorTestResults)
        {
            if (calibratorTestResult.Replicates != null)
            {
                foreach (var replicate in calibratorTestResult.Replicates)
                {
                    if (IsEligibleForCurveFit(replicate))
                    {
                        concentrations.Add(calibratorTestResult.ExpectedConcentration);
                        responses.Add(replicate.Response!.Value);
                    }
                }
            }
        }

        var curveData = new CurveData
        {
            Concentrations = [.. concentrations],
            Responses = [.. responses]
        };

        return curveData;
    }

    /// <summary>
    /// Aligns method calibration data with the concentration levels retained for curve fitting.
    /// </summary>
    private static MethodData AlignMethodDataWithCurveData(MethodData methodData, CurveData curveData)
    {
        var methodPoints = methodData.Points.OrderBy(point => point.DefinedConcentration).ToList();
        var retainedConcentrations = curveData.Concentrations.Distinct().ToHashSet();
        var retainedPoints = methodPoints
            .Where(point => retainedConcentrations.Contains(point.DefinedConcentration))
            .ToList();

        if (retainedPoints.Count != retainedConcentrations.Count)
        {
            throw new InvalidOperationException("Curve data contains a concentration that is not defined by the method.");
        }

        var calculation = methodData.Calculation;
        var calibratorStartingPoint = calculation.CalibratorStartingPoint;
        var calibratorsForLeastSquareCalc = calculation.CalibratorsForLeastSquareCalc;

        if (calibratorStartingPoint > 0)
        {
            var switchConcentration = methodPoints[calibratorStartingPoint].DefinedConcentration;
            calibratorStartingPoint = Math.Min(
                retainedPoints.Count(point => point.DefinedConcentration < switchConcentration),
                retainedPoints.Count - 1);

            if (calibratorsForLeastSquareCalc > 0)
            {
                var leastSquareEndIndex = Math.Min(calibratorsForLeastSquareCalc, methodPoints.Count) - 1;
                var leastSquareEndConcentration = methodPoints[leastSquareEndIndex].DefinedConcentration;
                calibratorsForLeastSquareCalc = retainedPoints.Count(point => point.DefinedConcentration <= leastSquareEndConcentration);
            }
        }

        var alignedCalculation = new MethodCalculation
        {
            CalibratorStartingPoint = calibratorStartingPoint,
            CalibratorsForLeastSquareCalc = calibratorsForLeastSquareCalc,
            CalculationType = calculation.CalculationType,
            MethodName = calculation.MethodName,
            MinCoefficientOfCorrelation = calculation.MinCoefficientOfCorrelation,
            NormalWeight = calculation.NormalWeight,
            RobustWeight = calculation.RobustWeight,
            PredictedVarianceA = calculation.PredictedVarianceA,
            PredictedVarianceB = calculation.PredictedVarianceB
        };

        return new MethodData(alignedCalculation, methodData.Calibrator, retainedPoints);
    }

    /// <summary>
    /// Retrieve method data needed for curve fit
    /// </summary>
    /// <param name="methodName">Method name</param>
    /// <returns>Method data</returns>
    private async Task<MethodData?> GetMethodData(string methodName)
    {
        var pufMethodCalculation = await GetMethodCalculationData(methodName);
        var pufMethodCalibrator = await GetMethodCalibratorData(methodName);
        var pufPoints = await GetMethodCalibratorPoints(methodName);

        if (pufMethodCalculation != null && pufMethodCalibrator != null && pufPoints.Count > 0)
        {
            return new MethodData(pufMethodCalculation, pufMethodCalibrator, pufPoints);
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Retrieve method calculation data needed for curve fit
    /// </summary>
    /// <param name="methodName">Method name</param>
    /// <returns>Method calculation data</returns>
    private async Task<MethodCalculation?> GetMethodCalculationData(string methodName)
    {
        MethodCalculation? pufMethodCalculation = null;

        var methodCalculation = await _methodsService.GetMethod(methodName, m => m.Calculation);

        if (methodCalculation != null)
        {
            pufMethodCalculation = new MethodCalculation()
            {
                CalculationType = (int)methodCalculation.CalculationType,
                CalibratorStartingPoint = methodCalculation.CalibratorStartingPoint,
                CalibratorsForLeastSquareCalc = methodCalculation.CalibratorsForLeastSquareCalc,
                MethodName = methodName,
                MinCoefficientOfCorrelation = methodCalculation.MinCoefficientOfCorrelation,
                NormalWeight = (int)methodCalculation.NormalWeight,
                RobustWeight = (int)methodCalculation.RobustWeight,
                PredictedVarianceA = methodCalculation.PredictedVarianceA,
                PredictedVarianceB = methodCalculation.PredictedVarianceB,
            };
        }

        return pufMethodCalculation;
    }

    /// <summary>
    /// Retrieve method calibrator data needed for curve fit
    /// </summary>
    /// <param name="methodName">Method name</param>
    /// <returns>Method calibrator data</returns>
    private async Task<MethodCalibrator?> GetMethodCalibratorData(string methodName)
    {
        MethodCalibrator? pufMethodCalibrator = null;

        var methodCalibrators = await _methodsService.GetMethod(methodName, m => m.CalibrationCurve);

        if (methodCalibrators != null)
        {
            pufMethodCalibrator = new MethodCalibrator()
            {
                ExcludeLowestCalibratorFromMeanDiff = methodCalibrators.ExcludeLowestCalibratorFromMeanDiff ? 1 : 0,
                MaxBadReplicates = methodCalibrators.MaxBadReplicates,
                MethodName = methodName,
            };
        }

        return pufMethodCalibrator;
    }

    /// <summary>
    /// Retrieve method calibrator points data needed for curve fit
    /// </summary>
    /// <param name="methodName">Method name</param>
    /// <returns>Method calibrator points</returns>
    private async Task<List<MethodCalibratorPoint>> GetMethodCalibratorPoints(string methodName)
    {
        var pufPoints = await _methodsService.GetMethodCalibratorPoints(methodName,
                   p => new MethodCalibratorPoint
                   {
                       Replicates = p.Replicates,
                       IdealResponse = p.IdealResponse,
                       DeviationSDLimit = p.StdDevLimit,
                       ExpectedSD = p.ExpectedStdDev,
                       LowerResponseLimit = p.LowerResponseLimit,
                       LowerLogarithmicLimit = p.LowerLogarithmicLimit ?? 0,
                       PrecisionSDLimit = p.StdDevPrecisionLimit,
                       UpperLogarithmicLimit = p.UpperLogarithmicLimit ?? 0,
                       UpperResponseLimit = p.UpperResponseLimit,
                       CalibratorName = p.CalibratorName,
                       DefinedConcentration = p.ExpectedConcentration,
                       MethodName = methodName,
                   });

        return [.. pufPoints.OrderBy(p => p.DefinedConcentration)];
    }

    /// <summary>
    /// Check if any replicate has been aborted or has a Low RU response value.
    /// </summary>
    private static bool HasAbortedOrLowRu(List<CalibratorTestResult> calibratorTestResults)
    {
        return calibratorTestResults.Any(to => to.Replicates != null && to.Replicates.Any(r => r.IsAborted || r.IsLowResponse));
    }

    /// <summary>
    /// Determines whether a replicate can be used as curve-fit input.
    /// </summary>
    private static bool IsEligibleForCurveFit(TestReplicate replicate)
    {
        return replicate.Response.HasValue && !replicate.IsAborted && !replicate.IsLowResponse;
    }

    /// <summary>
    /// Retrieve curve from database
    /// </summary>
    /// <param name="instrumentNumber">Instrument ID</param>
    /// <param name="groupId">Group id of the group of calibrators</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Curve</returns>
    private async Task<Curve> GetCurve(string instrumentNumber, Guid groupId, CancellationToken cancellationToken)
    {
        var curve = await _dbContext.Curves.FirstAsync(c => c.InstrumentNumber == instrumentNumber && c.GroupId == groupId, cancellationToken);
        return curve;
    }

    /// <summary>
    /// Transfer data from curve fit to curve
    /// </summary>
    /// <param name="fittedCurve">Fitted curve</param>
    /// <param name="curve">Curve</param>
    private static void MapToCurve(IFittedCurve fittedCurve, Curve curve)
    {
        curve.CurveMinConcentration = fittedCurve.CurveMinConcentration;
        curve.CurveMaxConcentration = fittedCurve.CurveMaxConcentration;
        curve.CalculationParameterA = fittedCurve.CalculationParameterA;
        curve.CalculationParameterB = fittedCurve.CalculationParameterB;
        curve.CalculationParameterC = fittedCurve.CalculationParameterC;
        curve.CalculationParameterD = fittedCurve.CalculationParameterD;
        curve.CalculationParameterE = fittedCurve.CalculationParameterE;
        curve.ErrorFlatCurveFlag = fittedCurve.ErrorFlatCurveFlag;
        curve.ErrorLowerPartTooHighFlag = fittedCurve.ErrorLowerPartTooHighFlag;
        curve.ErrorNoRisingTrendFlag = fittedCurve.ErrorNoRisingTrendFlag;
        curve.ErrorPrecisionFlag = fittedCurve.ErrorPrecisionFlag;
        curve.ErrorSingleCalibratorAboveRangeFlag = fittedCurve.ErrorSingleCalibratorAboveRangeFlag;
        curve.ErrorSingleCalibratorBelowRangeFlag = fittedCurve.ErrorSingleCalibratorBelowRangeFlag;
        curve.IterationsUsed = fittedCurve.IterationsUsed;
        curve.MaxIterationsAllowed = fittedCurve.MaxIterationsAllowed;
        curve.NumberOfCalibrationPointsInZeroPointCalculation = fittedCurve.NumberOfCalibrationPointsInZeroPointCalculation;
        curve.ZeroPointCalculationCoefficientAlfa = fittedCurve.ZeroPointCalculationCoefficientAlfa;
        curve.ZeroPointCalculationCoefficientBeta = fittedCurve.ZeroPointCalculationCoefficientBeta;
        curve.ZeroPointCalculationNumberOfIntervals = fittedCurve.ZeroPointCalculationNumberOfIntervals;
        curve.ZeroPointSwitchConcentration = fittedCurve.ZeroPointSwitchConcentration;

        if (fittedCurve.CurveFittedFlag)
        {
            curve.Judgment = fittedCurve.NumberOfFlaggedPoints > 1 ? CurveJudgment.Flagged : CurveJudgment.OK;
            curve.CurveFittedFlag = true;
        }
        else
        {
            curve.Judgment = CurveJudgment.NotOK;
            curve.CurveFittedFlag = false;
        }
    }

    /// <summary>
    /// Store result of curve check after curve fit
    /// </summary>
    /// <param name="fittedCurve">Fitted curve</param>
    /// <param name="calibratorTestResults">Calibrator test orders</param>
    private static void StoreCurveCheckResults(IFittedCurve fittedCurve, List<CalibratorTestResult> calibratorTestResults)
    {
        StoreCurveCheckResultsOnReplicates(fittedCurve, calibratorTestResults);
        StoreCurveCheckResultsOnMean(fittedCurve, calibratorTestResults);
    }

    /// <summary>
    /// Store result of curve check after curve fit on replicates
    /// </summary>
    /// <param name="fittedCurve">Fitted curve</param>
    /// <param name="calibratorTestResults">Calibrator test orders</param>
    private static void StoreCurveCheckResultsOnReplicates(IFittedCurve fittedCurve, List<CalibratorTestResult> calibratorTestResults)
    {
        var nTotalReplicate = 0;

        foreach (var calibratorTestResult in calibratorTestResults)
        {
            if (calibratorTestResult.Replicates != null)
            {
                foreach (var replicate in calibratorTestResult.Replicates)
                {
                    if (replicate is CalibratorTestReplicate calibratorReplicate)
                    {
                        if (!IsEligibleForCurveFit(calibratorReplicate))
                        {
                            // Apply the same filter as GetCurveData so fitter indexes map back to the correct replicate.
                            continue;
                        }

                        if (fittedCurve.IsReplicateFlagged(nTotalReplicate))
                        {
                            calibratorReplicate.Active = false;
                            calibratorReplicate.NormDoseCheckResult = NormDoseCheckResult.NotOk;
                        }
                        else
                        {
                            calibratorReplicate.Active = true;
                            calibratorReplicate.NormDoseCheckResult = NormDoseCheckResult.Ok;
                        }
                        nTotalReplicate++;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Store result of curve check after curve fit on results (summary of replicates)
    /// </summary>
    /// <param name="fittedCurve">Fitted curve</param>
    /// <param name="calibratorTestResults">Calibrator test orders</param>
    private static void StoreCurveCheckResultsOnMean(IFittedCurve fittedCurve, List<CalibratorTestResult> calibratorTestResults)
    {
        var nCalibratorPoint = 0;

        foreach (var testResult in calibratorTestResults)
        {
            if (testResult.Replicates == null || !testResult.Replicates.Any(IsEligibleForCurveFit))
            {
                testResult.ArtificialResponse = null;
                continue;
            }

            testResult.ArtificialResponse = fittedCurve.GetArtificialResponse(nCalibratorPoint);
            nCalibratorPoint++;
        }
    }

    /// <summary>
    /// Calculate concentrations for test orders
    /// </summary>
    /// <param name="calculationType">Type of calculation requested</param>
    /// <param name="fittedCurve">Fitted curve</param>
    /// <param name="testResults">Test orders</param>
    private async Task CalculateConcentrationsAfterCurveFit(CalculationType calculationType, IFittedCurve fittedCurve, List<CalibratorTestResult> testResults)
    {
        // TODO:
        // - Handle outside curve, low RU, concentration status, isZEROMethodFlag, UseLowRUFlag, CurveControlDisabledFlag ... (See Prime...)

        foreach (var testResult in testResults)
        {
            if (testResult.Replicates is { Count: > 0 })
            {
                var methodName = testResult.MethodName;
                var testName = testResult.TestName;
                var testResultType = testResult.ResultType;
                var preDilutionFactor = testResult.PreDilutionFactor;
                var instrumentDilutionFactor = testResult.InstrumentDilutionFactor;

                foreach (var replicate in testResult.Replicates)
                {
                    if (replicate.Response != null)
                    {
                        await CalculateReplicateConcentration(replicate, calculationType, fittedCurve, preDilutionFactor, instrumentDilutionFactor, methodName, testName, testResultType);
                    }
                }
                await CalculateSummaryResult(testResult);
            }
        }
    }

    /// <summary>
    /// Calculate concentration for one replicate using given curve
    /// </summary>
    /// <param name="replicate">The replicate</param>
    /// <param name="curveId">Curve ID</param>
    /// <param name="preDilutionFactor">Pre-dilution factor</param>
    /// <param name="instrumentDilutionFactor">Instrument dilution factor</param>
    /// <param name="methodName">Method name</param>
    /// <param name="testName">Test name</param>
    /// <param name="testResultType">Test order type</param>
    /// <param name="cancellationToken"></param>
    public async Task CalculateReplicateConcentration(
        TestReplicate replicate,
        int curveId,
        int preDilutionFactor,
        int instrumentDilutionFactor,
        string methodName,
        string testName,
        TestResultType testResultType,
        CancellationToken cancellationToken)
    {
        var curveFitter = new CurveFitter(_curveFitterApi);
        await CalculateReplicateConcentration(replicate, curveId, preDilutionFactor, instrumentDilutionFactor, methodName, testName, testResultType, curveFitter, cancellationToken);
    }

    /// <summary>
    /// Calculate concentration for one replicate using given curve
    /// </summary>
    /// <param name="replicate">The replicate</param>
    /// <param name="curveId">Curve ID</param>
    /// <param name="preDilutionFactor">Pre-dilution factor</param>
    /// <param name="instrumentDilutionFactor">Instrument dilution factor</param>
    /// <param name="methodName">Method name</param>
    /// <param name="testName">Test name</param>
    /// <param name="testResultType">Test order type</param>
    /// <param name="curveFitter">Curve fitter</param>
    /// <param name="cancellationToken"></param>
    private async Task CalculateReplicateConcentration(TestReplicate replicate,
                                                       int curveId,
                                                       int preDilutionFactor,
                                                       int instrumentDilutionFactor,
                                                       string methodName,
                                                       string testName,
                                                       TestResultType testResultType,
                                                       ICurveFitter curveFitter,
                                                       CancellationToken cancellationToken)
    {
        var curve = await _dbContext.Curves.FirstOrDefaultAsync(c => c.Id == curveId, cancellationToken);
        if (curve != null && IsCurveParametersValid(curve))
        {
            var fittedCurve = GetFittedCurve(curveFitter, curve);
            await CalculateReplicateConcentration(replicate, curve.CalculationType, fittedCurve, preDilutionFactor, instrumentDilutionFactor, methodName, testName, testResultType);
        }

    }

    /// <summary>
    /// Check if the curve has all needed parameters for concentration calculation
    /// </summary>
    /// <param name="curve">Curve</param>
    /// <returns>True if all is OK</returns>
    private bool IsCurveParametersValid(Curve curve)
    {
        if (!curve.ZeroPointCalculationNumberOfIntervals.HasValue ||
            !curve.NumberOfCalibrationPointsInZeroPointCalculation.HasValue ||
            !curve.ZeroPointSwitchConcentration.HasValue ||
            !curve.ZeroPointCalculationCoefficientAlfa.HasValue ||
            !curve.ZeroPointCalculationCoefficientBeta.HasValue ||
            !curve.CurveMinConcentration.HasValue ||
            !curve.CurveMaxConcentration.HasValue ||
            !curve.CalculationParameterA.HasValue ||
            !curve.CalculationParameterB.HasValue ||
            !curve.CalculationParameterC.HasValue ||
            !curve.CalculationParameterD.HasValue ||
            !curve.CalculationParameterE.HasValue)
        {
            _logger.LogError("Missing curve data to create a fitted curve");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Prepare curve fit data and return a "fitted curve" for concentration calculation
    /// </summary>
    /// <param name="curveFitter">Curve fitter</param>
    /// <param name="curve">Curve</param>
    /// <returns>Fitted curve</returns>
    private static IFittedCurve GetFittedCurve(ICurveFitter curveFitter, Curve curve)
    {

#pragma warning disable CS8629 // Nullable value type may be null.

        var fittedCurveData = new FittedCurveData
        {
            ZeroPointCalculationNumberOfIntervals = curve.ZeroPointCalculationNumberOfIntervals.Value,
            NumberOfCalibrationPointsInZeroPointCalculation = curve.NumberOfCalibrationPointsInZeroPointCalculation.Value,
            ZeroPointSwitchConcentration = curve.ZeroPointSwitchConcentration.Value,
            ZeroPointCalculationCoefficientAlfa = curve.ZeroPointCalculationCoefficientAlfa.Value,
            ZeroPointCalculationCoefficientBeta = curve.ZeroPointCalculationCoefficientBeta.Value,
            CurveMinConcentration = curve.CurveMinConcentration.Value,
            CurveMaxConcentration = curve.CurveMaxConcentration.Value,

            CalculationParameterA = curve.CalculationParameterA.Value,
            CalculationParameterB = curve.CalculationParameterB.Value,
            CalculationParameterC = curve.CalculationParameterC.Value,
            CalculationParameterD = curve.CalculationParameterD.Value,
            CalculationParameterE = curve.CalculationParameterE.Value,
            CalculationType = curve.CalculationType switch
            {
                CalculationType.Rodbard4Par => ModelNumCommon.MdRodbard4,
                CalculationType.Rodbard5Par => ModelNumCommon.MdRodbard5,
                CalculationType.Richards => ModelNumCommon.MdRichards,
                CalculationType.Quotient => ModelNumCommon.MdQuotient,
                _ => throw new ArgumentOutOfRangeException(nameof(curve)),
            }
        };
#pragma warning restore CS8629 // Nullable value type may be null.

        var fittedCurve = curveFitter.PrepareFittedCurveForCalculation(fittedCurveData);
        fittedCurve.CurveFittedFlag = curve.CurveFittedFlag;
        fittedCurve.CurveControlDisabledFlag = false;
        if (curve.CurveControlDisabledFlag.HasValue)
        {
            fittedCurve.CurveControlDisabledFlag = curve.CurveControlDisabledFlag.Value;
        }
        fittedCurve.CurveId = curve.Id;

        return fittedCurve;
    }

    /// <summary>
    /// Calculate concentration for a replicate
    /// </summary>
    /// <param name="replicate">The replicate</param>
    /// <param name="calculationType">Type of calculation requested</param>
    /// <param name="fittedCurve">Fitted curve</param>
    /// <param name="preDilutionFactor">Pre-dilution factor</param>
    /// <param name="instrumentDilutionFactor">Instrument dilution factor</param>
    /// <param name="methodName">Method name</param>
    /// <param name="testName">Test name</param>
    /// <param name="testResultType">Test order type</param>
    private async Task CalculateReplicateConcentration(TestReplicate replicate,
                                                       CalculationType calculationType,
                                                       IFittedCurve fittedCurve,
                                                       int preDilutionFactor,
                                                       int instrumentDilutionFactor,
                                                       string methodName,
                                                       string testName,
                                                       TestResultType testResultType)
    {
        if (replicate.Response != null)
        {
            var totalDilutionFactor = preDilutionFactor * instrumentDilutionFactor;
            var useTWCF = true;
            var useIterativeAlfaBeta1Beta2Flag = false;
            var adjustmentForAlfaBeta1Beta2 = 0.0;
            double totalConcentrationFactor;

            if (testResultType is TestResultType.Calibrator or TestResultType.CurveControl)
            {
                _logger.LogInformation("Result type is Calibrator or Curve control, using ConcentrationFactor=1 and not using TWCF");
                totalConcentrationFactor = 1.0;
                useTWCF = false;
            }
            else
            {
                var concentrationFactor = await GetConcentrationFactor(methodName, testName);
                var correctionFactor = await GetCorrectionFactorFromBarcode(replicate.CarrierBarcode, methodName, testName);
                totalConcentrationFactor = concentrationFactor * correctionFactor;
                (useIterativeAlfaBeta1Beta2Flag, adjustmentForAlfaBeta1Beta2) = await GetIterativeAlfaBeta1Beta2Parameters(methodName, testName);
            }

            var response = replicate.Response.Value;
            if (useTWCF && useIterativeAlfaBeta1Beta2Flag && calculationType != CalculationType.Quotient)
            {
                _logger.LogInformation("TWCF: Calculation is enabled");
                if (replicate.CarrierBarcode != null)
                {

                    //TODO: check if article type already available with replicate data
                    var (articleType, _) = await _barcodeService.DetectArticleType(replicate.CarrierBarcode).ConfigureAwait(false);
                    var lot = (await _barcodeService.GetReagentInfo(articleType, replicate.CarrierBarcode).ConfigureAwait(false))?.Lot;

                    if (lot is EliaCalibratorWellLotInformation { IsChecksumValid: true, NoAdjustment: false } eliaCalibratorWellLot)
                    {
                        var testWellCorrectionFactor = new TestWellCorrectionFactor(_logger);
                        var adjustResult = testWellCorrectionFactor.AdjustResponseByTestWellCorrectionFactor(fittedCurve, response, replicate.CarrierBarcode, eliaCalibratorWellLot, adjustmentForAlfaBeta1Beta2);
                        if (adjustResult.Result)
                        {
                            response = adjustResult.NewResponse;
                        }
                        else
                        {
                            _logger.LogError("TWCF: Error in TWCF function, cannot calculate concentration");
                            return;
                        }
                    }
                }
            }

            await CalculateReplicateConcentration(replicate, calculationType, fittedCurve, response, totalConcentrationFactor, totalDilutionFactor, methodName, testName, testResultType);
        }
        else
        {
            replicate.ConcentrationAsText = _resultFormatter.NoDataAvailable();
        }
    }

    /// <summary>
    /// Adjust response according to carrier lot and concentration
    /// </summary>
    /// <param name="response">Original response</param>
    /// <param name="eliaCalibratorLot">A lot for a EliA Calibrator Well carrier</param>
    /// <param name="concentration">Input concentration</param>
    /// <returns>Adjusted concentration</returns>
    public double AdjustResponseAccordingToCarrierLotAndConcentration(double response, EliaCalibratorWellLotInformation eliaCalibratorLot, double concentration)
    {
        return TestWellCorrectionFactor.AdjustResponseAccordingToCarrierLotAndConcentration(response, eliaCalibratorLot, concentration);
    }


    /// <summary>
    /// Retrieve the IterativeAlfaBeta1Beta2Flag value for given method test
    /// </summary>
    /// <param name="methodName">Method name</param>
    /// <param name="testName">Test name</param>
    /// <returns>IterativeAlfaBeta1Beta2Flag value for given method test. False if not found</returns>
    private async Task<(bool, double)> GetIterativeAlfaBeta1Beta2Parameters(string methodName, string testName)
    {
        var param = await _methodsService.GetMethodTest(methodName, testName,
            mt => new
            {
                mt.IterativeAlfaBeta1Beta2Flag,
                mt.AdjustmentForAlfaBeta1Beta2,
            });

        if (param == null)
        {
            return (false, 0.0);
        }

        return (param.IterativeAlfaBeta1Beta2Flag, param.AdjustmentForAlfaBeta1Beta2);
    }

    /// <summary>
    /// Calculate concentration for one replicate
    /// </summary>
    /// <param name="replicate">The replicate</param>
    /// <param name="calculationType">Type of calculation requested</param>
    /// <param name="fittedCurve">Fitted curve</param>
    /// <param name="response">Response value for the replicate</param>
    /// <param name="totalConcentrationFactor">Total concentration factor</param>
    /// <param name="totalDilutionFactor">Total dilution factor (pre-dilution * instrument dilution)</param>
    /// <param name="methodName">Method name</param>
    /// <param name="testName">Test name</param>
    /// <param name="testResultType"></param>
    private async Task CalculateReplicateConcentration(
        TestReplicate replicate,
        CalculationType calculationType,
        IFittedCurve fittedCurve,
        double response,
        double totalConcentrationFactor,
        int totalDilutionFactor,
        string methodName,
        string testName,
        TestResultType testResultType)
    {
        var concentration = 0.0;
        var status = ConcentrationStatus.NotAvailable;

        if (!fittedCurve.CurveFittedFlag)
        {
            replicate.ConcentrationStatus = status;
            replicate.Concentration = null;
            replicate.ConcentrationAsText = _resultFormatter.NoDataAvailable();
            return;
        }

        if (calculationType == CalculationType.Quotient)
        {
            var quotientIndex = await GetCalibratorsQuotientIndex(methodName);

            (var tmpConcentration, _) = await CalculateQuotient(response, QuotientReportMode.ReportAsFraction, quotientIndex, fittedCurve.CurveId, fittedCurve.CurveFittedFlag, fittedCurve.CurveControlDisabledFlag, totalDilutionFactor);
            if (tmpConcentration.HasValue)
            {
                concentration = tmpConcentration.Value;
                status = ConcentrationStatus.Ok;
            }
        }
        else
        {
            concentration = fittedCurve.ResponseToConcentration(response, out var concentrationStatus, totalConcentrationFactor, totalDilutionFactor);
            status = concentrationStatus switch
            {
                CurveFit.ConcentrationStatus.Ok => ConcentrationStatus.Ok,
                CurveFit.ConcentrationStatus.Over => ConcentrationStatus.Over,
                CurveFit.ConcentrationStatus.Under => ConcentrationStatus.Under,
                CurveFit.ConcentrationStatus.Doubtful => ConcentrationStatus.Doubtful,
                _ => ConcentrationStatus.NotAvailable
            };
        }

        concentration = double.Min(concentration, _maxConcentrationValue);
        status = await AdjustConcentrationStatus(totalConcentrationFactor, totalDilutionFactor, methodName, testName, concentration, status);

        await UpdateConcentrationData(replicate, concentration, status, methodName, testName, fittedCurve, totalDilutionFactor, testResultType);

        if (replicate is SampleTestReplicate sampleReplicate)
        {
            await CalculateSampleSpecificResults(sampleReplicate, methodName, testName, totalDilutionFactor, fittedCurve.CurveId, fittedCurve.CurveFittedFlag, fittedCurve.CurveControlDisabledFlag);
        }
        else if (replicate is QualityControlTestReplicate qcReplicate)
        {
            await CalculateQualityControlSpecificResults(qcReplicate, methodName, testName, totalDilutionFactor, fittedCurve.CurveId, fittedCurve.CurveFittedFlag, fittedCurve.CurveControlDisabledFlag);
        }
    }

    /// <summary>
    /// Update data about the concentration
    /// </summary>
    /// <param name="replicate">Replicate</param>
    /// <param name="concentration">Concentration so far</param>
    /// <param name="concentrationStatus">Concentration status</param>
    /// <param name="methodName">method name</param>
    /// <param name="testName">Test name</param>
    /// <param name="fittedCurve">Curve used for calculation of concentration</param>
    /// <param name="totalDilutionFactor"></param>
    /// <param name="testResultType"></param>
    private async Task UpdateConcentrationData(
        TestReplicate replicate,
        double concentration,
        ConcentrationStatus concentrationStatus,
        string methodName,
        string testName,
        IFittedCurve fittedCurve,
        int totalDilutionFactor,
        TestResultType testResultType)
    {
        replicate.ConcentrationStatus = concentrationStatus;
        replicate.Concentration = concentration;

        if (replicate.Concentration == null || replicate.ConcentrationStatus == ConcentrationStatus.NotAvailable || replicate.IsLowResponse)
        {
            replicate.ConcentrationAsText = _resultFormatter.NoDataAvailable();
            return;
        }

        if (concentrationStatus == ConcentrationStatus.Under)
        {
            if (IsZeroMethod(fittedCurve))
            {
                replicate.ConcentrationStatus = ConcentrationStatus.Ok;
                replicate.Concentration = 0.0;
            }
        }

        replicate.ConcentrationAsText = await _resultFormatter.ConcentrationAsText(
            replicate.Concentration.Value, replicate.ConcentrationStatus, methodName, testName,
            totalDilutionFactor, replicate.CarrierBarcode, testResultType);
    }

    /// <summary>
    /// Check if this is a "zero method" (lowest calibrator point "almost zero")
    /// </summary>
    /// <param name="fittedCurve">Fitted curve</param>
    /// <returns>True if this is a zero method</returns>
    private static bool IsZeroMethod(IFittedCurve fittedCurve)
    {
        return fittedCurve is { CurveFittedFlag: true, CurveMinConcentration: < _concentrationLimitOn, ZeroPointCalculationNumberOfIntervals: 0 };
    }

    private async Task CalculateSampleSpecificResults(
        SampleTestReplicate sampleReplicate,
        string methodName,
        string testName,
        int totalDilutionFactor,
        int curveId,
        bool curveFittedFlag,
        bool curveControlDisabledFlag)
    {
        var reportData = await _methodsService.GetMethodTest(methodName, testName,
            mt => new
            {
                mt.ReportClassId,
                mt.CutOffConcentration,
                mt.ReportQuotient,
                mt.Method.Reporting.TextBelowCutOff,
                mt.Method.Reporting.TextAboveCutOff,
                mt.Method.CalibrationCurve.QuotientIndex,
            });
        if (reportData != null)
        {
            (sampleReplicate.Class, sampleReplicate.LowestClassFlag, sampleReplicate.HighestClassFlag) =
                await CalculateClass(sampleReplicate.Concentration, sampleReplicate.ConcentrationAsText, sampleReplicate.ConcentrationStatus, methodName, reportData.ReportClassId);

            sampleReplicate.CutOff = CalculateCutOff(sampleReplicate.Concentration, sampleReplicate.ConcentrationStatus, reportData.CutOffConcentration, reportData.TextAboveCutOff ?? "", reportData.TextBelowCutOff ?? "");
            (sampleReplicate.Quotient, sampleReplicate.QuotientAsText) = await CalculateQuotient(sampleReplicate.Response, reportData.ReportQuotient, reportData.QuotientIndex, curveId, curveFittedFlag, curveControlDisabledFlag, totalDilutionFactor);
        }
    }

    private async Task CalculateQualityControlSpecificResults(
        QualityControlTestReplicate qcReplicate,
        string methodName,
        string testName,
        int totalDilutionFactor,
        int curveId,
        bool curveFittedFlag,
        bool curveControlDisabledFlag)
    {
        var reportData = await _methodsService.GetMethodTest(methodName, testName,
            mt => new
            {
                mt.ReportClassId,
                mt.CutOffConcentration,
                mt.ReportQuotient,
                mt.Method.Reporting.TextBelowCutOff,
                mt.Method.Reporting.TextAboveCutOff,
                mt.Method.CalibrationCurve.QuotientIndex,
            });

        if (reportData != null)
        {
            (qcReplicate.Class, qcReplicate.LowestClassFlag, qcReplicate.HighestClassFlag) =
                await CalculateClass(qcReplicate.Concentration, qcReplicate.ConcentrationAsText, qcReplicate.ConcentrationStatus, methodName, reportData.ReportClassId);

            qcReplicate.CutOff = CalculateCutOff(qcReplicate.Concentration, qcReplicate.ConcentrationStatus, reportData.CutOffConcentration, reportData.TextAboveCutOff ?? "", reportData.TextBelowCutOff ?? "");
            (qcReplicate.Quotient, qcReplicate.QuotientAsText) = await CalculateQuotient(qcReplicate.Response, reportData.ReportQuotient, reportData.QuotientIndex, curveId, curveFittedFlag, curveControlDisabledFlag, totalDilutionFactor);
        }
    }

    private async Task<(string ClassText, bool LowestClass, bool HighestClass)> CalculateClass(
        double? concentration, string? concentrationAsText, ConcentrationStatus concentrationStatus, string methodName, int reportClassTable)
    {
        var classText = _resultFormatter.NoDataAvailable();
        var lowestClass = false;
        var highestClass = false;

        if (reportClassTable <= 0)
        {
            return (classText, lowestClass, highestClass);
        }

        if (concentrationStatus == ConcentrationStatus.NotAvailable ||
            concentrationStatus == ConcentrationStatus.Doubtful ||
            concentration == null ||
            string.IsNullOrEmpty(concentrationAsText) ||
            concentrationAsText == _resultFormatter.NoDataAvailable())
        {
            return (classText, lowestClass, highestClass);
        }

        var classTableData = await _methodsService.GetMethod(
            methodName,
            m =>
            new
            {
                TableBelow = m.ClassTableBelows.FirstOrDefault(b => b.ClassTableId == reportClassTable),
                TablesAbove = m.ClassTables.Where(a => a.ClassTableId == reportClassTable).OrderBy(b => b.ClassTableRowId).ToList(),
            });

        if (classTableData == null || classTableData.TablesAbove.Count == 0)
        {
            return (classText, lowestClass, highestClass);
        }

        if (concentrationStatus == ConcentrationStatus.Under)
        {
            classText = classTableData.TableBelow?.TextBelowConcentration;
            lowestClass = true;
        }
        else if (concentrationStatus == ConcentrationStatus.Over)
        {
            classText = classTableData.TablesAbove.Last().TextAboveConcentration;
            highestClass = true;
        }
        else if (concentrationStatus == ConcentrationStatus.Ok)
        {
            classText = classTableData.TableBelow?.TextBelowConcentration;
            lowestClass = true;
            highestClass = false;

            var useGreaterThan = false;

            // Special for EliA with only one limit
            if (classTableData.TablesAbove.Count == 1)
            {
                if (await GetMethodAssayTechnology(methodName) == AssayTechnology.EliA)
                {
                    useGreaterThan = true;
                }
            }

            foreach (var aboveTable in classTableData.TablesAbove)
            {
                highestClass = false;
                if (useGreaterThan)
                {
                    if (concentration > aboveTable.Concentration)
                    {
                        classText = aboveTable.TextAboveConcentration;
                        lowestClass = false;
                        highestClass = true;
                    }
                }
                else
                {
                    if (concentration >= aboveTable.Concentration)
                    {
                        classText = aboveTable.TextAboveConcentration;
                        lowestClass = false;
                        highestClass = true;
                    }
                }
                useGreaterThan = false;
            }
        }

        return (classText ?? _resultFormatter.NoDataAvailable(), lowestClass, highestClass);
    }

    private async Task<AssayTechnology> GetMethodAssayTechnology(string methodName)
    {
        return await _methodsService.GetMethod(methodName, m => m.Technology);
    }

    private string CalculateCutOff(double? concentration, ConcentrationStatus concentrationStatus, double? cutOffConcentration, string textAboveCutOff, string textBelowCutOff)
    {
        var cutOffText = _resultFormatter.NoDataAvailable();

        if (concentration == null || cutOffConcentration == null ||
            concentrationStatus == ConcentrationStatus.NotAvailable ||
            concentrationStatus == ConcentrationStatus.Doubtful)
        {
            return cutOffText;
        }

        if (concentration.Value >= cutOffConcentration.Value)
        {
            cutOffText = textAboveCutOff;
        }
        else
        {
            cutOffText = textBelowCutOff;
        }

        return cutOffText;
    }

    private async Task<(double? Quotient, string? QuotientAsText)> CalculateQuotient(double? response, QuotientReportMode reportQuotient, int quotientIndex, int curveId, bool curveFittedFlag, bool curveControlDisabledFlag, int totalDilutionFactor, double? referenceResponse = null)
    {
        (double? Quotient, string? QuotientAsText) result = (null, _resultFormatter.NoDataAvailable());

        if (!curveFittedFlag)
        {
            _logger.LogInformation("Curve is not fitted when calculating quotient");
            return result;
        }

        if (curveControlDisabledFlag)
        {
            _logger.LogInformation("Quotient calculation disabled by curve controls");
            return result;
        }

        if (response == null)
        {
            _logger.LogInformation("Response is not set when calculating quotient");
            return result;
        }

        var respDiluted = response.Value * totalDilutionFactor;
        if (quotientIndex > 0)
        {
            referenceResponse ??= await GetResponseForLevelIndex(curveId, quotientIndex);

            if (referenceResponse != null && referenceResponse.Value != 0.0)
            {
                result.Quotient = respDiluted / referenceResponse.Value;
            }
        }

        if (result.Quotient != null)
        {
            result.QuotientAsText = _resultFormatter.QuotientAsText(result.Quotient.Value, reportQuotient == QuotientReportMode.ReportAsPercent);
        }

        return result;
    }

    private async Task<double?> GetResponseForLevelIndex(int curveId, int quotientIndex)
    {
        var skip = quotientIndex - 1;

        return await _dbContext.Curves
            .Where(c => c.Id == curveId)
            .SelectMany(c => c.TestResults!)
            .OrderBy(r => r.ExpectedConcentration)
            .Select(r => r.MeanResponse)
            .Skip(skip)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Update concentration status if needed to according to special setting on the test or method
    /// </summary>
    /// <param name="totalConcentrationFactor">Total concentration factor</param>
    /// <param name="totalDilutionFactor">Total dilution factor (pre-dilution * instrument dilution)</param>
    /// <param name="methodName">Method name</param>
    /// <param name="testName">Test name</param>
    /// <param name="concentration">Calculated concentration</param>
    /// <param name="concentrationStatus">Original concentration status</param>
    /// <returns>Updated concentration status</returns>
    private async Task<ConcentrationStatus> AdjustConcentrationStatus(double totalConcentrationFactor, int totalDilutionFactor, string methodName, string testName, double concentration, ConcentrationStatus concentrationStatus)
    {
        if (concentrationStatus == ConcentrationStatus.NotAvailable)
        {
            return ConcentrationStatus.NotAvailable;
        }

        var minConcentrationToReport = _concentrationLimitOn;
        var maxConcentrationToReport = _maxConcentrationValue;
        var minConcentrationToReportDetectionLimit = _concentrationLimitOn;
        var reportConcentrationBelowCurveFlag = false;
        var reportConcentrationAboveCurveFlag = false;

        var limitsData = await _methodsService.GetMethodTest(methodName, testName,
            mt => new
            {
                mt.Method.Reporting.MinConcentrationToReport,
                mt.HighestConcentrationToReport,
                LowerDetectionLimitTetris = mt.LowerDetectionLimit,
                mt.ReportConcentrationBelowCurveFlag,
                mt.ReportConcentrationAboveCurveFlag,
            });

        if (limitsData != null)
        {
            minConcentrationToReport = limitsData.MinConcentrationToReport;
            maxConcentrationToReport = limitsData.HighestConcentrationToReport ?? 0;
            minConcentrationToReportDetectionLimit = limitsData.LowerDetectionLimitTetris;
            reportConcentrationBelowCurveFlag = limitsData.ReportConcentrationBelowCurveFlag;
            reportConcentrationAboveCurveFlag = limitsData.ReportConcentrationAboveCurveFlag;
        }
        else
        {
            _logger.LogError("Missing calculation parameters for {MethodName}, {TestName}. Will use default values", methodName, testName);
        }

        var concRaw = concentration;
        if (totalDilutionFactor * totalConcentrationFactor > 0)
        {
            concRaw = concentration / (totalDilutionFactor * totalConcentrationFactor);
        }
        if (minConcentrationToReport > _concentrationLimitOn && concRaw < minConcentrationToReport)
        {
            concentrationStatus = ConcentrationStatus.Under;
        }
        if (maxConcentrationToReport > _concentrationLimitOn && concentration > maxConcentrationToReport)
        {
            concentrationStatus = ConcentrationStatus.Over;
        }
        if (minConcentrationToReportDetectionLimit > _concentrationLimitOn && concentration < minConcentrationToReportDetectionLimit)
        {
            concentrationStatus = ConcentrationStatus.Under;
        }
        if (reportConcentrationBelowCurveFlag && concentration >= 0.01 && concentrationStatus == ConcentrationStatus.Under)
        {
            concentrationStatus = ConcentrationStatus.Ok;
        }
        if (reportConcentrationAboveCurveFlag && concentrationStatus == ConcentrationStatus.Over)
        {
            concentrationStatus = ConcentrationStatus.Ok;
        }

        return concentrationStatus;
    }

    /// <summary>
    /// Return correction factor from carrier barcode
    /// </summary>
    /// <param name="carrierBarcode">Carrier barcode</param>
    /// <param name="methodName">Method name</param>
    /// <param name="testName">Test name</param>
    /// <returns>Correction factor</returns>
    private async Task<double> GetCorrectionFactorFromBarcode(string? carrierBarcode, string methodName, string testName)
    {
        const double noCorrection = 1.0;

        if (string.IsNullOrEmpty(carrierBarcode))
        {
            return noCorrection;
        }

        var useLotSpecificCode = await _methodsService.GetMethodTest(methodName, testName, t => t.UseLotSpecificCode);

        if (!useLotSpecificCode)
        {
            return noCorrection;
        }

        var (articleType, _) = await _barcodeService.DetectArticleType(carrierBarcode).ConfigureAwait(false);
        var reagentInfo = await _barcodeService.GetReagentInfo(articleType, carrierBarcode).ConfigureAwait(false);
        var lot = reagentInfo?.Lot;

        var result = (lot as EliaWellLotInformation)?.CorrectionFactor ?? noCorrection;
        if (result == 0.0)
        {
            result = noCorrection;
        }

        return result;
    }

    /// <summary>
    /// Get concentration factor, from database, for a method/test
    /// </summary>
    /// <param name="methodName">Method name</param>
    /// <param name="testName">Test name</param>
    /// <returns>Concentration factor</returns>
    private async Task<double> GetConcentrationFactor(string methodName, string testName)
    {
        var result = await _methodsService.GetMethodTest(methodName, testName, t => t.ConcentrationFactor);

        return result;
    }

    public async Task CalculateSummaryResult(TestResult result)
    {
        if (result.Replicates == null || result.Replicates.Count == 0)
        {
            return;
        }

        CalculateMeanResponse(result);
        await CalculateSummaryResultConcentration(result, result.Replicates);

        switch (result)
        {
            case SampleTestResult sampleTestResult:
                result.AddDomainEvent(new TestResultUpdatedEvent(result));
                await CalculateSampleSummaryResult(sampleTestResult, result.Replicates, result.MethodName, result.TestName);
                break;
            case CurveControlTestResult curveControlTestResult:
                CalculateCurveControlSummaryResult(curveControlTestResult, result.Replicates);
                break;
            case CalibratorTestResult calibratorTestResult:
                CalculateCalibratorSummaryResult(calibratorTestResult, result.Replicates);
                break;
            case QualityControlTestResult qualityControlTestResult:
                await CalculateQualityControlSummaryResult(qualityControlTestResult, result.Replicates, result.MethodName, result.TestName);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private static void CalculateCalibratorSummaryResult(CalibratorTestResult calibratorTestResult, IList<TestReplicate> replicates)
    {
        var calibratorReplicates = replicates.OfType<CalibratorTestReplicate>().ToList();

        if (calibratorReplicates.Count == 0)
        {
            calibratorTestResult.NormDoseCheckSummary = NormDoseCheckSummary.NotDefined;
            return;
        }

        var replicatesWithIssuesCount = calibratorReplicates.Count(r => r.NormDoseCheckResult != NormDoseCheckResult.Ok);

        calibratorTestResult.NormDoseCheckSummary = replicatesWithIssuesCount switch
        {
            0 => NormDoseCheckSummary.OK,
            var count when count == calibratorReplicates.Count => NormDoseCheckSummary.AllReplRemoved,
            _ => NormDoseCheckSummary.SomeReplRemoved
        };
    }

    private async Task CalculateSampleSummaryResult(SampleTestResult result, IList<TestReplicate> replicates, string methodName, string testName)
    {
        CheckLowestHighestClass(result, replicates);

        var reportData = await _methodsService.GetMethodTest(methodName, testName,
            mt => new
            {
                mt.ReportClassId,
                mt.ReportCutOff,
                mt.CutOffConcentration,
                mt.ReportQuotient,
                mt.Method.Reporting.TextBelowCutOff,
                mt.Method.Reporting.TextAboveCutOff,
                mt.Method.CalibrationCurve.QuotientIndex,
            });

        if (reportData != null)
        {
            result.MeanClass = await CalculateSampleSummaryResultClass(result, methodName, reportData.ReportClassId);
            CalculateSampleSummaryResultQuotient(result, replicates, reportData.ReportQuotient);
            result.MeanCutOff = CalculateCutOff(result.MeanConcentration, result.MeanConcentrationStatus, reportData.CutOffConcentration, reportData.TextAboveCutOff ?? "", reportData.TextBelowCutOff ?? "");
        }
    }

    private static void CheckLowestHighestClass(SampleTestResult result, IList<TestReplicate> replicates)
    {
        result.LowestClassFlag = true;
        result.HighestClassFlag = true;

        foreach (var replicate in replicates)
        {
            if (replicate is SampleTestReplicate sampleReplicate)
            {
                if (!sampleReplicate.LowestClassFlag)
                {
                    result.LowestClassFlag = false;
                }
                if (!sampleReplicate.HighestClassFlag)
                {
                    result.HighestClassFlag = false;
                }
            }
        }
    }

    private async Task<string> CalculateSampleSummaryResultClass(SampleTestResult result, string methodName, int reportClassTable)
    {
        var classText = _resultFormatter.NoDataAvailable();

        if (reportClassTable <= 0 || result.MeanConcentration == null)
        {
            return classText;
        }

        if (result.LowestClassFlag)
        {
            classText = await _methodsService.GetMethod(
                methodName,
                m => m.ClassTableBelows.Where(x => x.ClassTableId == reportClassTable).Select(c => c.TextBelowConcentration).First()) ?? _resultFormatter.NoDataAvailable();
        }
        else if (result.HighestClassFlag)
        {
            classText = await _methodsService.GetMethod(
                methodName,
                m => m.ClassTables.Where(x => x.ClassTableId == reportClassTable).OrderBy(x => x.ClassTableRowId).Select(c => c.TextAboveConcentration).Last()) ?? _resultFormatter.NoDataAvailable();
        }
        else
        {
            (classText, _, _) =
                await CalculateClass(result.MeanConcentration, result.MeanConcentrationAsText, result.MeanConcentrationStatus, methodName, reportClassTable);
        }

        return classText;
    }

    private void CalculateSampleSummaryResultQuotient(SampleTestResult sampleTestResult, IList<TestReplicate> replicates, QuotientReportMode reportQuotient)
    {
        var nQuotientReplicates = 0;
        var quotientSum = 0.0;

        foreach (var replicate in replicates)
        {
            if (replicate is SampleTestReplicate sampleReplicate)
            {
                if (sampleReplicate is { Quotient: not null, Active: true } && !replicate.IsAborted && !replicate.IsLowResponse)
                {
                    nQuotientReplicates++;
                    quotientSum += sampleReplicate.Quotient.Value;
                }
            }
        }

        if (nQuotientReplicates > 0)
        {
            sampleTestResult.MeanQuotient = quotientSum / nQuotientReplicates;
            sampleTestResult.MeanQuotientAsText = _resultFormatter.QuotientAsText(sampleTestResult.MeanQuotient.Value, reportQuotient == QuotientReportMode.ReportAsPercent);
        }
        else
        {
            sampleTestResult.MeanQuotientAsText = _resultFormatter.NoDataAvailable();
        }
    }

    private static void CalculateCurveControlSummaryResult(CurveControlTestResult curveControlTestResult, IList<TestReplicate> replicates)
    {
        // Nothing needed... (judgment handled when "group completed" message is received)
        _ = curveControlTestResult;
        _ = replicates;
    }

    private async Task CalculateQualityControlSummaryResult(QualityControlTestResult qcTestResult, IList<TestReplicate> replicates, string methodName, string testName)
    {
        CheckLowestHighestClass(qcTestResult, replicates);

        var reportData = await _methodsService.GetMethodTest(methodName, testName,
            mt => new
            {
                mt.ReportClassId,
                mt.CutOffConcentration,
                mt.ReportQuotient,
                mt.Method.Reporting.TextBelowCutOff,
                mt.Method.Reporting.TextAboveCutOff,
            });

        if (reportData != null)
        {
            qcTestResult.MeanClass = await CalculateQualityControlSummaryResultClass(qcTestResult, methodName, reportData.ReportClassId);
            CalculateQualityControlSummaryResultQuotient(qcTestResult, replicates, reportData.ReportQuotient);
            qcTestResult.MeanCutOff = CalculateCutOff(qcTestResult.MeanConcentration, qcTestResult.MeanConcentrationStatus, reportData.CutOffConcentration, reportData.TextAboveCutOff ?? "", reportData.TextBelowCutOff ?? "");
        }
    }

    private static void CheckLowestHighestClass(QualityControlTestResult qcTestResult, IList<TestReplicate> replicates)
    {
        qcTestResult.LowestClassFlag = true;
        qcTestResult.HighestClassFlag = true;

        foreach (var replicate in replicates)
        {
            if (replicate is QualityControlTestReplicate qcReplicate)
            {
                if (!qcReplicate.LowestClassFlag)
                {
                    qcTestResult.LowestClassFlag = false;
                }
                if (!qcReplicate.HighestClassFlag)
                {
                    qcTestResult.HighestClassFlag = false;
                }
            }
        }
    }

    private async Task<string> CalculateQualityControlSummaryResultClass(QualityControlTestResult result, string methodName, int reportClassTable)
    {
        var classText = _resultFormatter.NoDataAvailable();

        if (reportClassTable <= 0 || result.MeanConcentration == null)
        {
            return classText;
        }

        if (result.LowestClassFlag)
        {
            classText = await _methodsService.GetMethod(
                methodName,
                m => m.ClassTableBelows.Where(x => x.ClassTableId == reportClassTable).Select(c => c.TextBelowConcentration).First()) ?? _resultFormatter.NoDataAvailable();
        }
        else if (result.HighestClassFlag)
        {
            classText = await _methodsService.GetMethod(
                methodName,
                m => m.ClassTables.Where(x => x.ClassTableId == reportClassTable).OrderBy(x => x.ClassTableRowId).Select(c => c.TextAboveConcentration).Last()) ?? _resultFormatter.NoDataAvailable();
        }
        else
        {
            (classText, _, _) =
                await CalculateClass(result.MeanConcentration, result.MeanConcentrationAsText, result.MeanConcentrationStatus, methodName, reportClassTable);
        }

        return classText;
    }

    private void CalculateQualityControlSummaryResultQuotient(QualityControlTestResult sampleTestResult, IList<TestReplicate> replicates, QuotientReportMode reportQuotient)
    {
        var nQuotientReplicates = 0;
        var quotientSum = 0.0;

        foreach (var replicate in replicates)
        {
            if (replicate is QualityControlTestReplicate qcReplicate)
            {
                if (qcReplicate.Quotient.HasValue && qcReplicate.Active && !replicate.IsAborted)
                {
                    nQuotientReplicates++;
                    quotientSum += qcReplicate.Quotient.Value;
                }
            }
        }

        if (nQuotientReplicates > 0)
        {
            sampleTestResult.MeanQuotient = quotientSum / nQuotientReplicates;
            sampleTestResult.MeanQuotientAsText = _resultFormatter.QuotientAsText(sampleTestResult.MeanQuotient.Value, reportQuotient == QuotientReportMode.ReportAsPercent);
        }
        else
        {
            sampleTestResult.MeanQuotientAsText = _resultFormatter.NoDataAvailable();
        }
    }

    public IList<CalculatedPointDto> CalculateCurvePoints(Curve curve, double maxConcentration, double minConcentration, bool isLogScaleX)
    {
        IEnumerable<double> points;

        // create 400 points
        if (isLogScaleX)
        {
            if (minConcentration <= 0)
            {
                minConcentration = 0.001;
            }

            var maxExponent = Math.Log(maxConcentration);
            var minExponent = Math.Log(minConcentration);

            points = Enumerable.Range(0, 400)
                .Select(i => Math.Exp(minExponent + ((maxExponent - minExponent) * ((double)i / (400 - 1)))));
        }
        else
        {
            points = Enumerable.Range(0, 400)
                .Select(i => minConcentration + ((maxConcentration - minConcentration) * ((double)i / (400 - 1))));
        }

        //initialize curve points 
        var calculatedCurve = new List<CalculatedPointDto>();

        if (IsCurveParametersValid(curve) && curve.CalculationType != CalculationType.Quotient)
        {
            var curveFitter = new CurveFitter(_curveFitterApi);
            var fittedCurve = GetFittedCurve(curveFitter, curve);

            //Calculate response for each point
            calculatedCurve.AddRange(from concentration in points
                                     let response = fittedCurve.ConcentrationToResponse(concentration)
                                     select new CalculatedPointDto
                                     {
                                         Concentration = concentration,
                                         Response = response
                                     });
        }

        return calculatedCurve;
    }

    private async Task CalculateSummaryResultConcentration(TestResult result, IList<TestReplicate> replicates)
    {
        result.MeanConcentrationStatus = ConcentrationStatus.NotAvailable;
        var calculator = new StatisticsCalculator();
        var carrierBarcode = string.Empty;
        var anyLowResponse = false;

        foreach (var replicate in replicates.Where(x => x.Active))
        {
            if (string.IsNullOrEmpty(carrierBarcode) && !string.IsNullOrEmpty(replicate.CarrierBarcode))
            {
                carrierBarcode = replicate.CarrierBarcode;
            }

            if (replicate is { Concentration: not null, Active: true, IsAborted: false, IsLowResponse: false })
            {
                calculator.Add(replicate.Concentration.Value);
            }

            if (replicate.IsLowResponse)
            {
                anyLowResponse = true;
            }

            if (replicate.ConcentrationStatus == ConcentrationStatus.NotAvailable || result.MeanConcentrationStatus == ConcentrationStatus.Doubtful)
            {
                continue;
            }

            if (result.MeanConcentrationStatus == ConcentrationStatus.NotAvailable)
            {
                result.MeanConcentrationStatus = replicate.ConcentrationStatus;
            }
            else if (result.MeanConcentrationStatus != replicate.ConcentrationStatus)
            {
                result.MeanConcentrationStatus = ConcentrationStatus.Doubtful;
            }
        }

        if (calculator.NumberOfData > 0 && !anyLowResponse)
        {
            result.MeanConcentration = calculator.Mean;
            result.PercentCvConcentration = calculator.CV;

            result.MeanConcentrationAsText = await _resultFormatter.ConcentrationAsText(
                result.MeanConcentration.Value, result.MeanConcentrationStatus, result.MethodName, result.TestName,
                result.InstrumentDilutionFactor * result.PreDilutionFactor,
                carrierBarcode, result.ResultType);
        }
        else
        {
            result.MeanConcentrationAsText = _resultFormatter.NoDataAvailable();
        }
    }

    /// <summary>
    /// Helper class with data used when checking calibrators and having quotient as calculation type
    /// </summary>
    private class QuotientCurveCheck
    {
        public List<CurvePoint> Points { get; } = [];

        public static async Task<QuotientCurveCheck> Init(IMethodsService methodsService, string methodName, List<CalibratorTestResult> testResults)
        {
            var result = new QuotientCurveCheck();
            var calibratorPoints = await methodsService.GetMethodCalibratorPoints(
                methodName,
                c => new
                {
                    ExpectedSD = c.ExpectedStdDev,
                    PrecisionSDLimit = c.StdDevPrecisionLimit,
                    c.LowerLogarithmicLimit,
                    c.UpperLogarithmicLimit,
                    c.LowerResponseLimit,
                    c.UpperResponseLimit,
                    DefinedConcentration = c.ExpectedConcentration,
                });
            foreach (var point in calibratorPoints.OrderBy(c => c.DefinedConcentration))
            {
                var curvePoint = new CurvePoint()
                {
                    ExpectedSD = point.ExpectedSD,
                    PrecisionSDLimit = point.PrecisionSDLimit,
                    LowerLogarithmicLimit = point.LowerLogarithmicLimit,
                    UpperLogarithmicLimit = point.UpperLogarithmicLimit,
                    LowerResponseLimit = point.LowerResponseLimit,
                    UpperResponseLimit = point.UpperResponseLimit,
                    DefinedConcentration = point.DefinedConcentration,
                };
                var testResult = testResults.FirstOrDefault(t => Math.Abs(t.ExpectedConcentration - point.DefinedConcentration) < 0.0000001);
                if (testResult is { Replicates: not null })
                {
                    foreach (var replicate in testResult.Replicates.Cast<CalibratorTestReplicate>())
                    {
                        curvePoint.Replicates.Add(replicate);
                    }
                    result.Points.Add(curvePoint);
                }
            }

            return result;
        }


        /// <summary>
        /// Helper's help class ;-)
        /// </summary>
        public class CurvePoint
        {
            public double ExpectedSD { get; init; }
            public double PrecisionSDLimit { get; init; }
            public double? LowerLogarithmicLimit { get; init; }
            public double? UpperLogarithmicLimit { get; init; }
            public double LowerResponseLimit { get; init; }
            public double UpperResponseLimit { get; init; }
            public double DefinedConcentration { get; init; }
            public List<CalibratorTestReplicate> Replicates { get; } = [];
            public List<bool> ReplicateFlagged { get; } = [];
        }
    }
}
