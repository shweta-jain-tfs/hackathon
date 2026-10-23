using Pharos.Server.Result.CurveFit.PiePuf;

namespace Pharos.Server.Result.CurveFit;

public enum ConcentrationStatus
{
    NotAvailable = 0,
    Ok = 1,
    Over = 2,
    Under = 3,
    Doubtful = 4
}

public interface IFittedCurve
{
    double ResponseToConcentration(double response, out ConcentrationStatus concentrationStatus, double concentrationFactor = 1, int dilutionFactor = 1);
    double ConcentrationToResponse(double concentration);

    bool IsCurveCheckOk { get; }
    bool AnyFlaggedPoints { get; }
    bool CurveFittedFlag { get; set; }
    bool CurveControlDisabledFlag { get; set; }
    int CurveId { get; set; }
    int NumberOfFlaggedPoints { get; }

    double CalculationParameterA { get; }
    double CalculationParameterB { get; }
    double CalculationParameterC { get; }
    double CalculationParameterD { get; }
    double CalculationParameterE { get; }
    int ZeroPointCalculationNumberOfIntervals { get; }
    double ZeroPointSwitchConcentration { get; }
    int NumberOfCalibrationPointsInZeroPointCalculation { get; }
    double ZeroPointCalculationCoefficientAlfa { get; }
    double ZeroPointCalculationCoefficientBeta { get; }
    int IterationsUsed { get; }
    int MaxIterationsAllowed { get; }
    double CurveMinConcentration { get; }
    double CurveMaxConcentration { get; }
    bool ErrorPrecisionFlag { get; }
    bool ErrorFlatCurveFlag { get; }
    bool ErrorLowerPartTooHighFlag { get; }
    bool ErrorSingleCalibratorAboveRangeFlag { get; }
    bool ErrorSingleCalibratorBelowRangeFlag { get; }
    bool ErrorNoRisingTrendFlag { get; }
    bool IsReplicateFlagged(int nReplicate);
    double? GetArtificialResponse(int nCalibratorPoint);
}

public class FittedCurve : IFittedCurve
{
    private readonly ICurveFitterApi _curveFitter;
    private readonly CurveAdditionalDataTypeCommon _additionalData;
    private readonly FitParTypeCommon _fitPar;
    private readonly CurveCheckResultCommon _curveCheckResult;
    private readonly CurveFitResultCommon _curveFitResult;

    public bool IsCurveCheckOk => _curveCheckResult.IsCurveCheckOk;
    public bool IsCurveFitOk => _curveFitResult.IsCurveFitOk;
    public bool IsCurveShapeOk => _curveCheckResult.ShapeErrorCV == false && _curveCheckResult.ShapeErrorFlatCurve == false && _curveCheckResult.ShapeErrorLowerPartHigh == false &&
                                  _curveCheckResult.ShapeErrorNoRisingTrend == false && _curveCheckResult.ShapeErrorSingleAbove == false && _curveCheckResult.ShapeErrorSingleBelow == false;
    public bool AnyFlaggedPoints => _curveCheckResult.NumberOfFlaggedPoints > 0;

    public int IterationsToFit => _curveFitResult.IterationsToFit;
    public ModelNumCommon FittingModel => (ModelNumCommon)_fitPar.Model;
    public int[] FlaggedPointIndexes => _curveCheckResult.FlaggedPoint.ToArray();
    public double[] FittingModelParameters => _fitPar.ParamValue.ToArray();
    public int NumberOfFlaggedPoints => _curveCheckResult.NumberOfFlaggedPoints;

    public double CalculationParameterA => _fitPar.ParamValue != null ? _fitPar.ParamValue[0] : 0;
    public double CalculationParameterB => _fitPar.ParamValue != null ? _fitPar.ParamValue[1] : 0;
    public double CalculationParameterC => _fitPar.ParamValue != null ? _fitPar.ParamValue[2] : 0;
    public double CalculationParameterD => _fitPar.ParamValue != null ? _fitPar.ParamValue[3] : 0;
    public double CalculationParameterE => _fitPar.ParamValue != null && _fitPar.ParamNb > 4 ? _fitPar.ParamValue[4] : 0;

