namespace Pharos.Server.Result.CurveFit.PiePuf;

internal class CurveType
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
    public bool RobLst { get; set; }
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
    public int ShapeErrorFlatCurve { get; set; }
    public int ShapeErrorLowerPartHigh { get; set; }
    public int ShapeErrorSingleAbove { get; set; }
    public int ShapeErrorSingleBelow { get; set; }
    public int ShapeErrorNoRisingTrend { get; set; }
}

internal class FitInType
{
    public int MaxIterations { get; set; }
    public int DNb { get; set; }
    public double[] Xg { get; set; } = new double[400];
    public double[] Yg { get; set; } = new double[400];
    public int Model { get; set; }
    public int NormWt { get; set; }
    public int RobsWt { get; set; }
    public bool RobLst { get; set; }
    public double FacWtA { get; set; }
    public double FacWtB { get; set; }
}

internal class FitOutType
{
    public double DetCoef { get; set; }
    public int IterNb { get; set; }
    public bool ParamCalc { get; set; }
    public bool ParamConverg { get; set; }
    public int Result { get; set; }
}

internal class FitParType
{
    public int Model { get; set; }
    public int ParamNb { get; set; }
    public double[] ParamValue { get; set; } = new double[5];
    public double[] ParamCovar { get; set; } = new double[25];
}

internal class FitInRec
{
    public int DNb { get; set; }
    public int MaxIterations { get; set; }
    public double[] Xg { get; set; }
    public double[] Yg { get; set; }
    public byte Model { get; set; }
    public byte NormWt { get; set; }
    public byte RobsWt { get; set; }
    public bool RobLst { get; set; }
    public double FacWtA { get; set; }
    public double FacWtB { get; set; }
}

internal class FitOutRec
{
    public double DetCoef { get; set; }
    public int Result { get; set; }
    public int IterNb { get; set; }
    public bool ParamConverg { get; set; }
    public bool ParamCalc { get; set; }
}

internal class RespConcType
{
    public double Conc { get; set; }
    public double Resp { get; set; }
    public int DilutionFactor { get; set; }
    public double ConcentrationFactor { get; set; }
    public int BelowOnAboveCurve { get; set; }
}

internal class CurveAdditionalDataType
{
    public int StraightLine_Switch { get; set; }
    public int StraightLine_NumberOfPoints { get; set; }
    public double StraightLineSwitchConcentration { get; set; }
    public double CurveMinConcentration { get; set; }
    public double CurveMaxConcentration { get; set; }

    // Output from PIE_CurveFit 
    public double coeff_zeropoint_slope { get; set; }
    public double coeff_zeropoint_intercept { get; set; }
}

internal class VarType
{
    public double XValue { get; set; }
    public double YValue { get; set; }
}

internal class FitParRec
{
    public byte Model { get; set; }
    public int ParamNb { get; set; }
    public double[] ParamValue { get; set; }
    public double[] ParamCovar { get; set; }
}

internal class DataRec
{
    public int DataNb { get; set; } /*Number of data points*/
    public int VarNb { get; set; } /*Number of individual variables*/
    public double[] X { get; set; } /*Experimental dependent variables*/
    public double[] Y { get; set; } /*Experimental independent variable*/
    public double[] NormalWeight { get; set; } /*Normal and robust weights for each (x,y)*/
    public double[] RobustWeight { get; set; }
    public int Len { get; set; } /*Allocated length (internal use only)*/
}

internal class LOC_LineSearch
{
    public ParamRec Param { get; set; }
    public DataRec Data { get; set; }
    public ControlRec Control { get; set; }
    public int MxPos { get; set; }
    public double MxVal { get; set; }
    public double[] OldParamVal { get; set; }
}

internal enum ModelNum
{
    MdRodbard4,
    MdRodbard5,
    MdRichards
}

internal enum ResultType
{
    /* --- Working conditions --- */
    ParamDiverging,
    ErrorDiverging,
    StartDiverging,
    GridSearchSuccess,

    MatrixSingularFound,
    VectorNotDownHill,
    VectorTooLong,
    LineSearchDone,

    /* --- Stopping conditions --- */
    NoEnoughData,
    NoEnoughRepl,
    AllwaysStops,
    MxIterReached,

    ConvergAtDeriv,
    ConvergAtParam,
    ConvergAtSqErr,
    ConvergAttained
}

internal class ParamRec
{
    public int ParamNb { get; set; } /*Number of parameters values*/
    public int CovarNb { get; set; } /*Number of parameter covariances*/
    public double[] Value { get; set; } /*Estimated parameter values*/
    public double[] Covar { get; set; } /*Estimated parameter covariances*/
    public int Len { get; set; } /*Allocated length (internal use only)*/
}

