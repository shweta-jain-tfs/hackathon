using Pharos.Server.Result.CurveFit.PiePuf;

namespace Pharos.Server.Result.CurveFit;

public interface ICurveFitterApi
{
    CurveFitResultCommon CurveFit(CurveTypeCommon curveType, CurveAdditionalDataTypeCommon additionalData, FitParTypeCommon fitPar);
    CurveCheckResultCommon CurveCheck(CurveTypeCommon curveType, int straightLineSwitch);
    double ResponseToConcentration(double response, CurveAdditionalDataTypeCommon additionalData, FitParTypeCommon fitPar, double concentrationFactor, int dilutionFactor, out int belowOnAboveCurve);
}