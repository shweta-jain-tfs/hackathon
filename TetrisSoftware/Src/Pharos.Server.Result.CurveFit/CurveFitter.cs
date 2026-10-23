using Pharos.Server.Result.CurveFit.PiePuf;

namespace Pharos.Server.Result.CurveFit;

public interface ICurveFitter
{
    IFittedCurve FitCurve(CurveData curve, MethodData methodData);
    IFittedCurve PrepareFittedCurveForCalculation(FittedCurveData fittedCurveData);
}

public class CurveFitter : ICurveFitter
{
    private readonly ICurveFitterApi _curveFitterApi;

    public CurveFitter(ICurveFitterApi curveFitterApi)
    {
        _curveFitterApi = curveFitterApi;
    }

    public IFittedCurve FitCurve(CurveData curve, MethodData methodData)
    {
        var additionalData = new CurveAdditionalDataTypeCommon
        {
            StraightLineSwitch = methodData.Calculation.CalibratorStartingPoint,
            StraightLineNumberOfPoints = methodData.Calculation.CalibratorStartingPoint > 0 ? methodData.Calculation.CalibratorsForLeastSquareCalc : 0,
            StraightLineSwitchConcentration = methodData.Calculation.CalibratorStartingPoint > 0 ? methodData.Points[methodData.Calculation.CalibratorStartingPoint].DefinedConcentration : 0,

            CurveMinConcentration = methodData.Points.Min(p => p.DefinedConcentration),
            CurveMaxConcentration = methodData.Points.Max(p => p.DefinedConcentration)
        };

        var points = methodData.Points.OrderBy(p => p.DefinedConcentration).ToList();

        var limits = new PIECurve_IdealCurveLimits
        {
            ExcludeLowestCalibratorFromMeanDiff = methodData.Calibrator.ExcludeLowestCalibratorFromMeanDiff,
            MaxBadReplicates = methodData.Calibrator.MaxBadReplicates,
            NumberOfLevels = points.Count,
            IdealResponse = points.Select(p => p.IdealResponse).ToArray(),
            ExpectedSD = points.Select(p => p.ExpectedSD).ToArray(),
            DeviationSDLimit = points.Select(p => p.DeviationSDLimit).ToArray(),
            PrecisionSDLimit = points.Select(p => p.PrecisionSDLimit).ToArray(),
            LowerLogarithmicLimit = points.Select(p => p.LowerLogarithmicLimit).ToArray(),
            UpperLogarithmicLimit = points.Select(p => p.UpperLogarithmicLimit).ToArray(),
            LowerResponseLimit = points.Select(p => p.LowerResponseLimit).ToArray(),
            UpperResponseLimit = points.Select(p => p.UpperResponseLimit).ToArray()
        };

        var curveType = CurveTypeMarshal.CreateCurveData(curve, methodData.Calculation, limits);
        //var jsonCurveType = System.Text.Json.JsonSerializer.Serialize(curveType);

        var curveCheckResult = _curveFitterApi.CurveCheck(curveType, additionalData.StraightLineSwitch);

        //var jsonCheckResult = System.Text.Json.JsonSerializer.Serialize(curveCheckResult);

        var fitPar = new FitParTypeCommon();
        var curveFitResult = _curveFitterApi.CurveFit(curveType, additionalData, fitPar);
        //var jsonCurveFitResult = System.Text.Json.JsonSerializer.Serialize(curveFitResult);

        return new FittedCurve(_curveFitterApi, additionalData, fitPar, curveCheckResult, curveFitResult);
    }

    public IFittedCurve PrepareFittedCurveForCalculation(FittedCurveData fittedCurveData)
    {
        var additionalData = new CurveAdditionalDataTypeCommon()
        {
            StraightLineSwitch = fittedCurveData.ZeroPointCalculationNumberOfIntervals,
            StraightLineNumberOfPoints = fittedCurveData.NumberOfCalibrationPointsInZeroPointCalculation,
            StraightLineSwitchConcentration = fittedCurveData.ZeroPointSwitchConcentration,
            coeff_zeropoint_intercept = fittedCurveData.ZeroPointCalculationCoefficientAlfa,
            coeff_zeropoint_slope = fittedCurveData.ZeroPointCalculationCoefficientBeta,
            CurveMinConcentration = fittedCurveData.CurveMinConcentration,
            CurveMaxConcentration = fittedCurveData.CurveMaxConcentration
        };

        var fitPar = new FitParTypeCommon
        {
            Model = (int)fittedCurveData.CalculationType,
            ParamValue =
                         {
                             [0] = fittedCurveData.CalculationParameterA,
                             [1] = fittedCurveData.CalculationParameterB,
                             [2] = fittedCurveData.CalculationParameterC,
                             [3] = fittedCurveData.CalculationParameterD,
                             [4] = fittedCurveData.CalculationParameterE
                         },
            ParamNb = 5
        };
        if (fittedCurveData.CalculationType == ModelNumCommon.MdRodbard4)
        {
            fitPar.ParamNb = 4;
        }

        var curveCheckResult = new CurveCheckResultCommon();
        var curveFitResult = new CurveFitResultCommon();

        return new FittedCurve(_curveFitterApi, additionalData, fitPar, curveCheckResult, curveFitResult);
    }

}