using Pharos.Server.Result.CurveFit.PiePuf;

namespace Pharos.Server.Result.CurveFit;

public class CurveFitterApi : ICurveFitterApi
{
    public CurveFitResultCommon CurveFit(CurveTypeCommon curveType, CurveAdditionalDataTypeCommon additionalData, FitParTypeCommon fitPar)
    {
        var curveT = new CurveType();
        var additionalD = new CurveAdditionalDataType();
        var fitO = new FitOutType();
        var fitP = new FitParType();

        Map(curveType, curveT);
        Map(additionalData, additionalD);
        Map(fitPar, fitP);

        var result = new PiePuf.CurveFit().PIE32_CurveFit(curveT, additionalD, fitO, fitP);

        Map(curveT, curveType);
        Map(additionalD, additionalData);
        Map(fitP, fitPar);
        //Map(fitO, fitOut);

        return new CurveFitResultCommon
        {
            IsCurveFitOk = result == 1,
            IterationsToFit = fitO.IterNb
        };
    }

    public CurveCheckResultCommon CurveCheck(CurveTypeCommon curveType, int straightLineSwitch)
    {
        var curveT = new CurveType();
        Map(curveType, curveT);

        var result = CurveChecker.CurveCheck(curveT, straightLineSwitch);

        Map(curveT, curveType);

        return new CurveCheckResultCommon
        {
            IsCurveCheckOk = result == 1,
            ArtificialLevelFlag = curveT.ArtificialLevelFlg,
            ArtificialResponse = curveT.ArtificialResponse,
            FlaggedPoint = curveT.FlaggedPoint,
            NumberOfArtificialLevels = curveT.NumberOfArtificialLevels,
            NumberOfFlaggedPoints = curveT.NumberOfFlaggedPoints,
            LowestPointSubstituted = curveT.LowestPointSubstituted,
            ShapeErrorCV = curveT.ShapeErrorCV == 1,
            ShapeErrorFlatCurve = curveT.ShapeErrorFlatCurve == 1,
            ShapeErrorSingleAbove = curveT.ShapeErrorSingleAbove == 1,
            ShapeErrorSingleBelow = curveT.ShapeErrorSingleBelow == 1,
            ShapeErrorNoRisingTrend = curveT.ShapeErrorNoRisingTrend == 1,
            ShapeErrorLowerPartHigh = curveT.ShapeErrorLowerPartHigh == 1
        };
    }

    public double ResponseToConcentration(double response, CurveAdditionalDataTypeCommon additionalData, FitParTypeCommon fitPar, double concentrationFactor, int dilutionFactor, out int belowOnAboveCurve)
    {
        var rc = new RespConcType
        {
            Resp = response,
            ConcentrationFactor = concentrationFactor,
            DilutionFactor = dilutionFactor
        };

        var additionalD = new CurveAdditionalDataType();
        Map(additionalData, additionalD);

        var fitP = new FitParType();
        Map(fitPar, fitP);

        new PiePuf.CurveFit().PIE32_RespToConc(rc, additionalD, fitP);
        belowOnAboveCurve = rc.BelowOnAboveCurve;

        //Map(additionalD, additionalData);
        //Map(fitP, fitPar);

        return rc.Conc;
    }

    //private static void Map(FitOutType from, FitOutTypeCommon to)
    //{
    //    to.DetCoef = from.DetCoef;
    //    to.IterNb = from.IterNb;
    //    to.ParamCalc = from.ParamCalc ? 1 : 0;
    //    to.ParamConverg = from.ParamConverg ? 1 : 0;
    //    to.Result = from.Result;
    //}

    //private static void Map(FitOutTypeCommon from, FitOutType to)
    //{
    //    to.DetCoef = from.DetCoef;
    //    to.IterNb = from.IterNb;
    //    to.ParamCalc = from.ParamCalc == 1;
    //    to.ParamConverg = from.ParamConverg == 1;
    //    to.Result = from.Result;
    //}

    private static void Map(FitParTypeCommon from, FitParType to)
    {
        to.Model = from.Model;
        to.ParamCovar = from.ParamCovar;
        to.ParamNb = from.ParamNb;
        to.ParamValue = from.ParamValue;
    }

    private static void Map(FitParType from, FitParTypeCommon to)
    {
        to.Model = from.Model;
        to.ParamCovar = from.ParamCovar;
        to.ParamNb = from.ParamNb;
        to.ParamValue = from.ParamValue;
    }

    private static void Map(CurveAdditionalDataTypeCommon from, CurveAdditionalDataType to)
    {
        to.CurveMaxConcentration = from.CurveMaxConcentration;
        to.CurveMinConcentration = from.CurveMinConcentration;
        to.StraightLine_NumberOfPoints = from.StraightLineNumberOfPoints;
        to.StraightLine_Switch = from.StraightLineSwitch;
        to.StraightLineSwitchConcentration = from.StraightLineSwitchConcentration;
        to.coeff_zeropoint_intercept = from.coeff_zeropoint_intercept;
        to.coeff_zeropoint_slope = from.coeff_zeropoint_slope;
    }

