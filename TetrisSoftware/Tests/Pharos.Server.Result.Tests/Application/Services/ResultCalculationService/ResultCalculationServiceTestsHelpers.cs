using System.Reflection;
using Moq.EntityFrameworkCore;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Services.ResultCalculationService;
using Pharos.Server.Result.CurveFit;
using Pharos.Server.Result.Domain;

internal static class ResultCalculationServiceTestsHelpers
{

    public static Curve GetCurveForElG(int variant)
    {
        return variant switch
        {
            1 => new Curve()
            {
                Id = 1,
                MethodName = "El-G",
                ZeroPointCalculationNumberOfIntervals = 1,
                NumberOfCalibrationPointsInZeroPointCalculation = 4,
                ZeroPointSwitchConcentration = 4,
                ZeroPointCalculationCoefficientAlfa = 43.3535895889408,
                ZeroPointCalculationCoefficientBeta = 67.2797356828194,
                CurveMinConcentration = 0,
                CurveMaxConcentration = 600,
                CalculationParameterA = 75.6593481392058,
                CalculationParameterB = 27491.2169225128,
                CalculationParameterC = 315.807116188821,
                CalculationParameterD = 1.08562498780888,
                CalculationParameterE = 0,
                CalculationType = Pharos.Server.Domain.Enums.CalculationType.Rodbard4Par,
                CurveFittedFlag = true,
                CurveControlDisabledFlag = null,
                InstrumentNumber = "Instrument" + variant,
                GroupId = Guid.NewGuid(),
                InstrumentRunId = Guid.NewGuid()
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant)),
        };
    }

    public static Curve GetCurveForElT(int variant)
    {
        return variant switch
        {
            1 => new Curve()
            {
                Id = 10,
                MethodName = "El-T",
                ZeroPointCalculationNumberOfIntervals = 0,
                NumberOfCalibrationPointsInZeroPointCalculation = 0,
                ZeroPointSwitchConcentration = 0,
                ZeroPointCalculationCoefficientAlfa = 0,
                ZeroPointCalculationCoefficientBeta = 0,
                CurveMinConcentration = 0.01,
                CurveMaxConcentration = 40,
                CalculationParameterA = 1574.85399517171,
                CalculationParameterB = 19025.1024920831,
                CalculationParameterC = 5.37867357253424,
                CalculationParameterD = -0.982555022884768,
                CalculationParameterE = 0,
                CalculationType = Pharos.Server.Domain.Enums.CalculationType.Rodbard4Par,
                CurveFittedFlag = true,
                CurveControlDisabledFlag = null,
                InstrumentNumber = "Instrument" + variant,
                GroupId = Guid.NewGuid(),
                InstrumentRunId = Guid.NewGuid()
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant)),
        };
    }

    public static Curve GetCurveForSige(int variant)
    {
        return variant switch
        {
            1 => new Curve()
            {
                Id = 1,
                MethodName = "sIgE",
                ZeroPointCalculationNumberOfIntervals = 0,
                NumberOfCalibrationPointsInZeroPointCalculation = 0,
                ZeroPointSwitchConcentration = 0,
                ZeroPointCalculationCoefficientAlfa = 0,
                ZeroPointCalculationCoefficientBeta = 0,
                CurveMinConcentration = 0.001,
                CurveMaxConcentration = 100,
                CalculationParameterA = 15.5132438252122,
                CalculationParameterB = 25738.5158192894,
                CalculationParameterC = 46.2055293568991,
                CalculationParameterD = 0.983330116016995,
                CalculationParameterE = 1.42195006529122,
                CalculationType = Pharos.Server.Domain.Enums.CalculationType.Rodbard5Par,
                CurveFittedFlag = true,
                CurveControlDisabledFlag = null,
                InstrumentNumber = "Instrument" + variant,
                GroupId = Guid.NewGuid(),
                InstrumentRunId = Guid.NewGuid()
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant)),
        };
    }

    public static Curve GetCurveForView(int variant)
    {
        return variant switch
        {
            1 => new Curve()
            {
                Id = 100,
                MethodName = "View",
                ZeroPointCalculationNumberOfIntervals = 0,
                NumberOfCalibrationPointsInZeroPointCalculation = 0,
                ZeroPointSwitchConcentration = 0,
                ZeroPointCalculationCoefficientAlfa = 0,
                ZeroPointCalculationCoefficientBeta = 0,
                CurveMinConcentration = 0,
                CurveMaxConcentration = 0,
                CalculationParameterA = 0,
                CalculationParameterB = 0,
                CalculationParameterC = 0,
                CalculationParameterD = 0,
                CalculationParameterE = 0,
                CalculationType = Pharos.Server.Domain.Enums.CalculationType.Quotient,
                CurveFittedFlag = true,
                CurveControlDisabledFlag = null,
                InstrumentNumber = "Instrument" + variant,
                GroupId = Guid.NewGuid(),
                InstrumentRunId = Guid.NewGuid()
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant)),
        };
    }


    /// ================================================================================
    /// 
    ///  Some Helper functions
    ///  - Calculation curves for different methods and variants per method
    /// 
    /// ================================================================================
    public static IFittedCurve? GetFittedCurve(string methodName, int variant)
    {
        IFittedCurve? fittedCurve = null;
        var curveFitter = new CurveFitter(new CurveFitterApi());
        var curve = methodName switch
        {
            "El-T" => GetCurveForElT(variant),
            "sIgE" => GetCurveForSige(variant),
            "El-G" => GetCurveForElG(variant),
            "View" => GetCurveForView(variant),
            _ => throw new ArgumentOutOfRangeException(nameof(methodName)),
        };

        var type = typeof(ResultCalculationService);
        var methodInfo = type.GetMethod("GetFittedCurve", BindingFlags.NonPublic | BindingFlags.Static);
        if (methodInfo != null)
        {
            var c = (IFittedCurve?)methodInfo.Invoke(null, [curveFitter, curve]);
            if (c != null)
            {
                fittedCurve = c;
            }
        }

        return fittedCurve;
    }

    /// <summary>
    /// Prepare some curves
    /// </summary>
    /// <returns></returns>
    public static List<Curve> PrepareCurves(Mock<IResultDbContext> dbContextMock)
    {
        var curves = new List<Curve>()
        {
            GetCurveForSige(1),
            GetCurveForElT(1),
            GetCurveForView(1),
            GetCurveForElG(1),
        };

        var curveId = 1;
        foreach (var curve in curves)
        {
            curve.Id = curveId++;
        }

        dbContextMock.Setup(db => db.Curves).ReturnsDbSet(curves);

        return curves;
    }
}