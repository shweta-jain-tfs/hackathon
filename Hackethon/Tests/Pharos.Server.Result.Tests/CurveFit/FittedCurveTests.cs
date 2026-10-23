using Pharos.Server.Result.CurveFit.PiePuf;

namespace Pharos.Server.Result.CurveFit;

public class FittedCurveTests
{
    [Fact(Timeout = 1_000)]
    public async Task ResponseToConcentration_MalformedDescendingRodbard5_ReturnsUpperBoundary()
    {
        await Task.Delay(1, TestContext.Current.CancellationToken);

        var additionalData = new CurveAdditionalDataTypeCommon
        {
            CurveMinConcentration = 0,
            CurveMaxConcentration = 100
        };
        var fitParameters = new FitParTypeCommon
        {
            Model = (int)ModelNumCommon.MdRodbard5,
            ParamNb = 5,
            ParamValue = [100, 0, 0, 1, 1]
        };
        var fittedCurve = new FittedCurve(
            new CurveFitterApi(),
            additionalData,
            fitParameters,
            new CurveCheckResultCommon(),
            new CurveFitResultCommon { IsCurveFitOk = true });

        var concentration = fittedCurve.ResponseToConcentration(50, out var status);

        concentration.Should().Be(1e100);
        status.Should().Be(ConcentrationStatus.Over);
    }
}