    public int ZeroPointCalculationNumberOfIntervals => _additionalData.StraightLineSwitch;
    public double ZeroPointSwitchConcentration => _additionalData.StraightLineSwitchConcentration;
    public int NumberOfCalibrationPointsInZeroPointCalculation => _additionalData.StraightLineNumberOfPoints;
    public double ZeroPointCalculationCoefficientAlfa => _additionalData.coeff_zeropoint_intercept;
    public double ZeroPointCalculationCoefficientBeta => _additionalData.coeff_zeropoint_slope;
    public int IterationsUsed => _curveFitResult.IterationsToFit;
    public int MaxIterationsAllowed => CurveTypeMarshal._maxIterations;
    public double CurveMinConcentration => _additionalData.CurveMinConcentration;
    public double CurveMaxConcentration => _additionalData.CurveMaxConcentration;
    public bool ErrorPrecisionFlag => _curveCheckResult.ShapeErrorCV;
    public bool ErrorFlatCurveFlag => _curveCheckResult.ShapeErrorFlatCurve;
    public bool ErrorLowerPartTooHighFlag => _curveCheckResult.ShapeErrorLowerPartHigh;
    public bool ErrorSingleCalibratorAboveRangeFlag => _curveCheckResult.ShapeErrorSingleAbove;
    public bool ErrorSingleCalibratorBelowRangeFlag => _curveCheckResult.ShapeErrorSingleBelow;
    public bool ErrorNoRisingTrendFlag => _curveCheckResult.ShapeErrorNoRisingTrend;

    // When using a Curve to make a FittedCurve
    public bool CurveFittedFlag { get; set; }
    public bool CurveControlDisabledFlag { get; set; }
    public int CurveId { get; set; }

    public FittedCurve(ICurveFitterApi curveFitter, CurveAdditionalDataTypeCommon additionalData, FitParTypeCommon fitPar, CurveCheckResultCommon curveCheckResult, CurveFitResultCommon curveFitResult)
    {
        _curveFitter = curveFitter;
        _additionalData = additionalData;
        _fitPar = fitPar;
        _curveCheckResult = curveCheckResult;
        _curveFitResult = curveFitResult;
        CurveFittedFlag = curveFitResult.IsCurveFitOk;
    }

    public double ResponseToConcentration(double response, out ConcentrationStatus concentrationStatus, double concentrationFactor = 1, int dilutionFactor = 1)
    {
        concentrationStatus = ConcentrationStatus.NotAvailable;

        var concentration = _curveFitter.ResponseToConcentration(response, _additionalData, _fitPar, concentrationFactor, dilutionFactor, out var belowOnAboveCurve);

        switch (belowOnAboveCurve)
        {
            case 0:
                concentrationStatus = ConcentrationStatus.Ok;
                break;
            case -1:
                concentrationStatus = ConcentrationStatus.Under;
                break;
            case 1:
                concentrationStatus = ConcentrationStatus.Over;
                break;
            default:
                break;
        }

        return concentration;
    }

    public double ConcentrationToResponse(double concentration)
    {
        var result = 0.0;

        if (!CurveFittedFlag)
        {
            return result;
        }

        // Retrieve calculation data
        var calcData = new CurveCalculationType
        {
            CurveFittedFlag = true,
            CalculationType = ModelToCalculationType(_fitPar.Model),
            ZeroPointSwitchConcentration = ZeroPointSwitchConcentration,
            ZeroPointAlfa = ZeroPointCalculationCoefficientAlfa,
            ZeroPointBeta = ZeroPointCalculationCoefficientBeta,
            CalculationParameterA = CalculationParameterA,
            CalculationParameterB = CalculationParameterB,
            CalculationParameterC = CalculationParameterC,
            CalculationParameterD = CalculationParameterD,
            CalculationParameterE = CalculationParameterE
        };

        result = PUF.CalculatePUFResponse(concentration, calcData);

        return result;
    }

    // Helpers
    public bool IsReplicateFlagged(int nReplicate)
    {
        var result = _curveCheckResult.FlaggedPoint[nReplicate] == 1;

        return result;
    }

    public double? GetArtificialResponse(int nCalibratorPoint)
    {
        if (_curveCheckResult.ArtificialLevelFlag[nCalibratorPoint] == 1)
        {
            return _curveCheckResult.ArtificialResponse[nCalibratorPoint];
        }
        else
        {
            return null;
        }
    }

    private static EnumCalculationTypes ModelToCalculationType(int model)
    {
        return model == (int)ModelNumCommon.MdRodbard4 ? EnumCalculationTypes.CalcTypeRodbard4Par : EnumCalculationTypes.CalcTypeRodbard5Par;
    }
}