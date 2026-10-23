namespace Pharos.Server.Result.CurveFit.PiePuf;

public record CurveFitResultCommon
{
    public bool IsCurveFitOk { get; init; }
    public int IterationsToFit { get; init; }
}

public record CurveCheckResultCommon
{
    public bool IsCurveCheckOk { get; init; }
    public int[] FlaggedPoint { get; init; }
    public int[] ArtificialLevelFlag { get; init; }
    public double[] ArtificialResponse { get; init; }
    public int NumberOfFlaggedPoints { get; init; }
    public int NumberOfArtificialLevels { get; init; }
    public int LowestPointSubstituted { get; init; }
    public bool ShapeErrorCV { get; init; }
    public bool ShapeErrorFlatCurve { get; init; }
    public bool ShapeErrorLowerPartHigh { get; init; }
    public bool ShapeErrorSingleAbove { get; init; }
    public bool ShapeErrorSingleBelow { get; init; }
    public bool ShapeErrorNoRisingTrend { get; init; }
}

public class CurveTypeCommon
{
    private const int _maxCurvePoints = 400;
    private const int _maxCurveLevels = 20;

    // The curve data
    public int NumberOfPoints { get; set; }
    public double[] Concentration { get; set; } = new double[_maxCurvePoints];
    public double[] Response { get; set; } = new double[_maxCurvePoints];
    public int[] LevelIndex { get; set; } = new int[_maxCurvePoints];

    // The following is needed when using PIE_CurveFit
    public int Model { get; set; }
    public int NormWt { get; set; }
    public int RobsWt { get; set; }
    public int RobLst { get; set; }
    public double FacWtA { get; set; }
    public double FacWtB { get; set; }
    public int MaxIterations { get; set; }
    public double MinCorrCoeff { get; set; }
    public int CheckResult { get; set; }

    // The following is needed when using PIE_CurveCheck
    public int NumberOfLevels { get; set; }
    public double[] IdealResponse { get; set; } = new double[_maxCurveLevels];
    public double[] ExpectedSD { get; set; } = new double[_maxCurveLevels];
    public double[] DeviationSDLimit { get; set; } = new double[_maxCurveLevels];
    public double[] PrecisionSDLimit { get; set; } = new double[_maxCurveLevels];
    public double[] LowerLogarithmicLimit { get; set; } = new double[_maxCurveLevels];
    public double[] UpperLogarithmicLimit { get; set; } = new double[_maxCurveLevels];
    public double[] LowerResponseLimit { get; set; } = new double[_maxCurveLevels];
    public double[] UpperResponseLimit { get; set; } = new double[_maxCurveLevels];
    public int MaxBadReplicates { get; set; }
    public int ExcludeLowestCalibratorFromMeanDiff { get; set; }

    // Output from PIE_CurveCheck
    public int[] FlaggedPoint { get; set; } = new int[_maxCurvePoints];
    public int[] ArtificialLevelFlg { get; set; } = new int[_maxCurveLevels];
    public double[] ArtificialResponse { get; set; } = new double[_maxCurveLevels];
    public int NumberOfFlaggedPoints { get; set; }
    public int NumberOfArtificialLevels { get; set; }
    public int LowestPointSubstituted { get; set; }
    public int ShapeErrorCV { get; set; }
    public int ShapeErrorFlatcurve { get; set; }
    public int ShapeErrorLowerparthigh { get; set; }
    public int ShapeErrorSingleabove { get; set; }
    public int ShapeErrorSinglebelow { get; set; }
    public int ShapeErrorNorisingtrend { get; set; }
}

public class CurveAdditionalDataTypeCommon
{
    public int StraightLineSwitch { get; set; }
    public int StraightLineNumberOfPoints { get; set; }
    public double StraightLineSwitchConcentration { get; set; }
    public double CurveMinConcentration { get; set; }
    public double CurveMaxConcentration { get; set; }

    // Output from PIE_CurveFit 
    public double coeff_zeropoint_slope { get; set; }
    public double coeff_zeropoint_intercept { get; set; }
}

public class FitOutTypeCommon
{
    public double DetCoef { get; set; }
    public int IterNb { get; set; }
    public int ParamCalc { get; set; }
    public int ParamConverg { get; set; }
    public int Result { get; set; }
}