    private static void Map(CurveAdditionalDataType from, CurveAdditionalDataTypeCommon to)
    {
        to.CurveMaxConcentration = from.CurveMaxConcentration;
        to.CurveMinConcentration = from.CurveMinConcentration;
        to.StraightLineNumberOfPoints = from.StraightLine_NumberOfPoints;
        to.StraightLineSwitch = from.StraightLine_Switch;
        to.StraightLineSwitchConcentration = from.StraightLineSwitchConcentration;
        to.coeff_zeropoint_intercept = from.coeff_zeropoint_intercept;
        to.coeff_zeropoint_slope = from.coeff_zeropoint_slope;
    }

    private static void Map(CurveTypeCommon from, CurveType to)
    {
        to.Model = from.Model;
        to.CheckResult = from.CheckResult;
        to.Concentration = from.Concentration;
        to.DeviationSDLimit = from.DeviationSDLimit;
        to.ExcludeLowestCalibratorFromMeanDiff = from.ExcludeLowestCalibratorFromMeanDiff;
        to.ExpectedSD = from.ExpectedSD;
        to.FacWtA = from.FacWtA;
        to.FacWtB = from.FacWtB;
        to.IdealResponse = from.IdealResponse;
        to.LevelIndex = from.LevelIndex;
        to.LowerLogarithmicLimit = from.LowerLogarithmicLimit;
        to.LowerResponseLimit = from.LowerResponseLimit;
        to.MaxBadReplicates = from.MaxBadReplicates;
        to.MaxIterations = from.MaxIterations;
        to.MinCorrCoeff = from.MinCorrCoeff;
        to.NormWt = from.NormWt;
        to.NumberOfLevels = from.NumberOfLevels;
        to.NumberOfPoints = from.NumberOfPoints;
        to.PrecisionSDLimit = from.PrecisionSDLimit;
        to.Response = from.Response;
        to.RobLst = from.RobLst == 1;
        to.RobsWt = from.RobsWt;
        to.UpperLogarithmicLimit = from.UpperLogarithmicLimit;
        to.UpperResponseLimit = from.UpperResponseLimit;

        to.ArtificialLevelFlg = from.ArtificialLevelFlg;
        to.ArtificialResponse = from.ArtificialResponse;
        to.FlaggedPoint = from.FlaggedPoint;
        to.LowestPointSubstituted = from.LowestPointSubstituted;
        to.NumberOfArtificialLevels = from.NumberOfArtificialLevels;
        to.NumberOfFlaggedPoints = from.NumberOfFlaggedPoints;
        to.ShapeErrorCV = from.ShapeErrorCV;
        to.ShapeErrorFlatCurve = from.ShapeErrorFlatcurve;
        to.ShapeErrorLowerPartHigh = from.ShapeErrorLowerparthigh;
        to.ShapeErrorNoRisingTrend = from.ShapeErrorNorisingtrend;
        to.ShapeErrorSingleAbove = from.ShapeErrorSingleabove;
        to.ShapeErrorSingleBelow = from.ShapeErrorSinglebelow;
    }

    private static void Map(CurveType from, CurveTypeCommon to)
    {
        to.Model = from.Model;
        to.CheckResult = from.CheckResult;
        to.Concentration = from.Concentration;
        to.DeviationSDLimit = from.DeviationSDLimit;
        to.ExcludeLowestCalibratorFromMeanDiff = from.ExcludeLowestCalibratorFromMeanDiff;
        to.ExpectedSD = from.ExpectedSD;
        to.FacWtA = from.FacWtA;
        to.FacWtB = from.FacWtB;
        to.IdealResponse = from.IdealResponse;
        to.LevelIndex = from.LevelIndex;
        to.LowerLogarithmicLimit = from.LowerLogarithmicLimit;
        to.LowerResponseLimit = from.LowerResponseLimit;
        to.MaxBadReplicates = from.MaxBadReplicates;
        to.MaxIterations = from.MaxIterations;
        to.MinCorrCoeff = from.MinCorrCoeff;
        to.NormWt = from.NormWt;
        to.NumberOfLevels = from.NumberOfLevels;
        to.NumberOfPoints = from.NumberOfPoints;
        to.PrecisionSDLimit = from.PrecisionSDLimit;
        to.Response = from.Response;
        to.RobLst = from.RobLst ? 1 : 0;
        to.RobsWt = from.RobsWt;
        to.UpperLogarithmicLimit = from.UpperLogarithmicLimit;
        to.UpperResponseLimit = from.UpperResponseLimit;

        to.ArtificialLevelFlg = from.ArtificialLevelFlg;
        to.ArtificialResponse = from.ArtificialResponse;
        to.FlaggedPoint = from.FlaggedPoint;
        to.LowestPointSubstituted = from.LowestPointSubstituted;
        to.NumberOfArtificialLevels = from.NumberOfArtificialLevels;
        to.NumberOfFlaggedPoints = from.NumberOfFlaggedPoints;
        to.ShapeErrorCV = from.ShapeErrorCV;
        to.ShapeErrorFlatcurve = from.ShapeErrorFlatCurve;
        to.ShapeErrorLowerparthigh = from.ShapeErrorLowerPartHigh;
        to.ShapeErrorNorisingtrend = from.ShapeErrorNoRisingTrend;
        to.ShapeErrorSingleabove = from.ShapeErrorSingleAbove;
        to.ShapeErrorSinglebelow = from.ShapeErrorSingleBelow;
    }
}