using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Result.CurveFit;
using ConcentrationStatus = Pharos.Server.Result.CurveFit.ConcentrationStatus;

namespace Pharos.Server.Result.Application.Services.ResultCalculationService;

internal class TestWellCorrectionFactor
{
    private readonly ILogger _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">The logger to use</param>
    public TestWellCorrectionFactor(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adjust response value according to Test Well Correction Factor algorithm
    /// </summary>
    /// <param name="fittedCurve">Curve for concentration calculations</param>
    /// <param name="response">Response value</param>
    /// <param name="carrierBarcode">Carrier barcode</param>
    /// <param name="eliaCalibratorWellLot">A lot for a EliA Calibrator Well carrier</param>
    /// <param name="adjustmentForAlfaBeta1Beta2">Adjustment parameter</param>
    /// <returns>True if OK and the new adjusted response value</returns>
    public (bool Result, double NewResponse) AdjustResponseByTestWellCorrectionFactor(IFittedCurve fittedCurve, double response, string carrierBarcode, EliaCalibratorWellLotInformation eliaCalibratorWellLot, double adjustmentForAlfaBeta1Beta2)
    {
        (bool Result, double NewResponse) returnValue = (false, response);

        var maximumIterations = SettingTWCFMaximumIterations();
        var nIterations = 1;
        _logger.LogInformation("TWCF: Original response: {Response} Carrier barcode: {CarrierBarcode}", response.ToString("0.00"), carrierBarcode);

        var iteratedResponse = response;
        double previousConcentration = 0;

        // The iterations
        while (true)
        {
            var concentration = fittedCurve.ResponseToConcentration(iteratedResponse, out var concentrationStatus);

            if (concentrationStatus == ConcentrationStatus.NotAvailable)
            {
                _logger.LogInformation("TWCF: Failed to calculate TWCF concentration (not available) in iteration {Iteration}", nIterations);
                return returnValue;
            }

            if (concentrationStatus == ConcentrationStatus.Under)
            {
                concentration = fittedCurve.CurveMinConcentration;
                _logger.LogInformation("TWCF: Concentration became lower than curve, using curve Lowest concentration");
            }

            if (concentrationStatus == ConcentrationStatus.Over)
            {
                concentration = fittedCurve.CurveMaxConcentration;
                _logger.LogInformation("TWCF: Concentration became higher than curve, using curve Highest concentration");
            }

            concentration *= adjustmentForAlfaBeta1Beta2;

            // Check if exit criteria 0.001 difference in concentration is reached
            if (nIterations > 1)
            {
                if (Math.Abs(previousConcentration - concentration) < 0.001)
                {
                    returnValue.Result = true;
                    returnValue.NewResponse = iteratedResponse;
                    _logger.LogInformation("TWCF: Calculation completed, Response={IteratedResponse}", iteratedResponse.ToString("0.0000"));
                    return returnValue;
                }
            }

            // Check for maximum number of iterations
            if (nIterations > maximumIterations)
            {
                _logger.LogInformation("TWCF: Maximum number of iterations exceeded!");
                return returnValue;
            }

            const double nearZero = 0.0005;

            // Calculate next RU to use in Iteration                
            if (concentration >= nearZero)
            {
                iteratedResponse = AdjustResponseAccordingToCarrierLotAndConcentration(response, eliaCalibratorWellLot, concentration);
            }

            previousConcentration = concentration;
            _logger.LogInformation("TWCF: Iteration {Iteration}, Concentration={PreviousConcentration} -> new RU: {IteratedResponse}", nIterations, previousConcentration.ToString("0.0000"), iteratedResponse.ToString("0.00"));

            nIterations++;
        }
    }

    /// <summary>
    /// Adjust response according to carrier lot and concentration
    /// </summary>
    /// <param name="response">Original response</param>
    /// <param name="eliaCalibratorLot">A lot for a EliA Calibrator Well carrier</param>
    /// <param name="concentration">Input concentration</param>
    /// <returns>Adjusted concentration</returns>
    public static double AdjustResponseAccordingToCarrierLotAndConcentration(double response, EliaCalibratorWellLotInformation eliaCalibratorLot, double concentration)
    {
        var alpha = (eliaCalibratorLot.Alpha / 5) - 1;
        var beta1 = (eliaCalibratorLot.Beta1 / 500) - 0.01;
        var beta2 = (eliaCalibratorLot.Beta2 / 5000000) - 0.000001;
        const double nearZero = 0.0005;

        var expFactor = alpha + (beta1 * concentration) + (beta2 * concentration * concentration);
        var newResponse = response / Math.Exp(expFactor);

        if (newResponse < nearZero)
        {
            newResponse = nearZero;
        }

        return newResponse;
    }

    private static int SettingTWCFMaximumIterations()
    {
        return 20; // Default, possible to change using settings...
    }
}
