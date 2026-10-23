using Microsoft.Extensions.Options;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Application.Settings.Result;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Services;

internal class ResultFormatter(
    IMethodsService methodsService,
    IReagentBarcodeService barcodeService,
    IOptionsSnapshot<ResultSettings> resultSetting) : IResultFormatter
{
    private const double _minResponse = 0;
    private const double _maxResponse = 99999;
    private const double _maxQuotient = 99999;
    private const double _concentrationLimitOn = 0.005;
    private const double _absoluteConcentrationLimit = 0.01;

    public async Task<string> ConcentrationAsText(
        double concentration,
        ConcentrationStatus concentrationStatus,
        string methodName, string testName,
        int totalDilutionFactor,
        string? carrierBarcode,
        TestResultType resultType)
    {
        var methodReporting = await methodsService.GetMethod(methodName, m => m.Reporting) ?? throw new ArgumentException($"Unknown method name {methodName}");
        var methodTest = await methodsService.GetMethodTest(methodName, testName, t => t);
        var concentrationAdjustedFlag = GetConcentrationAdjustedFlag(methodTest, totalDilutionFactor);

        var zeroMethodFlag = await IsZeroMethod(methodName);
        var highestCalibrator = await GetMaxExpectedConcentration(methodName);
        var useTextBelowAboveFlag = resultType != TestResultType.Calibrator;
        var totalAdjustmentFactor = await GetTotalAdjustmentFactor(methodTest, totalDilutionFactor, carrierBarcode, resultType);

        return ConcentrationAsText(concentration, concentrationStatus, concentrationAdjustedFlag, useTextBelowAboveFlag,
            methodTest, methodReporting, totalAdjustmentFactor, highestCalibrator, zeroMethodFlag, resultType == TestResultType.Calibrator, outsourcedTest: false);
    }

    private async Task<double> GetTotalAdjustmentFactor(MethodTest? methodTest, int totalDilutionFactor, string? carrierBarcode, TestResultType resultType)
    {
        var adjustmentFactor = (double)totalDilutionFactor;

        if (resultType is TestResultType.Calibrator or TestResultType.CurveControl)
        {
            return adjustmentFactor;
        }

        if (methodTest == null)
        {
            return adjustmentFactor;
        }

        adjustmentFactor *= methodTest.ConcentrationFactor;

        if (methodTest.UseLotSpecificCode && !string.IsNullOrEmpty(carrierBarcode))
        {
            adjustmentFactor *= await GetCorrectionFactorFromBarcode(carrierBarcode);
        }

        return adjustmentFactor;
    }

    private async Task<double> GetCorrectionFactorFromBarcode(string carrierBarcode)
    {
        const double noCorrection = 1.0;

        var (articleType, _) = await barcodeService.DetectArticleType(carrierBarcode).ConfigureAwait(false);
        var lot = (await barcodeService.GetReagentInfo(articleType, carrierBarcode).ConfigureAwait(false))?.Lot;
        var result = (lot as EliaWellLotInformation)?.CorrectionFactor ?? noCorrection;
        if (result == 0.0)
        {
            result = noCorrection;
        }

        return result;
    }

    private static bool GetConcentrationAdjustedFlag(MethodTest? methodTest, int totalDilutionFactor)
    {
        return totalDilutionFactor > 1 || (methodTest != null && (methodTest.UseLotSpecificCode || Math.Abs(methodTest.ConcentrationFactor - 1.0) > 0.0005));
    }

    private async Task<double> GetMaxExpectedConcentration(string methodName)
    {
        var concentrationList = await methodsService.GetMethodCalibratorPoints(methodName, p => p.ExpectedConcentration);

        return concentrationList.Max();
    }
    private async Task<double> GetMinExpectedConcentration(string methodName)
    {
        var concentrationList = await methodsService.GetMethodCalibratorPoints(methodName, p => p.ExpectedConcentration);

        return concentrationList.Min();
    }

    /// <summary>
    /// Check if this is a "zero method" (lowest calibrator point "almost zero")
    /// </summary>
    /// <param name="methodName">Method name</param>
    /// <returns>True if this is a zero method</returns>
    private async Task<bool> IsZeroMethod(string methodName)
    {
        if (!(await GetMinExpectedConcentration(methodName) < _concentrationLimitOn))
        {
            return false;
        }

        var calibratorStartingPoint = await methodsService.GetMethod(methodName, m => m.Calculation.CalibratorStartingPoint);

        return calibratorStartingPoint == 0;
    }


    /// <summary>
    /// Format a response value as text
    /// </summary>
    /// <param name="response">Response value</param>
    /// <returns>Response as text</returns>
    public string ResponseAsText(double response)
    {
        if (response > _maxResponse)
        {
            return ">" + _maxResponse.ToString("0");
        }
        else
        {
            if (response > _minResponse)
            {
                return FormatCorrect(response, 0);
            }
            else
            {
                return _minResponse.ToString("0");
            }
        }
    }

    /// <summary>
    /// Format a value correctly ("Bankers rounding") with a certain number of decimals.
    /// </summary>
    /// <param name="value">Double value</param>
    /// <param name="decimals">Number of decimals</param>
    /// <returns>A formatted double value as text</returns>
    private static string FormatCorrect(double value, int decimals)
    {
        var doubleValue = Math.Round(value, decimals, MidpointRounding.ToEven);

        var formatString = "0" + "." + new string('0', decimals);
        return doubleValue.ToString(formatString);
    }

    public string NoDataAvailable()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        return resultSetting.Value == null ? "" : resultSetting.Value.TextSubstitution.MissingResultText;
    }

    public string LowRU()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        return resultSetting.Value == null ? "" : resultSetting.Value.TextSubstitution.LowResponseText;
    }

    public string QuotientAsText(double quotient, bool asPercent)
    {
        string text;

        var quotientForFormatting = quotient;

        if (asPercent)
        {
            quotientForFormatting *= 100;

            if (quotientForFormatting > _maxQuotient)
            {
                text = ">" + FormatCorrect(_maxQuotient, 0);
            }
            else if (quotientForFormatting >= 99.5)
            {
                text = FormatCorrect(quotientForFormatting, 0);
            }
            else if (quotientForFormatting >= 10)
            {
                text = FormatCorrect(quotientForFormatting, 1);
            }
            else
            {
                text = FormatCorrect(quotientForFormatting, 2);
            }
            text += "%";
        }
        else
        {
            if (quotientForFormatting > _maxQuotient)
            {
                text = ">" + FormatCorrect(_maxQuotient, 0);
            }
            else if (quotientForFormatting >= 100000)
            {
                text = FormatCorrect(quotientForFormatting, 0);
            }
            else if (quotientForFormatting >= 10000)
            {
                text = FormatCorrect(quotientForFormatting, 1);
            }
            else
            {
                text = FormatCorrect(quotientForFormatting, 2);
            }
        }

        return text;
    }

    /// <summary>
    /// Included formatting function from Prime code
    /// </summary>
    private string ConcentrationAsText(double concentration, ConcentrationStatus concentrationStatus, bool concentrationAdjustedFlag, bool useTextBelowAboveFlag,
        MethodTest? methodTest, MethodReporting methodReporting,
        double totalAdjustmentFactor, double highestCalibrator, bool zeroMethodFlag, bool isCalibratorFlag, bool outsourcedTest)
    {
        string textBelow;
        var textAbove = "";
        string textDoubtful;
        int decimalsBelow10;
        int decimalsBetween10And100;
        int decimalsAbove100;

        // Get texts to use for below/above curve
        if (methodTest != null)
        {
            // Get texts to use when undiluted
            if (concentrationAdjustedFlag == false)
            {
                if (!string.IsNullOrEmpty(methodTest.TextQuantitativeBelow))
                {
                    textBelow = methodTest.TextQuantitativeBelow ?? "";
                }
                else
                {
                    textBelow = methodReporting.TextQuantitativeBelow;
                }

                if (!string.IsNullOrEmpty(methodTest.TextQuantitativeAbove))
                {
                    textAbove = methodTest.TextQuantitativeAbove ?? "";
                }
                else
                {
                    if (!methodReporting.UseCalculatedTextQuantitativeAbove)
                    {
                        textAbove = methodReporting.TextQuantitativeAbove;
                    }
                }
            }

            // Get texts to use when diluted
            else
            {
                if (!string.IsNullOrEmpty(methodTest.TextQuantitativeBelowForDilutedTest))
                {
                    textBelow = methodTest.TextQuantitativeBelowForDilutedTest;
                }
                else
                {
                    textBelow = NoDataAvailable();
                }


                if (!string.IsNullOrEmpty(methodTest.TextQuantitativeAboveForDilutedTest))
                {
                    textAbove = methodTest.TextQuantitativeAboveForDilutedTest;
                }
                else
                {
                    if (!methodReporting.UseCalculatedTextQuantitativeAbove)
                    {
                        textAbove = NoDataAvailable();
                    }
                }
            }

            textDoubtful = methodReporting.TextQuantitativeDoubtful ?? "";
        }
        else
        {
            textBelow = NoDataAvailable();
            textAbove = NoDataAvailable();
            textDoubtful = NoDataAvailable();
        }

        // Get number of decimals to use
        if (methodTest != null && isCalibratorFlag == false)
        {
            decimalsBelow10 = methodTest.NumberOfDecimalsForConcentrationBelow10;
            decimalsBetween10And100 = methodTest.NumberOfDecimalsForConcentrationBetween10And100;
            decimalsAbove100 = methodTest.NumberOfDecimalsForConcentrationAbove100;
        }
        else
        {
            decimalsBelow10 = 2;
            decimalsBetween10And100 = 1;
            decimalsAbove100 = 0;
        }

        // Create a temporary Concentration status
        var useAbsoluteConcentrationLimitFlag = false;
        var formatStatus = concentrationStatus;
        if ((formatStatus != ConcentrationStatus.NotAvailable) && (useTextBelowAboveFlag == false))
        {
            formatStatus = ConcentrationStatus.Ok;
        }

        if (methodTest != null)
        {
            if (formatStatus == ConcentrationStatus.Under && methodTest.ReportConcentrationBelowCurveFlag)
            {
                useAbsoluteConcentrationLimitFlag = true;
            }
        }

        string temporaryText;
        switch (formatStatus)
        {
            // Quantitative value ---------------------------------------------------------------
            case ConcentrationStatus.Ok:
                temporaryText = ConcentrationAsText(concentration, decimalsBelow10, decimalsBetween10And100, decimalsAbove100, zeroMethodFlag, true);

                break;

            // No concentration calculated ------------------------------------------------------
            case ConcentrationStatus.NotAvailable:
                temporaryText = NoDataAvailable();
                break;

            // Over range -----------------------------------------------------------------------
            case ConcentrationStatus.Over:
                temporaryText = textAbove;

                // Check if calculated text above shall be used
                if (methodTest != null && string.IsNullOrEmpty(textAbove) && outsourcedTest == false)
                {
                    if (methodReporting.UseCalculatedTextQuantitativeAbove)
                    {
                        temporaryText = CalculateTextQuantitativeAbove(totalAdjustmentFactor, highestCalibrator, methodTest,
                            decimalsBelow10, decimalsBetween10And100, decimalsAbove100);
                    }
                }

                break;

            // Under range ----------------------------------------------------------------------
            case ConcentrationStatus.Under:
                temporaryText = textBelow;

                // Special case; when concentration was below curve but reporting was allowed, format against absolute limit 0.01
                if (useAbsoluteConcentrationLimitFlag)
                {
                    temporaryText = TextBelowCurveLowestConcentration();
                }

                break;

            // Doubtful concentration (mean value when replicates differ in status) --------------
            case ConcentrationStatus.Doubtful:
                temporaryText = textDoubtful;
                break;

            // Unknown, should not end up here --------------------------------------------------
            default:
                temporaryText = NoDataAvailable();
                break;
        }

        if (temporaryText is { Length: > Constants.ConcentrationAsTextLength })
        {
            temporaryText = NoDataAvailable();
        }

        return temporaryText;
    }

    private static string ConcentrationAsText(double concentration, int decimalsBelow10, int decimalsBetween10And100, int decimalsAbove100, bool zeroMethodFlag, bool limitToMaxConcentrationFlag)
    {
        string temporaryText;

        if (limitToMaxConcentrationFlag && (concentration > MaxConcentration()))
        {
            temporaryText = MaxConcentrationText();
        }
        else
        {
            if (concentration >= 100)
            {
                temporaryText = FormatCorrect(concentration, decimalsAbove100);
            }
            else
            {
                if (concentration >= 10)
                {
                    temporaryText = FormatCorrect(concentration, decimalsBetween10And100);
                }
                else
                {
                    temporaryText = FormatCorrect(concentration, decimalsBelow10);

                    // Special formatting, if non-ZERO method, and value became "0.00", use 3 decimals
                    if ((zeroMethodFlag == false) &&
                        (decimalsBelow10 == 2) &&
                        (concentration > 0) &&
                        (temporaryText == FormatCorrect(0, 2)))
                    {
                        temporaryText = FormatCorrect(concentration, 3);
                    }
                }

            }
        }

        return temporaryText;
    }

    private static double MaxConcentration()
    {
        // TODO: Should come from a setting
        return 99999;
    }

    private static string MaxConcentrationText()
    {
        var fixedText = ""; // TODO: Should come from a setting

#pragma warning disable CA1820
        if (fixedText == "")
#pragma warning restore CA1820
        {
            return ">" + FormatCorrect(MaxConcentration(), 0);
        }
        else
        {
            return fixedText;
        }
    }

    private string CalculateTextQuantitativeAbove(double totalAdjustmentFactor, double highestCalibrator, MethodTest? methodTest, int decimalsBelow10, int decimalsBetween10And100, int decimalsAbove100)
    {
        string resultText;

        // Find the upper limit for reporting
        var rawUpperLimit = highestCalibrator;
        if (methodTest != null)
        {
            var testUpperLimit = methodTest.HighestConcentrationToReport ?? 0;
            if (testUpperLimit > _concentrationLimitOn)
            {
                var testRawUpperLimit = testUpperLimit / totalAdjustmentFactor;
                if (testRawUpperLimit < rawUpperLimit)
                {
                    rawUpperLimit = testRawUpperLimit;
                }
            }
        }

        // Calculate the value to format
        var calculatedUpperLimit = rawUpperLimit * totalAdjustmentFactor;

        if (calculatedUpperLimit > MaxConcentration())
        {
            resultText = MaxConcentrationText();
        }
        else
        {
            if (calculatedUpperLimit >= 100)
            {
                resultText = ">" + FormatCorrect(calculatedUpperLimit, decimalsAbove100);
            }
            else
            {
                if (calculatedUpperLimit >= 10)
                {
                    resultText = ">" + FormatCorrect(calculatedUpperLimit, decimalsBetween10And100);
                }
                else
                {
                    resultText = ">" + FormatCorrect(calculatedUpperLimit, decimalsBelow10);
                }
            }
        }

        if (resultText.Length > Constants.ConcentrationAsTextLength)
        {
            resultText = NoDataAvailable();
        }

        return resultText;
    }

    private static string TextBelowCurveLowestConcentration()
    {
        var fixedText = ""; // TODO: Should come from a setting

#pragma warning disable CA1820
        if (fixedText == "")
#pragma warning restore CA1820
        {
            return "<" + FormatCorrect(_absoluteConcentrationLimit, 2);
        }
        else
        {
            return fixedText;
        }
    }
}
