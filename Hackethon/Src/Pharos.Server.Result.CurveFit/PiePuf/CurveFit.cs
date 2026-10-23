// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable JoinDeclarationAndInitializer
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
// ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
// ReSharper disable MergeIntoPattern
// ReSharper disable CommentTypo
// ReSharper disable GrammarMistakeInComment
#pragma warning disable IDE0010 // Populate switch
#pragma warning disable IDE0072 // Populate switch

namespace Pharos.Server.Result.CurveFit.PiePuf;

internal class CurveFit
{
    private const double NearlyZero = 1.0e-100;
    private const double BigValue = 1.0 / NearlyZero;
    private const double MaxArrSize = 65520L;
    private const double MaxFloatArr = MaxArrSize / sizeof(double);
    private const int SHORT_MAX = int.MaxValue; //  ((short)(((unsigned short) -1) >> 1))
    private const double NEAR_ZERO = 0.0005;
    private const int METHOD_MINZEROPOINT_NPOINTS = 2;

    private const double _safeExp = 100.0;
    private const int _mxMNb = 2;  /* --- *** Maximum number of models     --- */
    private const double _piValue = 3.14159265358979323846;
    private const double _piHalf = _piValue / 2;
    private const int _mxIterDefault = 1000;
    private const int _mxParam = 10;
    private const int _gridSize = 4;

    private FitInRec _fitData;
    private FitOutRec _fitResult;
    private byte _fitModel;
    private CallBackProcType _userCallBack;
    private bool _isInitialized;
    private double _determ;
    private double _dflDeltaP;
    private double _dflDeltaF;
    private double _constantMean;
    private double _constantVar;
    private int _mxIterTotal = _mxIterDefault;

    private readonly ModelRec[] _modelList;
    private readonly ParamRec _param = new();
    private readonly DataRec _data = new();
    private readonly ControlRec _control = new()
    {
        NormalWeightMode = WeightType.NoNWt,
        RobustWeightMode = RobustType.NoRWt,
        DoGridSearch = true,
        ConvergDeriv = 5.0e-07,
        ConvergParam = 5.0e-15,
        ConvergSqErr = 1.0e-05,
        ConvergGrid = 1.0e-10,
        DefaultDeltaP = 1.0e-12,
        Beta = 1.0e-05
    };

    internal CurveFit()
    {
        _modelList =
        [
            new ModelRec{ PNb=4, Comment="Four parameter Rodbard logistic function", ModelFunc=Rodbard4, DerivFunc=DerivRodbard4, Constraints=RodbardConstraint, GammaVal=100.0, LambdaVal=0.001 },
            new ModelRec{ PNb=5, Comment="Five parameter Rodbard logistic function", ModelFunc=Rodbard5, DerivFunc=DerivRodbard5, Constraints=RodbardConstraint, GammaVal=100.0, LambdaVal=0.001 }
        ];
    }

    //=========================================================
    // FUNCTION NAME:	PIE32_CurveFit
    //
    // DESCRIPTION:		Perform a curve fit
    //
    // ARGUMENTS:       Curve
    //						Curve data and flags (in/out)
    //                  CurveAdditionalData
    //                      Additional curve data (in/out)
    //                  FitOut
    //						Various result data (out)
    //					FitPar
    //						The curve parameters (out)
    //					
    // RETURNS:			0: Failed
    //                  1: OK
    //=========================================================
    public int PIE32_CurveFit(
        CurveType curve,
        CurveAdditionalDataType curveAdditionalData,
        FitOutType fitOut,
        FitParType fitPar
        )
    {
        curveAdditionalData.coeff_zeropoint_slope = 0.0;
        curveAdditionalData.coeff_zeropoint_intercept = 0.0;

        // Check that data is OK
        if (curve.CheckResult == 1)
        {
            if (curve.NumberOfFlaggedPoints > curve.MaxBadReplicates)
            {
                return 0;
            }

            if (curve.LowestPointSubstituted == 1)
            {
                return 0;
            }
        }

        // Copy data from CurveRec to FitInRec
        var fitIn = new FitInType
        {
            MaxIterations = curve.MaxIterations,
            Model = curve.Model,
            NormWt = curve.NormWt,
            RobsWt = curve.RobsWt,
            RobLst = curve.RobLst,
            DNb = 0
        };

        for (var i = 0; i < curve.NumberOfPoints; i++)
        {
            if (curve.CheckResult == 0 || curve.FlaggedPoint[i] == 0)
            {
                if (curve.LevelIndex[i] > curveAdditionalData.StraightLine_Switch)
                {
                    fitIn.Xg[fitIn.DNb] = curve.Concentration[i];
                    fitIn.Yg[fitIn.DNb] = curve.Response[i];
                    fitIn.DNb++;
                }
            }
        }

        long a = 0; //SZ, 'a' is being reused in the next loop iteration (if hit once it might always get hit)

        // Include Artificial points  
        if (curve.CheckResult == 1)
        {
            for (var i = curveAdditionalData.StraightLine_Switch; i < curve.NumberOfLevels; i++)
            {
                if (curve.ArtificialLevelFlg[i] == 1)
                {
                    for (var p = 0; p < curve.NumberOfPoints; p++)
                    {
                        if (curve.LevelIndex[p] - 1 == i)
                        {
                            a = p;
                            break;
                        }
                    }

                    if (a >= 0)
                    {
                        fitIn.Xg[fitIn.DNb] = curve.Concentration[a];
                        fitIn.Yg[fitIn.DNb] = curve.ArtificialResponse[i];
                        fitIn.DNb++;
                    }
                }
            }
        }

        fitIn.FacWtA = curve.FacWtA;
        fitIn.FacWtB = curve.FacWtB;

        var returnValue = PUF32_FitRun(fitIn, fitOut, fitPar);

        if (!CurveFitZeroPoint(curve, curveAdditionalData, fitPar))
        {
            returnValue = 0;
        }

        if (curve.MinCorrCoeff > NEAR_ZERO && fitOut.DetCoef < curve.MinCorrCoeff)
        {
            return 0;
        }

        return returnValue;
    }


    /*****************************************************************************
    FUNCTION:  CurveFitZeroPoint

    ARGUMENTS: Curve               - Calibration curve, to calc slope 
                                     and intercept for (in/out)
               CurveAdditionalData - Additional curve data (in/out)
               FitPar              - Curve fit parameters from PUF

    RETURNS:   True  - Zero-point slope and intercept calculated or not used
               False - Failed
    -----------------------------------------------------------------------------
    This function is called after successfully fitting a curve
    to calculate the least-square line used for lower parts of the curve in
    the zero-point calculation.
    ****************************************************************************/
    private bool CurveFitZeroPoint(
        CurveType curve,
        CurveAdditionalDataType curveAdditionalData,
        FitParType fitPar
        )
    {
        var ndata = 0;
        var sum_x = 0.0;
        var sum_y = 0.0;
        var sum_xx = 0.0;
        var sum_xy = 0.0;

        if (curveAdditionalData.StraightLine_Switch == 0)  /* Zero point calculation not enabled */
        {
            return true;
        }

        if (curveAdditionalData.StraightLine_NumberOfPoints < METHOD_MINZEROPOINT_NPOINTS)   /* Should not occur, but for safety  */
        {
            return false;
        }

        // Check that data is OK
        if (curve.CheckResult == 1)
        {
            if (curve.NumberOfFlaggedPoints > curve.MaxBadReplicates)
            {
                return false;
            }

            if (curve.LowestPointSubstituted == 1)
            {
                return false;
            }
        }

        // Find various sums needed for calculation
        for (var i = 0; i < curve.NumberOfPoints; i++)
        {
            if (curve.LevelIndex[i] <= curveAdditionalData.StraightLine_NumberOfPoints)
            {
                if (curve.FlaggedPoint[i] != 1 || curve.CheckResult != 1)
                {
                    ndata++;
                    sum_x += curve.Concentration[i];
                    sum_y += curve.Response[i];
                    sum_xx += curve.Concentration[i] * curve.Concentration[i];
                    sum_xy += curve.Concentration[i] * curve.Response[i];
                }
            }
        }

        var avg_x = sum_x / ndata;
        var avg_y = sum_y / ndata;

        var slope = (sum_xy - (sum_x * avg_y)) / (sum_xx - (sum_x * avg_x));
        if (slope <= 0.0)
        {
            return false;
        }

        var int_org = avg_y - (slope * avg_x);

        var SwitchConc = curveAdditionalData.StraightLineSwitchConcentration;

        var Variable = new VarType
        {
            XValue = SwitchConc
        };

        if (!PUF32_Forward(Variable, fitPar))
        {
            return false;
        }

        var rod = Variable.YValue;

        var lin = int_org + (SwitchConc * slope);
        var corr = lin - rod;
        var int_mod = int_org - corr;

        curveAdditionalData.coeff_zeropoint_slope = slope;
        curveAdditionalData.coeff_zeropoint_intercept = int_mod;

        return true;
    }


    //=========================================================
    // FUNCTION NAME:	PIE32_ConcToResp
    //
    // DESCRIPTION:		Calculate response from concentration.
    //
    // ARGUMENTS:       RespConc (in/out)
    //						Structure containing Concentration
    //                      and will be filled with Response.
    //                  CurveAdditionalData
    //                      Curve additional data (in)
    //					FitPar
    //						The curve parameters (in)
    //					
    // RETURNS:			0: Failed
    //                  1: OK
    //=========================================================

    //private int PIE32_ConcToResp(
    //    RespConcType RespConc,
    //    CurveAdditionalDataType CurveAdditionalData,
    //    FitParType FitPar
    //    )
    //{
    //    int ReturnValue;
    //    VarType Variable = new();

    //    if (CurveAdditionalData.StraightLine_Switch > 0)
    //    {
    //        if (RespConc.Conc <= CurveAdditionalData.StraightLine_SwitchConc)
    //        {
    //            if (CurveAdditionalData.coeff_zeropoint_slope <= 0.0)
    //            {
    //                RespConc.Resp = 0.0;
    //                return 0;
    //            }

    //            RespConc.Resp = CurveAdditionalData.coeff_zeropoint_intercept + RespConc.Conc * CurveAdditionalData.coeff_zeropoint_slope;

    //            if (RespConc.Resp < 0.0)
    //            {
    //                RespConc.Resp = 0.0;
    //            }

