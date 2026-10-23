namespace Pharos.Server.Result.CurveFit.PiePuf;

/// <summary>
/// Contains the class that can calculate a response from concentration.
/// Copied from Prime's PUF.cs
/// </summary>
internal class PUF
{
    //*********************************************************
    //  CONSTANTS
    //*********************************************************
    private const double NearlyZero = 1E-100;
    private const double BigValue = 1E+100;    // (1.0 / NearlyZero)
    private const double BigValueDecimal = 1E+26;    // (1.0 / NearlyZero)



    //********************************************************************************
    /// <summary>
    /// Calculate Response from a Concentration.
    /// </summary>
    /// <param name="concentration">Concentration to calculate for</param>
    /// <param name="curveCalculation">Structure with calculation parameters</param>
    /// <returns>Calculated response</returns>
    //********************************************************************************
    public static double CalculatePUFResponse(double concentration, CurveCalculationType curveCalculation)
    {
        var result = 0.0;

        try
        {
            if (curveCalculation.CurveFittedFlag == true)
            {
                // If this is on the straight line part for zero point calculation....
                if (concentration < curveCalculation.ZeroPointSwitchConcentration)
                {
                    result = curveCalculation.ZeroPointAlfa + (curveCalculation.ZeroPointBeta * concentration);
                    if (result < 0)
                    {
                        result = 0;
                    }
                }

                // ....else, use PUF functions
                else
                {
                    result = curveCalculation.CalculationType switch
                    {
                        EnumCalculationTypes.CalcTypeRodbard4Par => Rodbard4(concentration, curveCalculation),
                        EnumCalculationTypes.CalcTypeRodbard5Par => Rodbard5(concentration, curveCalculation),
                        _ => 0
                    };
                }
            }

        }
        catch { }

        return result;
    }



    //********************************************************************************
    /// <summary>
    /// Calculate Response from a Concentration (Rodbard 4 parameters)
    /// </summary>
    /// <param name="concentration">Concentration to calculate for</param>
    /// <param name="curveCalculation">Structure with calculation parameters</param>
    /// <returns>Calculated response</returns>
    //********************************************************************************
    private static double Rodbard4(double concentration, CurveCalculationType curveCalculation)
    {
        var result = 0.0;
        var Pw = 0.0;

        if (concentration <= NearlyZero)
        {
            result = LeftAsymptote(curveCalculation);
        }
        else if (concentration >= BigValue)
        {
            result = RightAsymptote(curveCalculation);
        }
        else
        {

            Pw = Power(concentration / curveCalculation.CalculationParameterC, curveCalculation.CalculationParameterD);
            if (Pw >= BigValueDecimal)
            {
                result = RightAsymptote(curveCalculation);
            }
            else
            {
                // CO1863
                //result = (curveCalculation.CalculationParameterB + (curveCalculation.CalculationParameterA - curveCalculation.CalculationParameterB) / (1 + Pw));
                var b = (decimal)curveCalculation.CalculationParameterB;
                var a = (decimal)curveCalculation.CalculationParameterA;
                var p = (decimal)Pw;
                var r = b + ((a - b) / (1 + p));
                result = (double)r;
            }
        }

        return result;
    }



    //********************************************************************************
    /// <summary>
    /// Calculate Response from a Concentration (Rodbard 5 parameters)
    /// </summary>
    /// <param name="concentration">Concentration to calculate for</param>
    /// <param name="curveCalculation">Structure with calculation parameters</param>
    /// <returns>Calculated response</returns>
    //********************************************************************************
    private static double Rodbard5(double concentration, CurveCalculationType curveCalculation)
    {
        double Pw1;
        double Pw2;
        var result = 0.0;

        if (concentration <= NearlyZero)
        {
            result = LeftAsymptote(curveCalculation);
        }
        else if (concentration >= BigValue)
        {
            result = RightAsymptote(curveCalculation);
        }
        else
        {
            Pw1 = Power(2 / ((curveCalculation.CalculationParameterC / concentration) + 1), curveCalculation.CalculationParameterD);
            Pw2 = Power((1 + (concentration / curveCalculation.CalculationParameterC)) / 2, curveCalculation.CalculationParameterE);
            if ((Pw1 >= BigValueDecimal) || (Pw2 >= BigValueDecimal))
            {
                result = RightAsymptote(curveCalculation);
            }
            else
            {
                // CO1863
                //result = (curveCalculation.CalculationParameterB + (curveCalculation.CalculationParameterA - curveCalculation.CalculationParameterB) / (1 + Pw1 * Pw2));
                var b = (decimal)curveCalculation.CalculationParameterB;
                var a = (decimal)curveCalculation.CalculationParameterA;
                var p1 = (decimal)Pw1;
                var p2 = (decimal)Pw2;
                var r = b + ((a - b) / (1 + (p1 * p2)));
                result = (double)r;
            }
        }

        return result;
    }



    //********************************************************************************
    /// <summary>
    /// Calculate LeftAsymptote.
    /// </summary>       
    /// <param name="curveCalculation">Structure with calculation parameters</param>
    /// <returns>The calculated value</returns>
    //********************************************************************************
    private static double LeftAsymptote(CurveCalculationType curveCalculation)
    {
        var result = 0.0;

        if (curveCalculation.CalculationParameterD > 0)
        {
            result = curveCalculation.CalculationParameterA;
        }
        else
        {
            result = curveCalculation.CalculationParameterB;
        }
        return result;

    }



    //********************************************************************************
    /// <summary>
    /// Calculate RightAsymptote.
    /// </summary>       
    /// <param name="curveCalculation">Structure with calculation parameters</param>
    /// <returns>The calculated value</returns>
    //********************************************************************************
    private static double RightAsymptote(CurveCalculationType curveCalculation)
    {
        var result = 0.0;

        if (curveCalculation.CalculationParameterD > 0)
        {
            result = curveCalculation.CalculationParameterB;
        }
        else
        {
            result = curveCalculation.CalculationParameterA;
        }

        return result;

    }



    //********************************************************************************
    /// <summary>
    /// Calculate Power of X and Y.
    /// </summary>       
    /// <param name="x">The x value</param>
    /// <param name="y">The y value</param>
    /// <returns>The calculated value</returns>
    //********************************************************************************
    private static double Power(double x, double y)
    {
        var result = BigValue;

        try
        {
            if (x == 0)
            {
                result = 0;
            }
            else if (y == 1)
            {
                result = x;
            }
            else
            {
                result = Math.Exp(y * Math.Log(x));
            }
        }
        catch { }
        return result;
    }

}
