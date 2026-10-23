using Bunit;
using Pharos.Common.Web.Components.Reagent;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Tests;

public class ReagentShapeViewTests : BunitContext
{
    private const string _dualWashColor = "#222223";
    private const string _specificIgeAntiIgeColor = "#191919";
    private const string _ecpColor = "#881737";
    private const string _miscellaneousColor = "#243DAE";
    private const string _elGmColor = "#F5D9E8";
    private const string _eltColor = "#DBEBC1";
    private const string _dilutionColor = "#F2F2F2";
    private const string _whiteColor = "#FFFFFF";

    [Fact]
    public void RendersFriendlyNameAndColor_ForNonCarrierReagent()
    {
        const string friendlyName = "Wash Solution";

        var cut = Render<ReagentShapeView>(parameters => parameters
            .Add(p => p.Shape, ReagentIndicatorShape.Rectangular)
            .Add(p => p.FriendlyName, friendlyName)
            .Add(p => p.Colors, [_whiteColor])
            .Add(p => p.IsCarrierTypeReagent, false));

        cut.Find(".ph-reagent-shapes__text-container")
            .TextContent.Trim()
            .Should().Be(friendlyName);
        cut.Find(".ph-reagent-shapes__shape")
            .GetAttribute("style")
            .Should().Contain($"background-color: {_whiteColor};");
    }

    [Theory]
    [InlineData(_dualWashColor)]
    [InlineData(_specificIgeAntiIgeColor)]
    public void ShouldShowOutline_ForVeryDarkColorsOnDarkSurface_ReturnsTrue(string color)
    {
        ReagentShapeView.ShouldShowOutline([color], ContrastSurface.Dark)
            .Should()
            .BeTrue();
    }

    [Theory]
    [InlineData(_ecpColor)]
    [InlineData(_miscellaneousColor)]
    public void ShouldShowOutline_ForOtherDarkColorsOnDarkSurface_ReturnsFalse(string color)
    {
        ReagentShapeView.ShouldShowOutline([color], ContrastSurface.Dark)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void ShouldShowOutline_WhenAnyDarkSurfaceColorNeedsOutline_ReturnsTrue()
    {
        var colors = new[]
        {
            _ecpColor,
            _miscellaneousColor,
            _dualWashColor
        };

        ReagentShapeView.ShouldShowOutline(colors, ContrastSurface.Dark)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void ShouldShowOutline_WhenNoDarkSurfaceColorNeedsOutline_ReturnsFalse()
    {
        var colors = new[]
        {
            _ecpColor,
            _miscellaneousColor,
            _whiteColor
        };

        ReagentShapeView.ShouldShowOutline(colors, ContrastSurface.Dark)
            .Should()
            .BeFalse();
    }

    [Theory]
    [InlineData(_elGmColor)]
    [InlineData(_eltColor)]
    [InlineData(_dilutionColor)]
    [InlineData(_dualWashColor)]
    public void ShouldShowOutline_ForLightAndPrintSurfaces_ReturnsTrue(string color)
    {
        ReagentShapeView.ShouldShowOutline([color], ContrastSurface.Light)
            .Should()
            .BeTrue();

        ReagentShapeView.ShouldShowOutline([color], ContrastSurface.Print)
            .Should()
            .BeTrue();
    }
}
