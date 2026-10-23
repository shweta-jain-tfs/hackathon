using Pharos.Server.Result.CurveFit;

namespace Pharos.Server.Result.Application.Services.ResultCalculationService;

public class CompetitiveAssay
{
    private readonly IFittedCurve? _fittedCurve;

    public CompetitiveAssay()
    {
        _fittedCurve = ResultCalculationServiceTestsHelpers.GetFittedCurve("El-T", 1);
    }

    [Theory]
    [InlineData(16151, 2.0608442104352123)]
    [InlineData(15370, 2.7837937263003796)]
    [InlineData(11554, 8.0124361245802209)]
    [InlineData(11390, 8.3307974158147111)]
    [InlineData(8873, 15.051980722948157)]
    [InlineData(8437, 16.726597679111677)]
    [InlineData(4206, 62.475564246635663)]
    [InlineData(3970, 69.860454920413474)]
    [Trait("ModuleRequirement", "MR_3541")]
    public void CompetitiveAssay_CalculateConcentration(double response, double expectedConcentration)
    {
        _fittedCurve.Should().NotBeNull();

        if (_fittedCurve != null)
        {
            // Decreasing RU gives increasing concentration 
            var calculatedConcentration = _fittedCurve.ResponseToConcentration(response, out _, 1, 2);

            Math.Round(calculatedConcentration, 5).Should().Be(Math.Round(expectedConcentration, 5));
        }
    }
}