public class FitParTypeCommon
{
    public int Model { get; set; }
    public int ParamNb { get; set; }
    public double[] ParamValue { get; set; } = new double[5];
    public double[] ParamCovar { get; set; } = new double[25];
}


public enum EnumCalculationTypes
{
    //CalcTypeCutOff = 0,
    //CalcTypeSpline = 1,
    //CalcTypeSplineNonSmooth = 2,
    CalcTypeRodbard4Par = 3,
    CalcTypeRodbard5Par = 4,
    //CalcTypeRichards = 5
    //CalcTypeQuotient = 6
}

public enum EnumRobustWeights
{
    NoRobustWeights = 0,
    Tukey = 1,
    Huber = 2,
    TieDePagano = 3
}

public enum EnumNormalWeights
{
    NoNormalWeights = 0,
    NormalWeightPredicted = 1
}

public enum ModelNumCommon
{
    MdRodbard4,
    MdRodbard5,
    MdRichards,
    MdQuotient
}


//********************************************************************************
/// <summary>
/// PIE curve structure, used together with the PIE class.
/// </summary>
//********************************************************************************
public struct PIECurve
{
    public int NumberOfLevels { get; set; }
    public int NumberOfPoints { get; set; }
    public PIECurve_Level[] Level { get; set; }

    public struct PIECurve_Level
    {
        public double Concentration { get; set; }
        public int NumberOfResponses { get; set; }
        public double[] Response { get; set; }
    }
}

public class CurveData
{
    public double[] Responses { get; set; }
    public double[] Concentrations { get; set; }
}

public class MethodCalculation
{
    public string MethodName { get; set; }
    public int CalibratorsForLeastSquareCalc { get; set; }
    public int CalibratorStartingPoint { get; set; }
    public int CalculationType { get; set; }
    public int NormalWeight { get; set; }
    public int RobustWeight { get; set; }
    public double PredictedVarianceA { get; set; }
    public double PredictedVarianceB { get; set; }
    public double MinCoefficientOfCorrelation { get; set; }
}

public struct PIECurve_IdealCurveLimits
{
    public int NumberOfLevels { get; set; }
    public double[] IdealResponse { get; set; }
    public double[] ExpectedSD { get; set; }
    public double[] DeviationSDLimit { get; set; }
    public double[] PrecisionSDLimit { get; set; }
    public double[] LowerLogarithmicLimit { get; set; }
    public double[] UpperLogarithmicLimit { get; set; }
    public double[] LowerResponseLimit { get; set; }
    public double[] UpperResponseLimit { get; set; }
    public int MaxBadReplicates { get; set; }
    public int ExcludeLowestCalibratorFromMeanDiff { get; set; }
}

public class MethodCalibrator
{
    public string MethodName { get; init; }
    public int ExcludeLowestCalibratorFromMeanDiff { get; init; }
    public int MaxBadReplicates { get; init; }
}

public class MethodCalibratorPoint
{
    public string MethodName { get; init; }
    public string CalibratorName { get; init; }
    public double DefinedConcentration { get; init; }
    public int Replicates { get; init; }
    public double AdjustmentForAlfaBeta1Beta2 { get; init; }

    public double DeviationSDLimit { get; init; }
    public double ExpectedSD { get; init; }
    public double IdealResponse { get; init; }
    public double LowerLogarithmicLimit { get; init; }
    public double LowerResponseLimit { get; init; }
    public double PrecisionSDLimit { get; init; }
    public double UpperLogarithmicLimit { get; init; }
    public double UpperResponseLimit { get; init; }
}

public record MethodData(MethodCalculation Calculation, MethodCalibrator Calibrator, List<MethodCalibratorPoint> Points);

public class FittedCurveData
{
    public int ZeroPointCalculationNumberOfIntervals { get; set; }
    public int NumberOfCalibrationPointsInZeroPointCalculation { get; set; }
    public double ZeroPointSwitchConcentration { get; set; }
    public double ZeroPointCalculationCoefficientAlfa { get; set; }
    public double ZeroPointCalculationCoefficientBeta { get; set; }
    public double CurveMinConcentration { get; set; }
    public double CurveMaxConcentration { get; set; }

    public double CalculationParameterA { get; set; }
    public double CalculationParameterB { get; set; }
    public double CalculationParameterC { get; set; }
    public double CalculationParameterD { get; set; }
    public double CalculationParameterE { get; set; }
    public ModelNumCommon CalculationType { get; set; }
}