    //            return 1;
    //        }
    //    }

    //    Variable.Xvalue = RespConc.Conc;
    //    ReturnValue = PUF32_Forward(Variable, FitPar);
    //    RespConc.Resp = Variable.Yvalue;

    //    return ReturnValue;
    //}




    //=========================================================
    // FUNCTION NAME:	PIE32_RespToConc
    //
    // DESCRIPTION:		Calculate concentration from response.
    //
    // ARGUMENTS:       RespConc (int/out)
    //						Structure containing Response and
    //                      will be filled with Concentration.
    //                  CurveAdditionalData
    //                      Curve additional data (in)
    //					FitPar
    //						The curve parameters (in)
    //					
    // RETURNS:			0: Failed
    //                  1: OK
    //=========================================================
    public int PIE32_RespToConc(
        RespConcType respConc,
        CurveAdditionalDataType curveAdditionalData,
        FitParType fitPar
        )
    {
        var variable = new VarType
        {
            YValue = respConc.Resp
        };

        var returnValue = PUF32_Reverse(variable, fitPar);
        respConc.Conc = variable.XValue;
        respConc.BelowOnAboveCurve = 0;

        if (respConc.Conc < curveAdditionalData.StraightLineSwitchConcentration)
        {
            if (curveAdditionalData.coeff_zeropoint_slope <= 0.0)
            {
                respConc.Conc = 0.0;
                return 0;
            }

            respConc.Conc = (respConc.Resp - curveAdditionalData.coeff_zeropoint_intercept) / curveAdditionalData.coeff_zeropoint_slope;
            if (respConc.Conc < 0)
            {
                respConc.Conc = 0.0;
            }

            returnValue = 1;
        }

        if (returnValue == 1)
        {
            if (respConc.Conc > curveAdditionalData.CurveMaxConcentration)
            {
                respConc.BelowOnAboveCurve = 1;
            }

            if (respConc.Conc < curveAdditionalData.CurveMinConcentration)
            {
                respConc.BelowOnAboveCurve = -1;
            }
        }

        if (respConc.ConcentrationFactor > NEAR_ZERO)
        {
            respConc.Conc *= respConc.ConcentrationFactor;
        }

        if (respConc.DilutionFactor > 0)
        {
            respConc.Conc *= respConc.DilutionFactor;
        }

        return returnValue;
    }

    //=========================================================
    // FUNCTION NAME:	PUF32_FitRun
    //
    // DESCRIPTION:		Perform a curve fit
    //
    // ARGUMENTS:       FitIn
    //						Curve data and flags (in)
    //                  FitOut
    //						Various result data (out)
    //					FitPar
    //						The parameters (out)
    //					
    // RETURNS:			Result code
    //=========================================================
    private int PUF32_FitRun(
        FitInType fitIn,
        FitOutType fitOut,
        FitParType fitPar
        )
    {
        FitInRec fitInPUF = new()
        {
            MaxIterations = fitIn.MaxIterations,
            DNb = fitIn.DNb,
            Xg = fitIn.Xg,
            Yg = fitIn.Yg,
            Model = (byte)fitIn.Model,
            NormWt = (byte)fitIn.NormWt,
            RobsWt = (byte)fitIn.RobsWt,
            RobLst = fitIn.RobLst,
            FacWtA = fitIn.FacWtA,
            FacWtB = fitIn.FacWtB
        };

        var fitOutPUF = new FitOutRec();
        var fitParPUF = new FitParRec();
        var returnValue = PUFFitRun(fitInPUF, fitOutPUF, fitParPUF, null) ? 1 : 0;

        fitOut.DetCoef = fitOutPUF.DetCoef;
        fitOut.Result = fitOutPUF.Result;
        fitOut.IterNb = fitOutPUF.IterNb;
        fitOut.ParamConverg = fitOutPUF.ParamConverg;
        fitOut.ParamCalc = fitOutPUF.ParamCalc;
        fitPar.Model = fitParPUF.Model;
        fitPar.ParamNb = fitParPUF.ParamNb;


        for (var i = 0; i < fitPar.ParamNb; i++)
        {
            fitPar.ParamValue[i] = fitParPUF.ParamValue[i];
        }
        for (var i = 0; i < fitPar.ParamNb * fitPar.ParamNb; i++)
        {
            fitPar.ParamCovar[i] = fitParPUF.ParamCovar[i];
        }

        return returnValue;
    }

    //=========================================================
    // FUNCTION NAME:	PUF32_Forward
    //
    // DESCRIPTION:		Calculate response from concentration.
    //
    // ARGUMENTS:       Variable
    //						Structure containing X and Y value
    //					FitPar 
    //						The curve parameters
    //					
    // RETURNS:			Result code
    //=========================================================
    private bool PUF32_Forward(VarType variable, FitParType fitPar)
    {
        var fitParPUF = new FitParRec
        {
            Model = (byte)fitPar.Model,
            ParamNb = fitPar.ParamNb,
            ParamValue = fitPar.ParamValue,
            ParamCovar = fitPar.ParamCovar
        };

        var xValue = variable.XValue;

        var returnValue = PUFForward(fitParPUF, xValue);

        variable.YValue = returnValue;

        if (returnValue.EqualsOrCloseTo(BigValue))
        {
            return false;
        }

        return true;
    }

    //Calculate concentration from response, varType: containing X and Y, fitPar: the curve parameters
    private int PUF32_Reverse(
        VarType variable,
        FitParType fitPar
        )
    {
        var fitParPUF = new FitParRec();
        var indata = variable.YValue;

        fitParPUF.Model = (byte)fitPar.Model;
        fitParPUF.ParamNb = fitPar.ParamNb;
        fitParPUF.ParamValue = fitPar.ParamValue;
        fitParPUF.ParamCovar = fitPar.ParamCovar;

        var returnValue = PUFReverse(fitParPUF, indata);

        variable.XValue = returnValue;

        //	if (returnValue == BigValue){
        //		return (0);  /*Unsuccessful return*/
        //	}

        return 1;
    }

    private static double Exp(double x)
    {
        if (Fabs(x) < _safeExp)
        {
            return MathExp(x);
        }

        if (x > 0.0)
        {
            return BigValue;
        }
        else
        {
            return NearlyZero;
        }
    }

    private static double Power(double x, double y)
    {
        if (x < 0.0)
        {
            return -BigValue;
        }

        if (x == 0.0)
        {
            return 0.0;
        }

        if (y.EqualsOrCloseTo(1.0))
        {
            return x;
        }

        return Exp(y * Log(x));
    }

    private static double LeftAsymptote(double[] P)
    {
        return P[3] > 0.0 ? P[0] : P[1];
    }

    private static double RightAsymptote(double[] P)
    {
        return P[3] > 0.0 ? P[1] : P[0];
    }

    private double Rodbard4(double[] P, double X)
    {
        if (X <= NearlyZero)
        {
            return LeftAsymptote(P);
        }

        if (X >= BigValue)
        {
            return RightAsymptote(P);
        }

        var pw = Power(X / P[2], P[3]);
        if (pw >= BigValue)
        {
            return RightAsymptote(P);
        }

        return P[1] + ((P[0] - P[1]) / (1.0 + pw));
    }

    private double Rodbard5(double[] P, double X)
    {
        if (X <= NearlyZero)
        {
            return LeftAsymptote(P);
        }

        if (X >= BigValue)
        {
            return RightAsymptote(P);
        }

        var pw1 = Power(2.0 / ((P[2] / X) + 1.0), P[3]);
        var pw2 = Power((1.0 + (X / P[2])) / 2.0, P[4]);
        if (pw1 >= BigValue || pw2 >= BigValue)
        {
            return RightAsymptote(P);
        }

        return P[1] + ((P[0] - P[1]) / (1.0 + (pw1 * pw2)));
    }

    private double DerivRodbard4(UserFuncType userFunc, ParamRec param, double x, double[] deriv)
    {
        //Out of range: revert to numerical derivative
        if (x <= NearlyZero)
        {
            return DerivFunc(userFunc, param, x, deriv);
        }

        var a = param.Value[0];
        var b = param.Value[1];
        var c = param.Value[2];
        var d = param.Value[3];
        var x0 = x;

        var xc = x0 / c;
        var xcd = Power(xc, d);
        var Q = 1.0 / (xcd + 1.0);
        var abQ = (a - b) * Q;
        var abQQx = abQ * Q * xcd;

        var Func = b + abQ;
        deriv[0] = Q;
        deriv[1] = 1.0 - Q;
        deriv[2] = abQQx * d / c;
        deriv[3] = -abQQx * Log(xc);
        return Func;
    }

    private double DerivRodbard5(UserFuncType userFunc, ParamRec param, double x, double[] deriv)
    {
        //Out of range: revert to numerical derivative
        if (x <= NearlyZero)
        {
            return DerivFunc(userFunc, param, x, deriv);
        }

        var a = param.Value[0];
        var b = param.Value[1];
        var c = param.Value[2];
        var d = param.Value[3];
        var e = param.Value[4];
        var x0 = x;

        var xc = x0 / c;
        var xcd = Power(xc, d);
        var xc2 = (xc + 1.0) / 2.0;
        var xcx = x0 / (c + x0);
        var ed = e - d;
        var xc2ed = Power(xc2, ed);
        var xcdxc2ed = xcd * xc2ed;
        var Q = 1.0 / (1.0 + xcdxc2ed);
        var abQ = (a - b) * Q;
        var abQQxx = abQ * Q * xcdxc2ed;

        var Func = b + abQ;
        deriv[0] = Q;
        deriv[1] = 1.0 - Q;
        deriv[2] = abQQxx * (d + (ed * xcx)) / c;
        deriv[3] = -abQQxx * Log(2.0 * xcx);
        deriv[4] = -abQQxx * Log(xc2);
        return Func;
    }

    private static bool RodbardConstraint(ParamRec param)
    {
        /* --- x divisor must be positive. Otherwise, reject the estimate --- */
        var ok = param.Value[2] > 0.0 && param.Value[1] > param.Value[0];
        return ok;
    }

