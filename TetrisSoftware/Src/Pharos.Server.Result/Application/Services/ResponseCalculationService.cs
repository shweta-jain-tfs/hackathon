using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Result.Application.Commands;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Services;

internal class ResponseCalculationService(
    IResultDbContext dbContext,
    IMethodsService methodsService,
    IReagentBarcodeService reagentBarcodeService,
    IResultCalculationService resultCalculationService,
    IResultFormatter resultFormatter,
    ILogger<ResponseCalculationService> logger) : IResponseCalculationService
{
    /// <summary>
    /// This is a "magic" fluorometer quotient (depending on the model). Probably, it's needed to compare RU values across different fluorometer models.
    /// </summary>
    public const double FluorometerQuotient = 25.0;

    private const double _lowResponseLimit = 5.0;

    public Task<Blank?> GetValidReagentBlank(string instrumentNumber, string devSolutionBarcode, int devSolutionCellPosition, BlankType blankType)
    {
        var query = from b in dbContext.Blanks
                    where b.InstrumentNumber == instrumentNumber && b.DevSolutionBarcode == devSolutionBarcode && b.DevSolutionCellPosition == devSolutionCellPosition && b.BlankType == blankType && b.ResultOk
                    orderby b.MeasurementTime descending, b.Id descending
                    select b;

        return query.FirstOrDefaultAsync();
    }

    public async Task CalculateResponseForTestReplicate(TestReplicate replicate, Blank blank, TestResultType testOrderType, string methodName, string calibratorCurveControlName)
    {
        if (replicate.FluorometerValue == null)
        {
            throw new InvalidOperationException("Test replicate has no fluorometer value");
        }

        var responseValue = (replicate.FluorometerValue - blank.MeanVoltage) * FluorometerQuotient;
        var finalResponse = responseValue;

        // Adjust response according to EliA calibrator well carrier barcode...
        if (testOrderType == TestResultType.Calibrator || testOrderType == TestResultType.CurveControl)
        {
            finalResponse = await AdjustCalibratorCurveControlResponse(responseValue, testOrderType, methodName, calibratorCurveControlName, replicate.CarrierBarcode ?? "");
        }

        if (finalResponse.HasValue)
        {
            finalResponse = Math.Max(finalResponse.Value, 1.0);
        }
        replicate.Response = finalResponse;
        if (replicate is CalibratorTestReplicate calibrator)
        {
            calibrator.CalibratorOriginalResponse = finalResponse ?? 0;
        }
        replicate.ResponseFromInstrument = responseValue;
        replicate.IsLowResponse = await IsLowResponse(finalResponse, testOrderType, methodName, calibratorCurveControlName);
        replicate.UsedBlankId = blank.Id;

        if (replicate is { Response: not null, IsLowResponse: false, IsAborted: false })
        {
            replicate.ResponseAsText = resultFormatter.ResponseAsText(replicate.Response.Value);
        }
        else if (replicate.IsLowResponse)
        {
            replicate.ResponseAsText = resultFormatter.LowRU();
        }
        else
        {
            replicate.ResponseAsText = resultFormatter.NoDataAvailable();
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
            result.MeanResponseAsText = resultFormatter.ResponseAsText(result.MeanResponse.Value);
            result.PercentCvResponse = calcResp.CV;
        }
        else if (anyLowResponse)
        {
            result.MeanResponseAsText = resultFormatter.LowRU();
        }
        else
        {
            result.MeanResponseAsText = resultFormatter.NoDataAvailable();
        }

    }

    private async Task<bool> IsLowResponse(double? response, TestResultType testOrderType, string methodName, string calibratorCurveControlName)
    {
        if (testOrderType == TestResultType.QualityControl)
        {
            return false;
        }

        if (testOrderType == TestResultType.Calibrator)
        {
            var calibratorInfo = await methodsService.GetMethodCalibratorPoint(methodName, calibratorCurveControlName,
                cal => new
                {
                    DefinedConcentration = cal.ExpectedConcentration,
                    ReportLowRUForZeroCalibratorsFlag = cal.CalibrationCurve.ReportLowRuForZeroCalibrators,
                });
            if (calibratorInfo != null && calibratorInfo.DefinedConcentration < 0.005 && !calibratorInfo.ReportLowRUForZeroCalibratorsFlag)
            {
                return false;
            }
        }

        return response != null && response < _lowResponseLimit;
    }


    /// <summary>
    /// Return an adjust response value for a calibrator/curve control replicate
    /// </summary>
    /// <param name="response">Response value</param>
    /// <param name="testOrderType">Calibrators or Curve controls</param>
    /// <param name="methodName">Method name</param>
    /// <param name="calibratorCurveControlName">Calibrator/Curve control name</param>
    /// <param name="carrierBarcode">Carrier barcode</param>
    /// <returns>Adjusted response</returns>
    private async Task<double?> AdjustCalibratorCurveControlResponse(double? response, TestResultType testOrderType, string methodName, string calibratorCurveControlName, string carrierBarcode)
    {
        if (response == null || string.IsNullOrEmpty(carrierBarcode))
        {
            return response;
        }

        var (articleType, _) = await reagentBarcodeService.DetectArticleType(carrierBarcode).ConfigureAwait(false);
        var lot = (await reagentBarcodeService.GetReagentInfo(articleType, carrierBarcode).ConfigureAwait(false))?.Lot;
        if (lot is not EliaCalibratorWellLotInformation eliaCalibratorLot)
        {
            return response;
        }

        if (!eliaCalibratorLot.IsChecksumValid)
        {
            logger.LogInformation("Adjusting RU for calibrator/CC: Invalid barcode {Barcode}", carrierBarcode);
            return response;
        }

        if (eliaCalibratorLot.NoAdjustment)
        {
            logger.LogInformation("Adjusting RU for calibrator/CC: code is default, no adjustment");
            return response;
        }

        double definedConcentration;
        double adjustmentForAlphaBeta1Beta2;

        if (testOrderType == TestResultType.Calibrator)
        {
            var methodCalibratorPointData = await methodsService.GetMethodCalibratorPoint(methodName, calibratorCurveControlName,
                p => new
                {
                    DefinedConcentration = p.ExpectedConcentration,
                    p.AdjustmentForAlfaBeta1Beta2
                });

            if (methodCalibratorPointData == null)
            {
                return response;
            }

            definedConcentration = methodCalibratorPointData.DefinedConcentration;
            adjustmentForAlphaBeta1Beta2 = methodCalibratorPointData.AdjustmentForAlfaBeta1Beta2;
        }
        else if (testOrderType == TestResultType.CurveControl)
        {
            var methodCurveControlData = await methodsService.GetMethod(methodName, m => m.CurveControls.Where(cc => cc.CurveControlName == calibratorCurveControlName).Select(
                cc => new
                {
                    DefinedConcentration = cc.ExpectedConcentration,
                    cc.AdjustmentForAlfaBeta1Beta2,
                }).First());

            if (methodCurveControlData == null)
            {
                return response;
            }

            definedConcentration = methodCurveControlData.DefinedConcentration;
            adjustmentForAlphaBeta1Beta2 = methodCurveControlData.AdjustmentForAlfaBeta1Beta2;
        }
        else
        {
            return response;
        }

        var concentration = definedConcentration * adjustmentForAlphaBeta1Beta2;
        const double nearZero = 0.0005;
        if (concentration < nearZero)
        {
            logger.LogInformation("Adjusting RU for calibrator/CC: Concentration is zero, no adjustment");
            return response;

        }

        var newResponse = resultCalculationService.AdjustResponseAccordingToCarrierLotAndConcentration(response.Value, eliaCalibratorLot, concentration);

        logger.LogInformation("Adjusting RU for calibrator/CC: {Response} -> {Result} ({CarrierBarcode})", response, newResponse, carrierBarcode);
        return newResponse;
    }

}
