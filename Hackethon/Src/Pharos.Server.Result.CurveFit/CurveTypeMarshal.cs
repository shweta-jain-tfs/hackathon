using Pharos.Server.Result.CurveFit.PiePuf;

namespace Pharos.Server.Result.CurveFit;

internal class CurveTypeMarshal
{
    public const int _maxIterations = 150;
    private const int _maxCurvePoints = 400;

    private const int _modelRodbard4 = 0;
    private const int _modelRodbard5 = 1;

    private const int _normalWeightNo = 0;
    private const int _normalWeightPredicted = 3;

    private const int _robustWeightNo = 0;
    private const int _robustWeightTukeys = 1;
    private const int _robustWeightHuber = 2;
    private const int _robustWeightTiedePagano = 4;

    public static CurveTypeCommon CreateCurveData(CurveData curveData, MethodCalculation methodCalculation, PIECurve_IdealCurveLimits limits)
    {
        var curve = BuildCurve(curveData);
        SetCurveData(out var curveType, curve, methodCalculation, limits);
        return curveType;
    }

    private static PIECurve BuildCurve(CurveData curveData)
    {
        var levels = curveData.Responses.Zip(curveData.Concentrations, (r, c) => CreateCurveLevel(new[] { r }, c)).ToArray();

        return new PIECurve
        {
            Level = levels,
            NumberOfLevels = levels.Length,
            NumberOfPoints = levels.Length
        };
    }

    private static PIECurve.PIECurve_Level CreateCurveLevel(double[] responses, double concentration)
    {
        return new PIECurve.PIECurve_Level
        {
            Response = responses,
            Concentration = concentration,
            NumberOfResponses = 1
        };
    }

    public static bool SetCurveData(out CurveTypeCommon curveType, PIECurve curve, MethodCalculation methodCalculation, PIECurve_IdealCurveLimits idealCurveLimits)
    {
        var concentrateArray = new double[_maxCurvePoints];
        var responseArray = new double[_maxCurvePoints];
        var levelArray = new int[_maxCurvePoints];

        var numberOfPoints = 0;
        var levelNumber = 1;

        var ll = curve.Level.GroupBy(l => l.Concentration).OrderBy(l => l.Key).ToList();

        foreach (var level in ll)
        {
            foreach (var l in level)
            {
                foreach (var response in l.Response)
                {
                    concentrateArray[numberOfPoints] = l.Concentration;
                    responseArray[numberOfPoints] = response;
                    levelArray[numberOfPoints] = levelNumber;
                    numberOfPoints++;
                }
            }
            levelNumber++;
        }

        curveType = new CurveTypeCommon
        {
            NumberOfPoints = numberOfPoints,
            Concentration = concentrateArray,
            Response = responseArray,
            LevelIndex = levelArray
        };

        // Model
        switch (methodCalculation.CalculationType)
        {
            case (short)EnumCalculationTypes.CalcTypeRodbard4Par:
                curveType.Model = _modelRodbard4;
                break;
            case (short)EnumCalculationTypes.CalcTypeRodbard5Par:
                curveType.Model = _modelRodbard5;
                break;
            default:
                // Common.Common.Instance.SystemLogError("Invalid calculation type for method " + methodCalculation.MethodName);
                return false;
        }

        // NormWt
        switch (methodCalculation.NormalWeight)
        {
            case (short)EnumNormalWeights.NoNormalWeights:
                curveType.NormWt = _normalWeightNo;
                break;
            case (short)EnumNormalWeights.NormalWeightPredicted:
                curveType.NormWt = _normalWeightPredicted;
                break;
            default:
                // Common.Common.Instance.SystemLogError("Invalid NormalWeight type for method " + methodCalculation.MethodName);
                return false;
        }

        // RobsWt
        switch (methodCalculation.RobustWeight)
        {
            case (short)EnumRobustWeights.NoRobustWeights:
                curveType.RobsWt = _robustWeightNo;
                break;
            case (short)EnumRobustWeights.Tukey:
                curveType.RobsWt = _robustWeightTukeys;
                break;
            case (short)EnumRobustWeights.Huber:
                curveType.RobsWt = _robustWeightHuber;
                break;
            case (short)EnumRobustWeights.TieDePagano:
                curveType.RobsWt = _robustWeightTiedePagano;
                break;
            default:
                // Common.Common.Instance.SystemLogError("Invalid RobustWeight type for method " + methodCalculation.MethodName);
                return false;
        }


        curveType.RobLst = 0;
        curveType.FacWtA = methodCalculation.PredictedVarianceA;
        curveType.FacWtB = methodCalculation.PredictedVarianceB;
        curveType.MaxIterations = _maxIterations;
        curveType.MinCorrCoeff = methodCalculation.MinCoefficientOfCorrelation;
        curveType.CheckResult = 1;


        // Data for PIE32_CurveCheck
        curveType.NumberOfLevels = idealCurveLimits.NumberOfLevels;

        Array.Copy(idealCurveLimits.IdealResponse, curveType.IdealResponse, idealCurveLimits.IdealResponse.Length);
        Array.Copy(idealCurveLimits.ExpectedSD, curveType.ExpectedSD, idealCurveLimits.ExpectedSD.Length);
        Array.Copy(idealCurveLimits.DeviationSDLimit, curveType.DeviationSDLimit, idealCurveLimits.DeviationSDLimit.Length);
        Array.Copy(idealCurveLimits.PrecisionSDLimit, curveType.PrecisionSDLimit, idealCurveLimits.PrecisionSDLimit.Length);
        Array.Copy(idealCurveLimits.LowerLogarithmicLimit, curveType.LowerLogarithmicLimit, idealCurveLimits.LowerLogarithmicLimit.Length);
        Array.Copy(idealCurveLimits.UpperLogarithmicLimit, curveType.UpperLogarithmicLimit, idealCurveLimits.UpperLogarithmicLimit.Length);
        Array.Copy(idealCurveLimits.LowerResponseLimit, curveType.LowerResponseLimit, idealCurveLimits.LowerResponseLimit.Length);
        Array.Copy(idealCurveLimits.UpperResponseLimit, curveType.UpperResponseLimit, idealCurveLimits.UpperResponseLimit.Length);

        curveType.MaxBadReplicates = idealCurveLimits.MaxBadReplicates;
        curveType.ExcludeLowestCalibratorFromMeanDiff = idealCurveLimits.ExcludeLowestCalibratorFromMeanDiff;

        return true;
    }
}