    private void GetModelData(ModelNum model, ParamRec param, DataRec data, ControlRec control)
    {
        if ((byte)model >= 32 ||
            ((1 << (byte)model) & ((1 << (_mxMNb + 1)) - (1 << 0))) == 0)
        {
            model = 0;
        }

        var modelData = _modelList[(byte)model];
        param.ParamNb = modelData.PNb;
        param.CovarNb = modelData.PNb * modelData.PNb;
        data.VarNb = 1;
        control.Gamma = modelData.GammaVal;
        control.Lambda = modelData.LambdaVal;
        control.UserFunc = modelData.ModelFunc;

        control.DerivateFunc = modelData.DerivFunc;

        control.ParamsOKFunc = modelData.Constraints;
        /* --- Define depending on the precision of the Float type --- */
        control.DefaultDeltaP = Exp(-Log(10.0) * sizeof(double));
        control.ConvergParam = 1.0e-10;
        control.ConvergGrid = 0.00001;
    }

    private static bool StartParams(ParamRec param, DataRec data, ModelNum model, double maxYscale, bool normalize, bool autoStart)
    {
        double Y, MinY, MaxY, MinYc, MaxYc, Factor;
        double[] DataX, DataY;

        /* --- Self-assignment to avoid warnings in C --- */
        //            Control = Control;

        /* --- Now find some starting parameter values --- */
        if (!autoStart)
        {
            return true;
        }

        if (((1 << (int)model) & ((1 << ((int)ModelNum.MdRichards + 1)) - (1 << (int)ModelNum.MdRodbard4))) == 0)
        {
            model = ModelNum.MdRodbard4;
        }

        /* --- Find extreme data values --- */
        MinY = data.Y[0];
        MaxY = MinY;
        for (var n = 1; n < data.DataNb; n++)
        {
            Y = data.Y[n];
            if (MinY > Y)
            {
                MinY = Y;
            }

            if (MaxY < Y)
            {
                MaxY = Y;
            }
        }

        /* --- Normalize input data values (y) when appropriate --- */
        if (normalize)
        {
            MaxY /= maxYscale;
            for (var n = 0; n < data.DataNb; n++)
            {
                data.Y[n] /= MaxY;
            }

            MinY /= MaxY;
            MaxY = maxYscale;
        }

        /* --- Modify extremes to avoid zero problems --- */
        Factor = 0.1;
        MinYc = MinY * (1.0 - Factor);
        MaxYc = MaxY * (1.0 + Factor);
        if (MinYc <= 0.0)
        {
            MinYc = MinY - ((MaxY - MinY) * Factor);
        }

        DataX = new double[data.DataNb];
        DataY = new double[data.DataNb];

        switch (model)
        {
            case ModelNum.MdRodbard4:
                /* --- Rodbard4 --- */
                /* --- Logistic transform of Rodbard4 --- */
                /* --- ln[(y-a)/(b-y)] = d*ln(x) - d*ln(c) --- */
                var DNb = 0;
                for (var n = 0; n < data.DataNb; n++)
                {
                    var X = data.X[n * data.VarNb];
                    Y = data.Y[n];
                    /* --- Discard X=0 to calculate estimate --- */
                    if (X > NearlyZero)
                    {
                        DataX[DNb] = Log(X);
                        DataY[DNb] = Log((Y - MinYc) / (MaxYc - Y));
                        DNb++;
                    }
                }
                /* --- Linear regression to calculate starting parameters --- */
                RegrLinear(DataX, DataY, DNb, /*out _,*/ out var SlopeX, out var OffsetX/*, out _, out _*/);
                param.Value[0] = MinYc;
                param.Value[1] = MaxYc;
                param.Value[2] = Exp(-(OffsetX / SlopeX));
                param.Value[3] = SlopeX;
                break;
            /*Model MdRodbard4,MdRichards*/

            case ModelNum.MdRodbard5:
                param.Value[4] = param.Value[3];
                break;

        }/*switch Model*/

        return true;
    }

    private double PredVar(double av)
    {
        double vr;

        av = Fabs(av);
        if (av < NearlyZero)
        {
            vr = NearlyZero;
        }
        else
        {
            vr = _fitData.FacWtB * Power(av, _fitData.FacWtA);
        }

        return vr;
    }

