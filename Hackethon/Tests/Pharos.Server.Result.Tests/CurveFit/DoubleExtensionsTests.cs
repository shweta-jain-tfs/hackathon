using Pharos.Server.Result.CurveFit.PiePuf;

namespace Pharos.Server.Result.CurveFit;

public class DoubleExtensionsTests
{
    private const double _nearlyZero = 1.0e-100;
    private const double _bigValue = 1.0 / _nearlyZero;

    [Theory]
    [InlineData(_bigValue, _bigValue)]
    [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
    public void EqualsOrCloseTo_EqualValues_ReturnsTrue(double value1, double value2)
    {
        var result = value1.EqualsOrCloseTo(value2);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(0d, _nearlyZero)]
    [InlineData(-_nearlyZero, 0d)]
    public void EqualsOrCloseTo_ValuesWithinAbsoluteFallback_ReturnsTrue(double value1, double value2)
    {
        var result = value1.EqualsOrCloseTo(value2);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(0d, 1e-12)]
    [InlineData(-1e-12, 0d)]
    public void EqualsOrCloseTo_ValuesAtAbsoluteFallback_ReturnsFalse(double value1, double value2)
    {
        var result = value1.EqualsOrCloseTo(value2);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(_bigValue, _bigValue * 1.0000000005d)]
    [InlineData(-_bigValue, -_bigValue * 1.0000000005d)]
    public void EqualsOrCloseTo_ValuesWithinRelativeTolerance_ReturnsTrue(double value1, double value2)
    {
        var result = value1.EqualsOrCloseTo(value2);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(_bigValue, _bigValue * 1.000000002d)]
    [InlineData(-_bigValue, -_bigValue * 1.000000002d)]
    public void EqualsOrCloseTo_ValuesOutsideRelativeTolerance_ReturnsFalse(double value1, double value2)
    {
        var result = value1.EqualsOrCloseTo(value2);

        result.Should().BeFalse();
    }

    [Fact]
    public void EqualsOrCloseTo_NaN_ReturnsFalse()
    {
        var result = double.NaN.EqualsOrCloseTo(double.NaN);

        result.Should().BeFalse();
    }
}