internal enum WeightType
{
    NoNWt, /* No weight */
    CalcNWt, /* Recalculate externally by PrepWeightProc */
    UserNWt, /* Given externally once through NormalWeight */
    PredNWt /* Calculate once wi=1/åiý using PredVarFunc */
}

internal enum RobustType
{
    NoRWt, /* No weight */
    TukeyRWt, /* Mosteller-Tukey */
    HuberRWt, /* Huber 1973 */
    TukBiRWt, /* Beaton-Tukey biweight */
    TiedeRWt, /* Tiede-Pagano 1979 */
    GivenRWt /* Calculated by user-specified RobustFunc */
}

internal class ModelRec
{
    /* --- Number of parameters --- */
    public int PNb { get; set; }

    /* --- Model denomination --- */
    public string Comment { get; set; } = string.Empty;

    /* --- Model procedure name --- */
    public UserFuncType ModelFunc { get; set; }

    //# ifdef UseDeriv
    /* --- Derivative procedure name --- */
    public DerivateFuncType DerivFunc { get; set; }

    //#endif  /*UseDeriv*/
    /* --- Safety devices --- */
    public ParamsOKFuncType Constraints { get; set; }
    public double LambdaVal { get; set; }
    public double GammaVal { get; set; }
}

internal class ControlRec
{
    public int TotalDataNb { get; set; } /*Original data number in replicated models*/
    public WeightType NormalWeightMode { get; set; } /*Normal weighting mode*/
    public RobustType RobustWeightMode { get; set; } /*Robust weighting mode*/
    public double RobustConst { get; set; } /*Robustness Constant Value*/
    public bool DoGridSearch { get; set; } /*Final convergence check (GridSearch)*/
    public double ConvergDeriv { get; set; } /*Convergence test for derivatives*/
    public double ConvergParam { get; set; } /*Convergence test for parameters*/
    public double ConvergSqErr { get; set; } /*Convergence test for sum squares*/
    public double ConvergGrid { get; set; } /*Convergence test for sum squares in Grid*/
    public double DefaultDeltaP { get; set; } /*Default DeltaP for DerivFunc*/
    public double Beta { get; set; } /*Used in the bisection routine*/
    public double Gamma { get; set; } /*Test to reject too big estim.{ get; set; } 0=inactive*/
    public double Lambda { get; set; } /*Damping factor (modifies MatrixFF)*/
    public double EstimSqErr { get; set; } /*Estimated (weighted) SqErr to calc H,à*/
    public double UnWtTotErr { get; set; } /*Unweighted residual sum of absolute error*/
    public double UnWtSqErr { get; set; } /*Unweighted residual sum of squares*/
    public double UnWtStdDev { get; set; } /*Unweighted standard error*/
    public double TotErr { get; set; } /*Residual sum of absolute error*/
    public double SqErr { get; set; } /*Residual sum of squares: RSS*/
    public double StdDev { get; set; } /*Standard error: RSS/(n-p)*/
    public double SqErrY { get; set; } /*Residual sum of squares of Y: RSSy*/
    public double StdDevY { get; set; } /*Standard deviation of Y: RSSy/(n-p)*/
    public double DetCoef { get; set; } /*Determination coefficient*/
    public double DetCoefC { get; set; } /*Corrected to compensate parameter number*/
    public double Ftest { get; set; } /*Fp-1,n-p test H:p=0 except for one param.*/
    public int Cycle { get; set; } /*Number of iterations to show results*/
    public int MxIter { get; set; } /*Maximum Iteration Number*/
    public int IterNb { get; set; } /*Iteration Number: checked with mod Cycle*/
    public int Result { get; set; } /*Result Code*/
    public UserFuncType UserFunc { get; set; }
    public DerivateFuncType DerivateFunc { get; set; }
    public ParamsOKFuncType ParamsOKFunc { get; set; }
    public RobustFuncType RobustFunc { get; set; }
    public LstSqProcType PrepWeightProc { get; set; }
    public PredVarFuncType PredVarFunc { get; set; }
}

internal struct CurveCalculationType
{
    public bool CurveFittedFlag;
    public EnumCalculationTypes CalculationType;
    public double CalculationParameterA;
    public double CalculationParameterB;
    public double CalculationParameterC;
    public double CalculationParameterD;
    public double CalculationParameterE;
    public double ZeroPointSwitchConcentration;
    public double ZeroPointAlfa;
    public double ZeroPointBeta;
};

internal delegate double UserFuncType(double[] p, double x);

internal delegate bool ParamsOKFuncType(ParamRec Param);

internal delegate double DerivateFuncType(UserFuncType UserFunc, ParamRec Param, double X, double[] Deriv);

internal delegate double RobustFuncType(double z, double a);

internal delegate double PredVarFuncType(double Av);

internal delegate void LstSqProcType(ParamRec Param, DataRec Data, ControlRec Control);

internal delegate void CallBackProcType(int IterNb, char EchoCh);