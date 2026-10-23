using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Result.CurveFit;
using ConcentrationStatus = Pharos.Server.Result.CurveFit.ConcentrationStatus;

namespace Pharos.Server.Result.Application.Services.ResultCalculationService;

public class TestWellCorrectionFactorTests
{
    private readonly IFittedCurve? _fittedCurve;
    private readonly TestWellCorrectionFactor _sut;

    public TestWellCorrectionFactorTests()
    {
        var logger = new Mock<ILogger>();
        _sut = new TestWellCorrectionFactor(logger.Object);

        _fittedCurve = ResultCalculationServiceTestsHelpers.GetFittedCurve("El-T", 1);
    }

    [Theory]
    [InlineData(15055, ConcentrationStatus.Ok, 0.85, 2, 2.6351, 2.6352)]
    [InlineData(10050, ConcentrationStatus.Ok, 0.85, 2, 9.693, 9.69301)]
    [InlineData(6000, ConcentrationStatus.Ok, 0.85, 2, 27.434, 27.4349)]
    public void TestThatTheCurveToUseHasNotChanged(double response, ConcentrationStatus concentrationStatus, double concentrationFactor, int dilutionFactor, double minConcentration, double maxConcentration)
    {
        _fittedCurve.Should().NotBeNull();

        if (_fittedCurve != null)
        {
            // This is like a check that the curve, to be used for the other TWCF tests, is as the tests expects it to be
            var conc = _fittedCurve.ResponseToConcentration(response, out var concStatus, concentrationFactor, dilutionFactor);

            concStatus.Should().Be(concentrationStatus);
            conc.Should().BeInRange(minConcentration, maxConcentration);
        }
    }

    [Theory]
    [InlineData(3000, 3231.1958104321739)]
    [InlineData(4000, 4054.63248316031)]
    [InlineData(5030, 4799.10193330036)]
    [InlineData(6070, 5650.7199527051243)]
    [InlineData(9234, 8509.8319768370948)]
    [InlineData(13637, 12672.721165354755)]
    [InlineData(15212, 14175.110297706286)]
    [InlineData(17611, 16468.3999434819)]
    [InlineData(19211, 17999.612086367233)]
    [InlineData(20756, 19468.531233956888)]
    [Trait("ModuleRequirement", "MR_3798")]
    public void TestWellCorrectionFactor(double response, double expectedNewResponse)
    {
        _fittedCurve.Should().NotBeNull();
        if (_fittedCurve != null)
        {
            var carrierBarcode = "D14EJESE2B6345";
            var adjustmentForAlfaBeta1Beta2 = 30.0;
            var lot = new EliaCalibratorWellLotInformation
            {
                Alpha = 5.32,
                Beta1 = 5.06,
                Beta2 = 4.02,
                IsChecksumValid = true
            };
            var result = _sut.AdjustResponseByTestWellCorrectionFactor(_fittedCurve, response, carrierBarcode, lot, adjustmentForAlfaBeta1Beta2);

            // Equal up to 5th decimal... (for response values: decimals not really used...)
            Math.Round(result.NewResponse, 5).Should().Be(Math.Round(expectedNewResponse, 5));
            result.Result.Should().Be(true);
        }
    }
    [Theory]
    [InlineData(4000, 30, true)]
    [InlineData(4000, 31, false)]
    [Trait("ModuleRequirement", "MR_3798")]
    public void TestWellCorrectionFactor_TooManyIterations(double response, double adjustmentForAlfaBeta1Beta2, bool adjustmentDone)
    {
        _fittedCurve.Should().NotBeNull();
        if (_fittedCurve != null)
        {
            var carrierBarcode = "D14EJESE2B6345";
            var lot = new EliaCalibratorWellLotInformation
            {
                Alpha = 5.32,
                Beta1 = 5.06,
                Beta2 = 4.02,
                IsChecksumValid = true
            };
            var result = _sut.AdjustResponseByTestWellCorrectionFactor(_fittedCurve, response, carrierBarcode, lot, adjustmentForAlfaBeta1Beta2);

            result.Result.Should().Be(adjustmentDone);

        }
    }
}