    private void ShowResults(ParamRec param, DataRec data, ControlRec control)
    {
        char echoCh;

        /* --- Common actions --- */
        if (control.Result == 0)
        {
            echoCh = '.';
        }
        else
        {
            if ((control.Result & (1 << (int)ResultType.MatrixSingularFound)) != 0)
            {
                echoCh = 'M';
            }
            else
            {
                if ((control.Result & (1 << (int)ResultType.LineSearchDone)) != 0)
                {
                    echoCh = 'L';
                }
                else
                {
                    if ((control.Result & (1 << (int)ResultType.GridSearchSuccess)) != 0)
                    {
                        echoCh = 'G';
                    }
                    else
                    {
                        if ((control.Result & (1 << (int)ResultType.ConvergAttained)) != 0)
                        {
                            echoCh = 'C';
                        }
                        else
                        {
                            if ((control.Result & ((1 << (int)ResultType.ParamDiverging) |
                                   (1 << (int)ResultType.ErrorDiverging) | (1 << (int)ResultType.StartDiverging))) != 0)
                            {
                                echoCh = 'D';
                            }
                            else
                            {
                                if ((control.Result & ((1 << (int)ResultType.VectorNotDownHill) |
                                                        (1 << (int)ResultType.VectorTooLong))) != 0)
                                {
                                    echoCh = 'V';
                                }
                                else
                                {
                                    if ((control.Result & (1 << (int)ResultType.MxIterReached)) != 0)
                                    {
                                        echoCh = 'X';
                                    }
                                    else
                                    {
                                        echoCh = 'E';
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        var last = ParamsConverged(control) || ParamsMxIter(control) || ParamsFailed(control);

        if ((ParamsConverged(control) || ParamsMxIter(control))
            && _fitModel != _fitData.Model)
        {
            /* --- Rodbard>4, by steps --- */
            if (ParamsMxIter(control))
            {
                echoCh = 'X';
            }
            else
            {
                echoCh = '*';
                last = false;
                control.Result = 0;
            }
            /* --- Weights were already calculated at the Rodbard4 fit --- */
            if (control.NormalWeightMode == WeightType.PredNWt)
            {
                control.NormalWeightMode = WeightType.UserNWt;
            }

            _fitModel++;
            GetModelData((ModelNum)_fitModel, param, data, control);
            param.Value[4] = param.Value[3];
            LeastSquareError(param, data, control);
        }

        if (ParamsConverged(control) && (byte)control.RobustWeightMode != _fitData.RobsWt)
        {
            /* --- Robust weight mode, by steps --- */
            echoCh = '$';
            last = false;
            control.Result = 0;
            control.RobustWeightMode = (RobustType)_fitData.RobsWt;
            LeastSquareError(param, data, control);
        }

        //sprintf(STR1, "%c", EchoCh);
        //Echo(STR1);

        /* --- Last round: report fitting --- */
        if (last)
        {
            if (ParamsConverged(control))
            {
                Echo(" Success ");
            }
            else
            {
                if (ParamsMxIter(control))
                {
                    Echo(" MaxIterations reached ");
                }
                else
                {
                    Echo(" Failure ");
                }
            }
        }

        /* --- Update output record --- */
        _fitResult.DetCoef = control.DetCoef;
        _fitResult.Result = control.Result;
        _fitResult.IterNb = control.IterNb;
        _fitResult.ParamConverg = ParamsConverged(control);

        _userCallBack?.Invoke(control.IterNb, echoCh);
    }

    //Fit data from run
    private bool PUFFitRun(FitInRec fitIn, FitOutRec fitOut, FitParRec fitPar, CallBackProcType callback)
    {
        /* --- Call LstSq initialization routine --- */
        LstSqInit();

        _fitData = fitIn;
        _fitResult = fitOut;
        _fitModel = fitIn.Model;

        /* --- Initialize parameter record --- */
        fitOut.DetCoef = 0.0;
        fitOut.Result = 0;
        fitOut.IterNb = 0;
        fitOut.ParamConverg = false;
        fitOut.ParamCalc = false;

        /* --- Initialize output record --- */
        fitPar.Model = 0;
        fitPar.ParamNb = 0;
        fitPar.ParamValue = null;
        fitPar.ParamCovar = null;

        /* --- Check correctness of FitIn --- */
        if (fitIn.Model > 2
          || (fitIn.NormWt != 0 && fitIn.NormWt != 3)
          || fitIn.RobsWt > 4
          || fitIn.DNb < 5
          || (fitIn.DNb < 6 && fitIn.Model > 0)
          || fitIn.Xg == null
          || fitIn.Yg == null
           )
        {
            return false;
        }

        /* --- Take the proper true-model parameters --- */
        Echo("Model ... ");
        GetModelData((ModelNum)fitIn.Model, _param, _data, _control);

        /* --- Get number of levels --- */
        Echo("Levels ... ");
        var conc = -0.0001 /* TR 051010: Changed from "Conc = 0.0;", PUF version 1.21 */;
        var LvNb = 0;
        for (var n = 0; n < fitIn.DNb; n++)
        {
            if (!conc.EqualsOrCloseTo(fitIn.Xg[n]))
            {
                conc = fitIn.Xg[n];
                LvNb++;
            }
        }
        if (LvNb <= _param.ParamNb)
        {
            Echo("ERROR:Levels ");
            return false;
        }

        /* --- Allocate arrays for LstSq --- */
        Echo("Allocate ... ");
        if (!AllocateLstSq(_param, _data, _param.ParamNb, fitIn.DNb, 1))
        {
            Echo("ERROR:Allocate ");
            return false;
        }

        /* --- Fill Data --- */
        Echo("Data ... ");
        for (var n = 0; n < fitIn.DNb; n++)
        {
            _data.X[n] = fitIn.Xg[n];
            _data.Y[n] = fitIn.Yg[n];
        }

        /* --- Update parameter record --- */
        fitPar.Model = fitIn.Model;
        fitPar.ParamNb = _param.ParamNb;
        fitPar.ParamValue = _param.Value;
        fitPar.ParamCovar = _param.Covar;

        /* --- Find appropriate starting parameters --- */
        Echo("Start parameters ... ");

        /* --- Rodbard>4, by steps --- */
        if ((ModelNum)fitIn.Model == ModelNum.MdRodbard5)
        {
            _fitModel = (byte)ModelNum.MdRodbard4;
            GetModelData((ModelNum)_fitModel, _param, _data, _control);
        }

        /* --- Obtain starting parameters --- */
        if (!StartParams(_param, _data, (ModelNum)_fitModel, 1.0, false, true))
        {
            Echo("ERROR:StartParams ");
            return false;
        }

        /* --- Set weight modes --- */
        Echo("Weights ... ");

        /* --- Use robust last only when appropriate: --- */
        /* --- Flag set, robust-weighted model. --- */
        if (fitIn.RobsWt == (byte)RobustType.NoRWt)
        {
            fitIn.RobLst = false;
        }

        _control.NormalWeightMode = (WeightType)fitIn.NormWt;
        _control.RobustWeightMode = (RobustType)fitIn.RobsWt;

        if (fitIn.RobLst)
        {
            _control.RobustWeightMode = RobustType.NoRWt;
        }

        ControlWeightMode(_control, null, PredVar);


        /* --- Set maximum number of iterations --- */
        _control.MxIter = fitIn.MaxIterations;
        _mxIterTotal = SHORT_MAX;

        /* --- Perform the fitting itself --- */
        Echo("Fit ");
        _userCallBack = callback;
        CalculateParams(MeyerRoth, _param, _data, _control, ShowResults, 1);
        Echo("Ok ");

        /* --- Update output record --- */
        fitOut.DetCoef = _control.DetCoef;
        fitOut.Result = _control.Result;
        fitOut.IterNb = _control.IterNb;
        fitOut.ParamConverg = ParamsConverged(_control);
        fitOut.ParamCalc = true;

        /* --- DeAllocate LstSq arrays --- */
        Echo("DeAllocate ... ");
        DeAllocateData(_data);
        _param.Value = null;
        _param.Covar = null;
        _param.ParamNb = 0;
        _param.CovarNb = 0;
        _param.Len = 0;

        Echo("Done!");
        return fitOut.ParamCalc;

    }

    //Reverse prediction
    private double PUFReverse(FitParRec fitPar, double y)
    {
        double x;

        if (((fitPar.ParamNb == 4 && fitPar.Model == 0) ||
               (fitPar.ParamNb == 5 && fitPar.Model == 1) ||
               (fitPar.ParamNb == 5 && fitPar.Model == 2))
          && fitPar.ParamValue != null
          && y > NearlyZero
          && y < BigValue
           )
        {
            x = PredictX(_modelList[fitPar.Model].ModelFunc, fitPar.ParamValue, y);
        }
        else
        {
            x = BigValue;
        }

        return x;
    }

    //Forward prediction
    private double PUFForward(FitParRec fitPar, double x)
    {
        double y;

        if (((fitPar.ParamNb == 4 && fitPar.Model == 0) ||
               (fitPar.ParamNb == 5 && fitPar.Model == 1) ||
               (fitPar.ParamNb == 5 && fitPar.Model == 2))
          && fitPar.ParamValue != null
          && x > NearlyZero
          && x < BigValue
           )
        {
            y = _modelList[fitPar.Model].ModelFunc(fitPar.ParamValue, x);
        }
        else
        {
            y = BigValue;
        }

        return y;
    }

    private static double MathExp(double x)
    {
        return Math.Exp(x);
    }

    private static double Fabs(double v)
    {
        return Math.Abs(v);
    }

    private static double Log(double v)
    {
        return Math.Log(v);
    }

    private static double Sqrt(double s)
    {
        return Math.Sqrt(s);
    }

    // ReSharper disable once UnusedParameter.Local
    private static void Echo(string s)
    {
        //           print(s);
    }

    private static double Cos(double t)
    {
        return Math.Cos(t);
    }

    private static double Min(double a, double b)
    {
        return Math.Min(a, b);
    }

    private static double Max(double a, double b)
    {
        return Math.Max(a, b);
    }

    private static int Max(int a, int b)
    {
        return Math.Max(a, b);
    }

    private const int AnyFatalError =
                    (1 << (int)ResultType.NoEnoughData) |
                    (1 << (int)ResultType.NoEnoughRepl) |
                    (1 << (int)ResultType.ParamDiverging) |
                    (1 << (int)ResultType.ErrorDiverging) |
                    (1 << (int)ResultType.AllwaysStops);

    private double ConstVar(double notUsed)
    {
        return _constantVar;
    }

    private static double SampleMean(double[] data, int dataNb)
    {
        var average = 0.0;
        if (dataNb == 0)
        {
            return average;
        }

        for (var n = 0; n < dataNb; n++)
        {
            average += data[n];
        }

        average /= dataNb;
        return average;
    }

    private static double SampleVar(double[] data, int dataNb, double av)
    {
        int n;
        double Vr;

        Vr = 0.0;
        if (dataNb <= 1)
        {
            return Vr;
        }

        for (n = 0; n < dataNb; n++)
        {
            var Df = data[n] - av;
            Vr += Df * Df;
        }
        Vr /= dataNb - 1;
        return Vr;
    }

    private static void DeAllocateParam(ParamRec param)
    {
        if (param.Len == 0)
        {
            return;
        }

        param.Value = null;
        param.Covar = null;
        param.ParamNb = 0;
        param.CovarNb = 0;
        param.Len = 0;
    }

    private static bool AllocateParam(ParamRec param, int paramNb)
    {
        DeAllocateParam(param);
        /* --- Allocate arrays --- */
        param.ParamNb = paramNb;
        param.CovarNb = paramNb * paramNb;
        param.Value = new double[param.ParamNb];
        param.Covar = new double[param.CovarNb];
        param.Len = param.ParamNb;
        return true;
    }

    private static void DeAllocateData(DataRec data)
    {
        if (data.Len == 0)
        {
            return;
        }

        data.X = null;
        data.Y = null;
        data.NormalWeight = null;
        data.RobustWeight = null;
        data.DataNb = 0;
        data.VarNb = 0;
        data.Len = 0;
    }

    private static bool AllocateData(DataRec data, int dataNb, int varNb)
    {
        DeAllocateData(data);
        /* --- Allocate data arrays --- */
        data.DataNb = dataNb;
        data.VarNb = varNb;
        data.X = new double[data.DataNb * data.VarNb];
        data.Y = new double[data.DataNb];
        data.NormalWeight = new double[data.DataNb];
        data.RobustWeight = new double[data.DataNb];
        data.Len = data.DataNb;
        return true;
    }

    private static bool AllocateLstSq(ParamRec param, DataRec data, int paramNb, int dataNb, int varNb)
    {
        var Ok = AllocateData(data, dataNb, varNb);
        if (Ok)
        {
            Ok = AllocateParam(param, paramNb);
        }
        else
        {
            DeAllocateData(data);
        }

        return Ok;
    }

    //Normal weight preparation . Use a broader range of options for NormalWeightMode
    private static void ControlWeightMode(ControlRec control, LstSqProcType prepWeight, PredVarFuncType predVar)
    {
        /* --- Normal weight type --- */
        switch ((byte)control.NormalWeightMode)
        {

            case 0:  /* NoNWt */
                /* Nothing to set */
                break;

            case 1:  /* CalcNWt */
                control.PrepWeightProc = prepWeight;
                break;

            case 2:  /* UserNWt */
                /* Vector has been copied */
                break;

            case 3:  /* PredNWt */
                control.PredVarFunc = predVar;
                break;

            default:
                /* --- Undefined in this version --- */
                //exit(0);
                throw new Exception("Undefined in this version ");
        }
    }

    private void PrepareNormalWeight(DataRec data, ControlRec control)
    {
        int n;


        if (control.TotalDataNb == 0)
        {
            control.TotalDataNb = data.DataNb;
        }

        switch (control.NormalWeightMode)
        {

            case WeightType.NoNWt:
                for (n = 0; n < data.DataNb; n++)
                {
                    data.NormalWeight[n] = 1.0;
                }

                break;

            case WeightType.CalcNWt:   /* --- Calculate externally at each iteration --- */
            case WeightType.UserNWt:   /* --- Given by the user! --- */
                break;

            case WeightType.PredNWt:
                /* --- Predicted using PredVarFunc --- */
                /* --- Internal globals useful for Constant Variance --- */
                _constantMean = SampleMean(data.Y, data.DataNb);
                _constantVar = SampleVar(data.Y, data.DataNb, _constantMean);
                for (n = 0; n < data.DataNb; n++)
                {
                    data.NormalWeight[n] = 1.0 / control.PredVarFunc(data.Y[n]);
                }

                break;
        }/*switch NormalWeightMode*/

        for (n = 0; n < data.DataNb; n++)
        {
            data.RobustWeight[n] = 1.0;
        }
    }

    private double RobustTukey(double z, double a)
    {
        double RobWt, t;

        t = z / a;
        if (Fabs(t) < 1.0)
        {
            RobWt = 1.0 - (t * t);
        }
        else
        {
            RobWt = 0.0;
        }

        return RobWt;
    }

    private double RobustTukBi(double z, double a)
    {
        double RobWt, t;

        t = z / a;
        if (Fabs(t) < 1.0)
        {
            RobWt = 1.0 - (t * t);
        }
        else
        {
            RobWt = 0.0;
        }

        return RobWt * RobWt;
    }

    private double RobustHuber(double z, double a)
    {
        double RobWt, t;

        t = z / a;
        if (Fabs(t) < 1.0)
        {
            RobWt = 1.0;
        }
        else
        {
            RobWt = 1.0 / t;
        }

        return RobWt;
    }

    private double RobustTiede(double z, double a)
    {
        var t = z / a;

        if (Fabs(t) < _piHalf)
        {
            return Cos(t);
        }

        return 0.0;
    }

    private static void CalcErrY(ParamRec param, DataRec data, ControlRec control)
    {
        var NrmWt = 1.0;
        var RobWt = 1.0;

        /* --- Calculate weighted AvgY --- */
        var AvgY = 0.0;
        var WtNb = 0.0;

        for (var n = 0; n < data.DataNb; n++)
        {
            if (control.RobustWeightMode != RobustType.NoRWt)
            {
                RobWt = data.RobustWeight[n];
            }

            if (control.NormalWeightMode != WeightType.NoNWt)
            {
                NrmWt = data.NormalWeight[n];
            }

            var TotWt = NrmWt * RobWt;
            AvgY += data.Y[n] * TotWt;
            WtNb += TotWt;
        }

        AvgY /= WtNb;

        /* --- Calculate SqErrY and StdDevY --- */
        control.SqErrY = 0.0;
        for (var n = 0; n < data.DataNb; n++)
        {
            if (control.RobustWeightMode != RobustType.NoRWt)
            {
                RobWt = data.RobustWeight[n];
            }

            if (control.NormalWeightMode != WeightType.NoNWt)
            {
                NrmWt = data.NormalWeight[n];
            }

            var TotWt = NrmWt * RobWt;
            var Diff = data.Y[n] - AvgY;
            Diff *= Diff * TotWt;
            control.SqErrY += Diff;
        }

        control.StdDevY = Sqrt(control.SqErrY / (data.DataNb - param.ParamNb));
    }

    private void CalcErr(ParamRec param, DataRec data, ControlRec control, bool prepareWeight)
    {
        if (control.ParamsOKFunc != null && !control.ParamsOKFunc(param))
        {
            control.TotErr = BigValue;
            control.SqErr = control.TotErr;
            return;
        }

        var WithWeight = control.NormalWeightMode != WeightType.NoNWt || control.RobustWeightMode != RobustType.NoRWt;
        var RobWt = 1.0;
        var NrmWt = 1.0;
        control.TotErr = 0.0;
        control.SqErr = 0.0;
        control.UnWtTotErr = 0.0;
        control.UnWtSqErr = 0.0;

        /* --- Calculates Diff --- */
        var Diff = new double[data.DataNb];
        for (var n = 0; n < data.DataNb; n++)
        {
            Diff[n] = data.Y[n] - control.UserFunc(param.Value, data.X[data.VarNb * n]);

            //if (Control.IterNb == 27)
            //{
            //    print("Iter 27 Diff[n] " +  Diff[n]);
            //}
            //if (Control.IterNb == 28)
            //{
            //    print("Iter 28 Diff[n] " + Diff[n]);
            //}
        }

        /* --- Calculates TotErr,UnWtTotErr,DiffDiff --- */
        for (var n = 0; n < data.DataNb; n++)
        {
            var AbsDiff = Fabs(Diff[n]);
            Diff[n] = AbsDiff * AbsDiff;
            control.UnWtTotErr += AbsDiff;
            if (control.NormalWeightMode != WeightType.NoNWt)
            {
                /* --- AbsDiff now includes weight --- */
                AbsDiff *= Sqrt(data.NormalWeight[n]);
                control.TotErr += AbsDiff;
            }
        }  /*for n*/

        if (control.NormalWeightMode == WeightType.NoNWt)
        {
            control.TotErr = control.UnWtTotErr;
        }

        /* --- Calculates RobustWeight --- */
        if (prepareWeight)
        {
            control.PrepWeightProc?.Invoke(param, data, control);

            if (control.RobustWeightMode == RobustType.GivenRWt && control.RobustFunc == null)
            {
                control.RobustWeightMode = RobustType.NoRWt;
            }

            if (control.RobustWeightMode != RobustType.NoRWt && control.TotErr > 0.0)
            {
                /* --- Prepare variables --- */
                switch (control.RobustWeightMode)
                {

                    case RobustType.TukeyRWt:
                        control.RobustFunc = RobustTukey;
                        break;

                    case RobustType.TukBiRWt:
                        control.RobustFunc = RobustTukBi;
                        break;

                    case RobustType.HuberRWt:
                        control.RobustFunc = RobustHuber;
                        break;

                    case RobustType.TiedeRWt:
                        control.RobustFunc = RobustTiede;
                        break;

                    case RobustType.GivenRWt:   /* --- RobustFunc given by user --- */
                        break;
                }
                if (control.RobustConst == 0.0)
                {
                    control.RobustConst = control.RobustWeightMode switch
                    {
                        RobustType.TukeyRWt => 3.0,
                        RobustType.TukBiRWt => 2.1,
                        RobustType.HuberRWt => 1.5,
                        RobustType.TiedeRWt => 2.1,
                        RobustType.GivenRWt => 2.0,
                        _ => control.RobustConst
                    };
                }
                /* --- Very crude SD estimate --- */
                var SDestim = 2.0 * control.TotErr / data.DataNb;
                /* --- Calculate robust weights --- */
                for (var n = 0; n < data.DataNb; n++)
                {
                    var DiffDiff = Diff[n];
                    double WtDiffDiff;
                    if (control.NormalWeightMode == WeightType.NoNWt)
                    {
                        WtDiffDiff = DiffDiff;
                    }
                    else
                    {
                        WtDiffDiff = DiffDiff * data.NormalWeight[n];
                    }

                    data.RobustWeight[n] = control.RobustFunc(Sqrt(WtDiffDiff) / SDestim, control.RobustConst);
                }  /*for n*/
            }  /*if RobustWeightMode*/

            /* --- Calculate weighted SqErrY after weights are updated --- */
            CalcErrY(param, data, control);

        }  /*if PrepareWeight*/


        /* --- Calculates SqErr,UnWtSqErr --- */
        for (var n = 0; n < data.DataNb; n++)
        {
            var DiffDiff = Diff[n];
            control.UnWtSqErr += DiffDiff;
            if (WithWeight)
            {
                if (control.RobustWeightMode != RobustType.NoRWt)
                {
                    RobWt = data.RobustWeight[n];
                }

                if (control.NormalWeightMode != WeightType.NoNWt)
                {
                    NrmWt = data.NormalWeight[n];
                }

                var TotWt = NrmWt * RobWt;
                /* --- DiffDiff now includes weights --- */
                DiffDiff *= TotWt;
                control.SqErr += DiffDiff;
            }
        }  /*for n*/

        if (!WithWeight)
        {
            control.SqErr = control.UnWtSqErr;
        }

        control.UnWtStdDev = Sqrt(control.UnWtSqErr / (data.DataNb - param.ParamNb));
        control.StdDev = Sqrt(control.SqErr / (data.DataNb - param.ParamNb));
    }

    private static void CalcStat(ParamRec param, DataRec data, ControlRec control)
    {
        if (control.SqErr < control.SqErrY)
        {
            control.DetCoef = 1.0 - (control.SqErr / control.SqErrY);
            control.DetCoefC = 1.0 + (control.SqErr / control.SqErrY
                                    * (1 - data.DataNb) / (data.DataNb - param.ParamNb));
            if (control.SqErr == 0.0 || param.ParamNb == 1)
            {
                control.Ftest = 100.0;
            }
            else
            {
                control.Ftest = ((control.SqErrY / control.SqErr) - 1.0) *
                                (data.DataNb - param.ParamNb) / (param.ParamNb - 1);
            }
        }
        else
        {
            control.DetCoef = 0.0;
            control.DetCoefC = 0.0;
            control.Ftest = 0.0;
        }
    }

    //Called once before starting: it will prepare normal weight and calculate starting error
    private void LeastSquareError(ParamRec param, DataRec data, ControlRec control)
    {
        PrepareNormalWeight(data, control);
        CalcErr(param, data, control, true);
        CalcStat(param, data, control);
        /* --- Initialize Param.Covar --- */
        FloatArrFill(param.Covar, param.CovarNb, 0.0);
    }

    private bool OKtoStart(ParamRec param, DataRec data, ControlRec control)
    {
        control.DerivateFunc ??= DerivFunc;

        if (control.IterNb != 0)
        {
            return true;
        }

        if (data.DataNb <= param.ParamNb ||
            data.DataNb * data.VarNb > MaxFloatArr ||
            param.ParamNb < 1 ||
            param.ParamNb > _mxParam || data.X == null ||
            param.Value == null)
        {
            control.Result |= (1 << (int)ResultType.NoEnoughData) | (1 << (int)ResultType.AllwaysStops);
            return false;
        }
        control.Result = 0;
        if (control.Cycle == 0)
        {
            control.Cycle = _mxIterTotal;
        }

        if (control.MxIter == 0)
        {
            control.MxIter = _mxIterTotal;
        }

        if (control.SqErr != 0.0)
        {
            return true;
        }

        LeastSquareError(param, data, control);
        if (data.DataNb <= param.ParamNb)
        {
            control.Result |= (1 << (int)ResultType.NoEnoughRepl) | (1 << (int)ResultType.AllwaysStops);
            return false;
        }
        return true;
    }

    private static bool OKtoCycle(ControlRec control)
    {
        if (((1 << (int)ResultType.AllwaysStops) & control.Result) != 0)
        {
            return false;
        }

        /* --- Restore Result to start-of-cycle values --- */
        control.Result &= ~((1 << (int)ResultType.GridSearchSuccess) |
                              (1 << (int)ResultType.MatrixSingularFound) |
                              (1 << (int)ResultType.VectorNotDownHill) |
                              (1 << (int)ResultType.VectorTooLong) |
                              (1 << (int)ResultType.LineSearchDone));
        /* --- Update and check IterNb --- */
        control.IterNb++;
        if (control.IterNb >= control.MxIter)
        {
            /* --- MxIterReached bit set in Result --- */
            /* ---       when IterNb >= MaxIterations = 0 --- */
            control.Result |= 1 << (int)ResultType.MxIterReached;
        }
        else
        {
            control.Result &= ~(1 << (int)ResultType.MxIterReached);
        }

        return true;
    }

    private static bool LeaveRoutine(ControlRec control)
    {
        return control.IterNb % control.Cycle == 0 ||                      /*-Cycle-*/
               ((1 << (int)ResultType.ConvergAttained) & control.Result) != 0 ||   /*-Converged-*/
               ((1 << (int)ResultType.MxIterReached) & control.Result) != 0 ||     /*-IterNb=MaxIterations-*/
               (control.Result & AnyFatalError) != 0;                      /*-Problem-*/
    }

    private double DerivFunc(UserFuncType userFunc, ParamRec param, double x, double[] deriv)
    {
        var Func = userFunc(param.Value, x);

        for (var n = 0; n < param.ParamNb; n++)
        {
            var oldP = param.Value[n];
            var deltaP = Max(_dflDeltaP, Min(Fabs(Func), Max(_dflDeltaP, _dflDeltaF * oldP)));
            param.Value[n] = oldP + deltaP;
            var funcPlus = userFunc(param.Value, x);
            param.Value[n] = oldP;
            deriv[n] = (funcPlus - Func) / deltaP;
        }
        return Func;
    }

    private void CalcMatrixFF(ParamRec param, DataRec data, ControlRec control, double[] vectorFr)
    {
        double weight = 0;

        var withWeight = control.NormalWeightMode != WeightType.NoNWt || control.RobustWeightMode != RobustType.NoRWt;

        /* --- Calculate matrix (F'F) and vector (F'r), --- */
        /* --- where F = ëf(x)/ëp' = ( ëf(x[i])/ëp[j] ) --- */
        /* --- and   r = y-f(x)    = ( y[i] - f(x[i]) ) --- */
        /* --- with  i=0..DataNb-1  and  j=0..ParamNb-1 --- */
        /* --- With weighted regression F'F is (F'W F) and F'r is (F'W r) --- */
        /* --- where  W = diag( w[i] ) are the weights (w[i]=nw[i]*rw[i]) --- */
        /* --- rw=Psi(z)/z; Psi(z)=Rho'(z); z=r/å; estim å÷ä|r|; r=y-f(x) --- */
        /* --- Symmetric Matrix: lower diagonal --- */
        /* --- FFij = 0 , Fri = 0 --- */
        var matrixFf = param.Covar;
        var JacobLine = new double[param.ParamNb];
        FloatArrFill(vectorFr, param.ParamNb, 0.0);
        FloatArrFill(matrixFf, param.CovarNb, 0.0);

        /* --- Reset DflDeltaP, just in case --- */
        _dflDeltaP = control.DefaultDeltaP;
        _dflDeltaF = Sqrt(_dflDeltaP);

        /* --- Calculate F'F (lower diagonal) and F'r --- */
        for (var k = 0; k < data.DataNb; k++)
        {
            /* --- Calculate Normal Func and derivatives (Jacobian line) --- */
            var Func = control.DerivateFunc(control.UserFunc, param, data.X[data.VarNb * k], JacobLine);
            var diff = data.Y[k] - Func;

            /* --- Prepare and use weight if appropriate --- */
            if (withWeight)
            {
                weight = 1.0;
                if (control.NormalWeightMode != WeightType.NoNWt)
                {
                    weight *= data.NormalWeight[k];
                }

                if (control.RobustWeightMode != RobustType.NoRWt)
                {
                    weight *= data.RobustWeight[k];
                }

                weight = Sqrt(weight);
                diff *= weight;
            }

            for (var i = 0; i < param.ParamNb; i++)
            {
                /* --- Use weight if appropriate --- */
                if (withWeight)
                {
                    JacobLine[i] *= weight;
                }

                /* --- FFij = ä GiGj  and  Fri = ä GiRi --- */
                for (var j = 0; j <= i; j++)
                {
                    matrixFf[(param.ParamNb * i) + j] += JacobLine[i] * JacobLine[j];
                }

                vectorFr[i] += JacobLine[i] * diff;
            }

        }

        /* --- Symmetric Matrix: upper diagonal --- */
        if (param.ParamNb > 1)
        {
            for (var i = 0; i <= param.ParamNb - 2; i++)
            {
                for (var j = i + 1; j < param.ParamNb; j++)
                {
                    matrixFf[(param.ParamNb * i) + j] = matrixFf[(param.ParamNb * j) + i];
                }
            }
        }
    }

    private static void ModifyMatrixFF(double[] matrixFf, int paramNb, double lambda)
    {
        int n;
        double DiagChange;

        if (lambda == 0.0)
        {
            return;
        }  /*if Lambda<>0.0*/
        /* --- Let Q = Q+Lambda*Diag(Q) to make Q less ill conditioned --- */
        DiagChange = 1.0 + Fabs(lambda);
        /* --- Lambda<0 -. restores unmodified values --- */
        if (lambda < 0.0)
        {
            DiagChange = 1.0 / DiagChange;
        }

        for (n = 0; n < paramNb; n++)
        {
            matrixFf[(paramNb * n) + n] *= DiagChange;
        }
    }

    private static bool VectorPPok(double[] vectorPP, double[] vectorFr, ParamRec param, ControlRec control, bool matrixSingular, ref double gp)
    {
        bool TooLongVecP, PDownHill;

        /* --- Check matrix singularity --- */
        if (matrixSingular)
        {
            control.Result |= 1 << (int)ResultType.MatrixSingularFound;
            return false;
        }

        /* --- Check that the vector points downhill --- */
        gp = -ProdVectors(vectorFr, vectorPP, param.ParamNb);
        PDownHill = gp < 0.0;
        if (!PDownHill)
        {
            control.Result |= 1 << (int)ResultType.VectorNotDownHill;
            return false;
        }

        /* --- Test that step relative length is not too long --- */
        TooLongVecP = false;
        if (control.Gamma > 0.0)
        {  /*Gamma=0.0 -. inactive test*/
            var Factor = 1.0;
            int n;
            for (n = 0; n < param.ParamNb; n++)
            {
                var NewFactor = Fabs(param.Value[n] / vectorPP[n] * control.Gamma);
                /* --- The test is not performed for parameter values = 0 --- */
                if (NewFactor > 0.0 && NewFactor < Factor)
                {
                    Factor = NewFactor;
                }
            }
            TooLongVecP = Factor < 1.0;
            /* --- if step too long, reduce it (if not exaggerated) --- */
            if (TooLongVecP)
            {
                n = 0;
                do
                {
                    vectorPP[n] *= Factor;
                    /* --- Scaling down was too big: reject, do LineSearch --- */
                    TooLongVecP = Fabs(vectorPP[n]) < 0.0001;
                    n++;
                } while (!(TooLongVecP || n == param.ParamNb));
            }  /*if TooLongVecP*/
        }  /*if Control.Gamma*/

        if (TooLongVecP)
        {
            control.Result |= 1 << (int)ResultType.VectorTooLong;
            return false;
        }

        /* --- Everything was ok --- */
        return true;
    }

    private static void MaxAbsValVector(double[] vector, int dimen, out int mxPos, out double mxVal)
    {
        mxPos = 0;
        mxVal = vector[0];
        for (var n = 1; n < dimen; n++)
        {
            if (Fabs(vector[n]) > Fabs(mxVal))
            {
                mxPos = n;
                mxVal = vector[n];
            }
        }
        mxVal = -mxVal;   /* Passed vector was -g */
    }

    private bool BetterNewAlpha(ref double alpha, ref double oldErr, LOC_LineSearch link)
    {
        link.Param.Value[link.MxPos] = link.OldParamVal[link.MxPos] - (alpha * link.MxVal);
        CalcErr(link.Param, link.Data, link.Control, false);
        return link.Control.SqErr <= oldErr;
    }

    private void LineSearch(ParamRec param, DataRec data, ControlRec control, ref int mxPos, ref double mxVal, out double modParamDiff)
    {
        LOC_LineSearch V = new();
        double NewSqErr, FP, H;

        V.Param = param;
        V.Data = data;
        V.Control = control;
        V.MxPos = mxPos;
        V.MxVal = mxVal;
        FP = V.MxVal * V.MxVal;
        var OldSqErr = V.Control.SqErr;
        V.OldParamVal = new double[V.Param.ParamNb];
        FloatArrCopy(V.OldParamVal, V.Param.Value, V.Param.ParamNb);

        /* --- Initial H estimate for Alpha --- */
        H = Min((OldSqErr - V.Control.EstimSqErr) / FP, Sqrt(1.0 / FP));

        /* --- Bracket Alpha estimate --- */
        H /= 2.0;   /* to compensate the first doubling */
        do
        {
            H *= 2.0;
            NewSqErr = V.Control.SqErr;
        } while (BetterNewAlpha(ref H, ref NewSqErr, V));
        H /= 2.0;   /* to compensate the last (useless) doubling */
        V.Control.SqErr = NewSqErr;

        /* --- Quadratic interpolation to find a better Alpha estimate --- */
        do
        {
            var Alpha = H * H * FP / (V.Control.SqErr - OldSqErr + (2.0 * H * FP));
            if (Alpha < H * 0.25)
            {
                H *= 0.25;
            }
            else
            {
                if (Alpha > H * 0.75)
                {
                    H *= 0.75;
                }
                else
                {
                    H = Alpha;
                }
            }
        } while (!BetterNewAlpha(ref H, ref OldSqErr, V));

        modParamDiff = Fabs(V.MxVal * H);
        V.Control.Result |= 1 << (int)ResultType.LineSearchDone;
    }

    private void Bisection(ParamRec param, DataRec data, ControlRec control, double gp, double[] vectorPP, out double modParamDiff)
    {
        double Alpha, OldSqErr;
        double[] OldParamVal;

        OldSqErr = control.SqErr;
        OldParamVal = new double[param.ParamNb];
        FloatArrCopy(OldParamVal, param.Value, param.ParamNb);
        Alpha = 2.0;
        do
        {
            Alpha /= 2.0;
            int n;
            for (n = 0; n < param.ParamNb; n++)
            {
                param.Value[n] = OldParamVal[n] + (vectorPP[n] * Alpha);
            }

            CalcErr(param, data, control, false);

            //if (Control.IterNb == 27   || Control.IterNb == 26)
            //{
            //    print($"Iter {Control.IterNb} bisectControl.SqErr  " + Control.SqErr);
            //    print($"Iter {Control.IterNb} bisect {OldSqErr} + 2.0 * {Alpha} * {Control.Beta} * {GP}  ==== > " 
            //        + (OldSqErr + 2.0 * Alpha * Control.Beta * GP));
            //}

        } while (control.SqErr > OldSqErr + (2.0 * Alpha * control.Beta * gp));
        /* --- || ParamVal - OldParamVal || = Alpha * || VectorPP || --- */
        modParamDiff = Alpha * ModVector(vectorPP, param.ParamNb);
    }

    private static bool ErrParamConverged(ref double modParamDiff, ref double oldSqErr, ControlRec control)
    {
        bool ConvAtSqErr, ConvAtParam;

        ConvAtSqErr = Fabs(control.SqErr - oldSqErr) <= control.ConvergSqErr * oldSqErr;
        ConvAtParam = modParamDiff <= control.ConvergParam;
        if (ConvAtSqErr)
        {
            control.Result |= 1 << (int)ResultType.ConvergAtSqErr;
        }

        if (ConvAtParam)
        {
            control.Result |= 1 << (int)ResultType.ConvergAtParam;
        }

        return ConvAtSqErr || ConvAtParam;
    }

    private void GridSearch(ParamRec param, DataRec data, ControlRec control)
    {
        double[] gridFactor = [1.1, 0.9, 1.01, 0.99];  /*called Gamma by Wolf*/

        var mMin = 0;
        var nMin = -1;

        if (!control.DoGridSearch)
        {
            control.Result |= 1 << (int)ResultType.ConvergAttained;
            return;
        }

        var newControl = MakeCopy(control);
        var oldSqErr = control.SqErr * (1.0 - control.ConvergGrid);

        for (var n = 0; n < param.ParamNb; n++)
        {
            var oldParamVal = param.Value[n];

            for (var m = 0; m < _gridSize; m++)
            {
                param.Value[n] = oldParamVal * gridFactor[m];
                CalcErr(param, data, newControl, false);

                if (newControl.SqErr < oldSqErr)
                {
                    control = newControl;
                    oldSqErr = control.SqErr * (1.0 - control.ConvergGrid);
                    nMin = n;
                    mMin = m;
                }
            }

            param.Value[n] = oldParamVal;
        }
        if (nMin == -1)
        {
            control.Result |= 1 << (int)ResultType.ConvergAttained;
        }
        else
        {
            control.Result = (control.Result | (1 << (int)ResultType.GridSearchSuccess)) &
                              ~((1 << (int)ResultType.ConvergAtDeriv) | (1 << (int)ResultType.ConvergAtParam) |
                                (1 << (int)ResultType.ConvergAtSqErr));
            param.Value[nMin] *= gridFactor[mMin];
        }
    }

    private static ControlRec MakeCopy(ControlRec control)
    {
        return new ControlRec
        {
            TotalDataNb = control.TotalDataNb,
            NormalWeightMode = control.NormalWeightMode,
            RobustWeightMode = control.RobustWeightMode,
            RobustConst = control.RobustConst,
            DoGridSearch = control.DoGridSearch,
            ConvergDeriv = control.ConvergDeriv,
            ConvergParam = control.ConvergParam,
            ConvergSqErr = control.ConvergSqErr,
            ConvergGrid = control.ConvergGrid,
            DefaultDeltaP = control.DefaultDeltaP,
            Beta = control.Beta,
            Gamma = control.Gamma,
            Lambda = control.Lambda,
            EstimSqErr = control.EstimSqErr,
            UnWtTotErr = control.UnWtTotErr,
            UnWtSqErr = control.UnWtSqErr,
            UnWtStdDev = control.UnWtStdDev,
            TotErr = control.TotErr,
            SqErr = control.SqErr,
            StdDev = control.StdDev,
            SqErrY = control.SqErrY,
            StdDevY = control.StdDevY,
            DetCoef = control.DetCoef,
            DetCoefC = control.DetCoefC,
            Ftest = control.Ftest,
            Cycle = control.Cycle,
            MxIter = control.MxIter,
            IterNb = control.IterNb,
            Result = control.Result,
            UserFunc = control.UserFunc,
            DerivateFunc = control.DerivateFunc,
            ParamsOKFunc = control.ParamsOKFunc,
            RobustFunc = control.RobustFunc,
            PrepWeightProc = control.PrepWeightProc,
            PredVarFunc = control.PredVarFunc
        };
    }

    private void CalcStdDev(ParamRec param, ControlRec control)
    {
        LLInvertMatrix(param.Covar, param.ParamNb, out var matrixSingular);
        if (matrixSingular)
        {
            /* --- Covar not inverted --- */
            FloatArrFill(param.Covar, param.CovarNb, 0.0);
            control.Result |= 1 << (int)ResultType.MatrixSingularFound;
            return;
        }

        /* --- Convert into covariance matrix --- */
        var variance = control.StdDev * control.StdDev;

        for (var n = 0; n < param.CovarNb; n++)
        {
            param.Covar[n] *= variance;
        }
    }

    private static bool ParamsConverged(ControlRec control)
    {
        return ((1 << (int)ResultType.ConvergAttained) & control.Result) != 0;
    }

    private static bool ParamsMxIter(ControlRec control)
    {
        return ((1 << (int)ResultType.MxIterReached) & control.Result) != 0;
    }

    private static bool ParamsFailed(ControlRec control)
    {
        return (control.Result & AnyFatalError) != 0;
    }

    private void MeyerRoth(ParamRec param, DataRec data, ControlRec control)
    {
        double GP = 0;
        double ModParamDiff = 0;
        double[] VectorPP;   /* ë = p(a+1) - p(a): parameter update vector */
        double[] VectorFr;   /* F'r */
        double[] MatrixFF;   /* F'F */
        double[] OrigMatrixFF;


        if (!OKtoStart(param, data, control))
        {
            return;
        }

        VectorFr = new double[param.ParamNb];
        VectorPP = new double[param.ParamNb];
        MatrixFF = param.Covar;
        OrigMatrixFF = null;

        do
        {

            if (!OKtoCycle(control))
            {
                return;
            }

            /* --- Robust and Individual Weights change from cycle to cycle --- */
            /* --- So they must be recalculated, as well as error values.   --- */
            if (control.RobustWeightMode != RobustType.NoRWt || control.NormalWeightMode == WeightType.CalcNWt)
            {
                CalcErr(param, data, control, true);
            }

            var derivConverged = false;
            var OldSqErr = control.SqErr;

            CalcMatrixFF(param, data, control, VectorFr);

            if (control.Lambda != 0.0)
            {
                /* --- If appropriated, save original matrix, and modify it --- */
                OrigMatrixFF ??= new double[param.CovarNb];

                FloatArrCopy(OrigMatrixFF, MatrixFF, param.CovarNb);
                ModifyMatrixFF(MatrixFF, param.ParamNb, control.Lambda);
            }

            LLClean();
            LLSolveMatrix(VectorPP, MatrixFF, VectorFr, param.ParamNb, out var matrixSingular);

            if (VectorPPok(VectorPP, VectorFr, param, control, matrixSingular, ref GP))
            {
                /* --- If VectorPP OK perform a bisection --- */
                Bisection(param, data, control, GP, VectorPP, out ModParamDiff);
                control.Lambda *= 0.25;
            }
            else
            {
                /* --- If VectorPP not OK perform a line search --- */
                MaxAbsValVector(VectorFr, param.ParamNb, out var MxPos, out var MxVal);
                derivConverged = Fabs(MxVal) <= control.ConvergDeriv;
                if (derivConverged)
                {
                    control.Result |= 1 << (int)ResultType.ConvergAtDeriv;
                }
                else
                {
                    LineSearch(param, data, control, ref MxPos, ref MxVal, out ModParamDiff);
                    if (control.Lambda < 1.0e10)
                    {
                        control.Lambda *= 4.0;
                    }
                }
            }

            if (derivConverged || ErrParamConverged(ref ModParamDiff, ref OldSqErr, control))
            {
                /* --- Last convergence test; prevents from finding saddle points --- */
                GridSearch(param, data, control);
            }

            /* --- if convergence not yet attained, perform a new iteration --- */
        } while (!LeaveRoutine(control));

        /* --- Update DetCoef+Ftest --- */
        CalcStat(param, data, control);

        if (ParamsConverged(control) || ParamsMxIter(control))
        {
            if (control.Lambda != 0.0)
            {
                /* --- If original matrix was modified, restore it --- */
                FloatArrCopy(MatrixFF, OrigMatrixFF, param.CovarNb);
                LLClean();
            }
            CalcStdDev(param, control);
        }

        LLClean();
    }

    private void CalculateParams(LstSqProcType lstSqProc, ParamRec param, DataRec data, ControlRec control, LstSqProcType showResults, int showCycle)
    {
        control.Cycle = showCycle;
        control.SqErr = 0.0;
        control.IterNb = 0;
        control.Result = 0;
        LeastSquareError(param, data, control);

        showResults?.Invoke(param, data, control);

        do
        {
            lstSqProc(param, data, control);
            showResults?.Invoke(param, data, control);
        } while (!(ParamsConverged(control) ||
                   ParamsMxIter(control) ||
                   ParamsFailed(control)));

    }

    // Given mainly to help finding initial estimates with some models
    private static void RegrLinear(double[] dataX, double[] dataY, int dataNb, /*out double corrCoef,*/ out double slopeX, out double offsetX/*, out double slopeY, out double offsetY*/)
    {
        /* --- Parameters are:  y=SlopeX*x+OffsetX  and  x:=SlopeY*y+OffsetY  --- */
        double VarX, /*VarY, */CovXY, SumX, SumY, SumXX, /*SumYY,*/ SumXY;

        SumX = 0.0;
        SumY = 0.0;
        SumXX = 0.0;
        //SumYY = 0.0;
        SumXY = 0.0;

        for (var n = 0; n < dataNb; n++)
        {
            var X = dataX[n];
            var Y = dataY[n];
            SumX += X;
            SumY += Y;
            SumXX += X * X;
            //SumYY += Y * Y;
            SumXY += X * Y;
        }

        SumX /= dataNb;
        SumY /= dataNb;
        SumXX /= dataNb;
        //SumYY /= dataNb;
        SumXY /= dataNb;
        VarX = SumXX - (SumX * SumX);
        //VarY = SumYY - (SumY * SumY);
        CovXY = SumXY - (SumX * SumY);

        // out parameters
        //corrCoef = CovXY / Sqrt(VarX * VarY);
        slopeX = CovXY / VarX;
        //slopeY = CovXY / VarY;
        offsetX = ((SumXX * SumY) - (SumXY * SumX)) / VarX;
        //offsetY = ((SumYY * SumX) - (SumXY * SumY)) / VarY;
    }

    private static void Bisect(UserFuncType userFunc, double[] paramValue, ref double xa, ref double xb, double yp, int iter)
    {
        double PrErr;

        PrErr = yp - userFunc(paramValue, xa);
        while (iter > 0)
        {
            iter--;
            var Xp = (xa + xb) / 2.0;
            var NwErr = yp - userFunc(paramValue, Xp);
            if (PrErr * NwErr < 0.0)
            {
                xb = Xp;
            }
            else
            {
                xa = Xp;
                PrErr = NwErr;
            }
        }
    }

    private double Solve(UserFuncType userFunc, double[] paramValue, double xa, double xb, double yp)
    {
        int Iter;
        bool Stop, Found;
        double Xp = 0;
        double Func;

        if (Fabs(xa - xb) < Fabs(xa * _control.ConvergSqErr))
        {
            return (xa + xb) / 2.0;
        }

        Iter = 0;
        Func = userFunc(paramValue, xa);
        /* --- Calc new iterate --- */
        do
        {
            Iter++;
            var OldFunc = Func;
            Func = userFunc(paramValue, xb);
            if (!Func.EqualsOrCloseTo(OldFunc))
            {
                Xp = xb - ((Func - yp) * (xb - xa) / (Func - OldFunc));
                xa = xb;
                xb = Xp;
            }
            /* --- Check convergence --- */
            Found = Fabs(Func - yp) < Fabs(yp * _control.ConvergSqErr);
            Stop = !Found && (Iter == _mxIterDefault ||
                              Fabs(Func - OldFunc) < Fabs(Func * _control.ConvergSqErr));
        } while (!(Found || Stop));
        if (!Found)
        {
            Xp = -BigValue;
        }

        return Xp;
    }

    private double PredictX(UserFuncType userFunc, double[] paramValue, double yp)
    {
        double Xa, Xb, Xp, PrErr, NwErr, Step, LfY, RtY;

        /* --- Given the fitted parameters value and model --- */
        /* --- calculate for a given response value "y"    --- */
        /* --- the predicted "x" such that f(x;p)=y.       --- */
        /* --- This routine assumes x>0 and f monotonous.  --- */

        /* --- First check asymptotes --- */
        Xp = NearlyZero;
        LfY = userFunc(paramValue, Xp);
        Xp = BigValue;
        RtY = userFunc(paramValue, Xp);
        if (LfY < RtY)
        {
            if (yp <= LfY)
            {
                Xp = NearlyZero;
            }
            else
            {
                if (yp >= RtY)
                {
                    Xp = BigValue;
                }
                else
                {
                    Xp = 0.0;
                }
            }
        }
        else
        {
            /*if LfY>RtY then*/
            if (yp >= LfY)
            {
                Xp = NearlyZero;
            }
            else
            {
                if (yp <= RtY)
                {
                    Xp = BigValue;
                }
                else
                {
                    Xp = 0.0;
                }
            }
        }
        if (Xp > 0.0)
        {
            return Xp;
        }

        /* --- First look for the closest standard as the initial guess --- */
        Step = 10.0;
        Xp = 1.0;
        PrErr = yp - userFunc(paramValue, Xp);
        Xp *= Step;
        NwErr = yp - userFunc(paramValue, Xp);
        /* ---
           Step>1 if:
             Curve increasing ( (PrErr-NwErr)>0 ) and estimate left ( PrErr>0 ),
           or
             Curve decreasing ( (PrErr-NwErr)<0 ) and estimate left ( PrErr<0 ).
         --- */
        if ((PrErr - NwErr) * PrErr < 0.0)
        {
            Step = 1.0 / Step;
        }

        while (PrErr * NwErr > 0.0)
        {
            Xp *= Step;

            if (Xp <= NearlyZero)
            {
                return NearlyZero;
            }

            if (!double.IsFinite(Xp) || Xp >= BigValue)
            {
                return BigValue;
            }

            PrErr = NwErr;
            NwErr = yp - userFunc(paramValue, Xp);
        }

        /* --- Xp is beyond NearlyZero and BigValue --- */
        if (Xp < NearlyZero)
        {
            return NearlyZero;
        }

        if (Xp > BigValue)
        {
            return BigValue;
        }

        /* --- Xp is somewhere between the last two estimates --- */
        if (Step > 1.0)
        {
            Xa = Xp / Step;
            Xb = Xp;
        }
        else
        {
            Xa = Xp;
            Xb = Xp / Step;
        }

        /* --- Narrow interval again if previous estimate fails --- */
        for (var retry = 0; retry < _mxIterDefault; retry++)
        {
            /* --- Perform a few bisections to narrow the interval --- */
            Bisect(userFunc, paramValue, ref Xa, ref Xb, yp, 4);
            /* --- Solve f(x;p)=y using the previous initial guess --- */
            Xp = Solve(userFunc, paramValue, Xa, Xb, yp);

            if (!Xp.EqualsOrCloseTo(-BigValue))
            {
                return Xp;
            }
        }

        return BigValue;

    }

    private void LstSqInit()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;

        /* --- Default value; can be modified --- */
        /* --- Type-casting to avoid warnings in C --- */
        //MxData = (int)(MaxFloatArr / MxVar);
        /* --- Define depending on the precision of the Float type --- */
        _control.DefaultDeltaP = MathExp(-Log(10.0) * sizeof(double));
        _control.DerivateFunc = DerivFunc;
        _control.PredVarFunc = ConstVar;
        /* --- Globals --- */
        _constantMean = 0.0;
        _constantVar = 1.0;
    }

    private static void FloatArrFill(double[] result, int dataNb, double value)
    {
        for (var n = 0; n < dataNb; n++)
        {
            result[n] = value;
        }
    }

    private static void FloatArrCopy(double[] result, double[] data, int dataNb)
    {
        if (result == data)
        {
            return;
        }

        Array.Copy(data, result, dataNb);
    }

    private static double ProdVectors(double[] vector1, double[] vector2, int dimen)
    {
        var pv = 0d;
        for (var n = 0; n < dimen; n++)
        {
            pv += vector1[n] * vector2[n];
        }

        return pv;
    }

    private static double ModVector(double[] vector, int dimen)
    {
        double EE = 0;

        for (var n = 0; n < dimen; n++)
        {
            var E = vector[n];
            EE += E * E;
        }
        return Sqrt(EE);
    }

    private void LLDecomp(double[] matrix, int dimen)
    {
        var diag = new double[dimen];

        /* --- Calculate Cholesky decomposition (L such as LL'=A) --- */
        for (var i = 0; i < dimen; i++)
        {
            for (var j = i; j < dimen; j++)
            {
                var sum = matrix[(dimen * i) + j];
                for (var k = 0; k < i; k++)
                {
                    sum -= matrix[(dimen * i) + k] * matrix[(dimen * j) + k];
                }

                if (i != j)
                {
                    matrix[(dimen * j) + i] = sum / diag[i];
                }
                else if (sum > 0.0)
                {
                    diag[i] = Sqrt(sum);
                }
                else
                {
                    /* --- Diagonal=0 -> not positive definite --- */
                    _determ = 0.0;
                    return;
                }
            }
        }
        /* --- Set diagonal elements --- */
        for (var i = 0; i < dimen; i++)
        {
            matrix[(dimen * i) + i] = diag[i];
        }

        /* --- Clear upper diagonal elements --- */
        for (var i = 0; i < dimen; i++)
        {
            for (var j = i + 1; j < dimen; j++)
            {
                matrix[(dimen * i) + j] = 0.0;
            }
        }
        /* --- Calculate determinant --- */
        _determ = 1.0;
        for (var i = 0; i < dimen; i++)
        {
            _determ *= diag[i];
        }

        _determ *= _determ;
    }

    private static void LLBackSubstitution(double[] LLMatrix, double[] vector, int dimen)
    {
        /* --- Solve Ly=b --- */
        for (var i = 0; i < dimen; i++)
        {
            var sum = vector[i];
            for (var j = 0; j < i; j++)
            {
                sum -= LLMatrix[(dimen * i) + j] * vector[j];
            }

            vector[i] = sum / LLMatrix[(dimen * i) + i];
        }
        /* --- Solve L'x=y --- */
        for (var i = dimen - 1; i >= 0; i--)
        {
            var sum = vector[i];
            for (var j = i + 1; j < dimen; j++)
            {
                sum -= LLMatrix[(dimen * j) + i] * vector[j];
            }

            vector[i] = sum / LLMatrix[(dimen * i) + i];
        }
    }

    private void LLSolveMatrix(double[] vectorResult, double[] matrixCoef, double[] vectorCoef, int dimen, out bool error)
    {
        if (dimen == 1)
        {
            error = false;
            _determ = matrixCoef[0];
            if (Fabs(_determ) < NearlyZero)
            {
                error = true;
            }
            else
            {
                vectorResult[0] = vectorCoef[0] / _determ;
            }

            return;
        }

        if (_determ == 0.0)
        {
            LLDecomp(matrixCoef, dimen);
        }

        error = _determ == 0.0;
        if (error)
        {
            return;
        }

        FloatArrCopy(vectorResult, vectorCoef, dimen);
        LLBackSubstitution(matrixCoef, vectorResult, dimen);
    }

    private void LLInvertMatrix(double[] matrix, int dimen, out bool error)
    {
        if (dimen == 1)
        {
            error = false;
            _determ = matrix[0];
            if (Fabs(_determ) < NearlyZero)
            {
                error = true;
            }
            else
            {
                matrix[0] = 1.0 / _determ;
            }

            return;
        }

        if (_determ == 0.0)
        {
            LLDecomp(matrix, dimen);
        }

        error = _determ == 0.0;
        if (error)
        {
            return;
        }

        /* --- Invert L in situ --- */
        for (var j = 0; j < dimen; j++)
        {
            matrix[(dimen * j) + j] = 1.0 / matrix[(dimen * j) + j];
            for (var i = j + 1; i < dimen; i++)
            {
                var sum = 0.0;
                for (var k = j; k < i; k++)
                {
                    sum -= matrix[(dimen * i) + k] * matrix[(dimen * k) + j];
                }

                matrix[(dimen * i) + j] = sum / matrix[(dimen * i) + i];
            }
        }

        /* --- Calculate A^-1 = (LL')^-1 = L'^-1 L^-1 in situ --- */
        /* --- Upper diagonal --- */
        for (var i = 0; i <= dimen - 2; i++)
        {
            for (var j = i + 1; j < dimen; j++)
            {
                var sum = 0.0;
                for (var k = Max(i, j); k < dimen; k++)
                {
                    sum += matrix[(dimen * k) + i] * matrix[(dimen * k) + j];
                }

                matrix[(dimen * i) + j] = sum;
            }
        }
        /* --- Middle diagonal --- */
        for (var i = 0; i < dimen; i++)
        {
            var sum = 0.0;
            for (var k = i; k < dimen; k++)
            {
                sum += matrix[(dimen * k) + i] * matrix[(dimen * k) + i];
            }

            matrix[(dimen * i) + i] = sum;
        }
        /* --- Lower diagonal --- */
        for (var i = 1; i < dimen; i++)
        {
            for (var j = 0; j < i; j++)
            {
                matrix[(dimen * i) + j] = matrix[(dimen * j) + i];
            }
        }

        LLClean();
    }

    private void LLClean()
    {
        _determ = 0.0;
    }
}