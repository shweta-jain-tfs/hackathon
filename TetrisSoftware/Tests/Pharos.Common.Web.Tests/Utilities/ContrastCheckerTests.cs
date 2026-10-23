using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Tests.Utilities;

public class ContrastCheckerTests
{
    // Values can be manually cross-checked with WebAIM's Contrast Checker:
    // https://webaim.org/resources/contrastchecker/
    // WebAIM displays contrast ratios truncated to two decimal places.
    [Theory]
    [InlineData("#000000", "#FFFFFF", 21.0)] // Maximum contrast
    [InlineData("#FFFFFF", "#000000", 21.0)] // Reversed order
    [InlineData("#842573", "#842573", 1.0)] // Same color
    [InlineData("#E3660E", "#FFFFFF", 3.4)] // WCAG example
    [InlineData("#949494", "#FFFFFF", 3.03)] // WebAIM truncated value (near 3)
    [InlineData("#959595", "#FFFFFF", 2.99)] // WebAIM truncated value (near 3)
    public void GetContrastRatio_ReturnsExpectedValue(
        string firstColor,
        string secondColor,
        double expected)
    {
        var actual = ContrastChecker.GetContrastRatio(
            firstColor,
            secondColor);

        actual.Should().BeApproximately(expected, 0.01);
    }

    [Theory]
    [InlineData("#949494", false)] // 3.03:1, above 3
    [InlineData("#959595", true)]  // 2.99:1, below 3
    public void IsColorBelowMinimumContrastRatio_ReturnsExpectedResult(
        string color,
        bool expected)
    {
        ContrastChecker.IsColorBelowMinimumContrastRatio(
                color,
                ContrastSurface.Print,
                ContrastChecker.WcagAaNonTextMinimumContrastRatio)
            .Should()
            .Be(expected);
    }

    [Theory]
    [InlineData(2.0, false)] // Actual ratio is approximately 2.995
    [InlineData(4.0, true)]  // Actual ratio is below 4
    public void IsColorBelowMinimumContrastRatio_UsesSuppliedMinimumContrastRatio(
        double minimumContrastRatio,
        bool expected)
    {
        ContrastChecker.IsColorBelowMinimumContrastRatio(
                "#959595",
                ContrastSurface.Print,
                minimumContrastRatio)
            .Should()
            .Be(expected);
    }

    [Fact]
    public void AnyColorBelowMinimumContrastRatio_ReturnsTrueWhenAnyColorIsBelowRatio()
    {
        var colors = new[]
        {
            "#949494", // Above 3:1
            "#959595"  // Below 3:1
        };

        ContrastChecker.AnyColorBelowMinimumContrastRatio(
                colors,
                ContrastSurface.Print,
                ContrastChecker.WcagAaNonTextMinimumContrastRatio)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void AnyColorBelowMinimumContrastRatio_ReturnsFalseWhenAllColorsMeetRatio()
    {
        var colors = new[]
        {
            "#000000",
            "#949494"
        };

        ContrastChecker.AnyColorBelowMinimumContrastRatio(
                colors,
                ContrastSurface.Print,
                ContrastChecker.WcagAaNonTextMinimumContrastRatio)
            .Should()
            .BeFalse();
    }
}