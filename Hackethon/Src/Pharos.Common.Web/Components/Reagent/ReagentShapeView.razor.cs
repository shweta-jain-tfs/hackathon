using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Reagent;

public partial class ReagentShapeView
{
    [Parameter]
    public ReagentIndicatorShape Shape { get; set; }

    [Parameter]
    public ICollection<string> Texts { get; set; } = new List<string>();

    [Parameter]
    public ICollection<string> Colors { get; set; } = new List<string>();

    [Parameter]
    public bool IsCarrierTypeReagent { get; set; } = false;

    [Parameter]
    public string Test { get; set; } = string.Empty;

    [Parameter]
    public ReagentShapeGap Gap { get; set; } = ReagentShapeGap.Xs;

    [Parameter]
    public string FriendlyName { get; set; } = string.Empty;

    [Parameter]
    public ContrastSurface? ContrastSurfaceOverride { get; set; }

    /// <summary>
    /// Contrast ratio threshold for dark icon visibility. Icons below 1.5 receive an outline
    /// in dark mode to prevent them from appearing invisible against dark backgrounds.
    /// Note: This is a UX aesthetic choice by design, not a WCAG accessibility requirement.
    /// </summary>
    private const double _darkSurfaceMinimumContrastRatio = 1.5;

    private string _shapeSlotCss = string.Empty;

    private string _containerCss = string.Empty;

    private string _names = string.Empty;

    protected override void OnParametersSet()
    {
        // Graceful fallback for unexpected nulls from upstream data.
        Colors ??= [];
        Texts ??= [];

        SetShapeSlotCss();
        SetNames();
        SetContainerCss();
    }

    private void SetContainerCss()
    {
        var surfaceType = ContrastSurfaceOverride ?? ContrastSurface.Dark;
        var renderedColors = Shape == ReagentIndicatorShape.Slanted ? Colors : Colors.Take(1);
        var needsOutline = ShouldShowOutline(renderedColors, surfaceType);

        _containerCss = new CssBuilder("ph-reagent-shapes")
            .AddClass("ph-reagent-shapes--gap-sm", Gap == ReagentShapeGap.Sm)
            .AddClass("ph-reagent-shapes--contrast-outline", needsOutline)
            .Build();
    }

    private void SetNames()
    {
        if (IsCarrierTypeReagent)
        {
            _names = Test ?? string.Empty;
        }
        else
        {
            _names = Texts is { Count: > 0 }
                ? string.Join("/", Texts.Where(text => !string.IsNullOrEmpty(text)))
                : string.Empty;

            if (!string.IsNullOrEmpty(FriendlyName))
            {
                _names = $"{_names} {FriendlyName}";
            }
        }
    }

    private void SetShapeSlotCss()
    {
        if (Shape != ReagentIndicatorShape.None)
        {
            _shapeSlotCss = new CssBuilder("ph-reagent-shapes__shape")
                .AddClass("ph-reagent-shapes__shape--rectangle", Shape == ReagentIndicatorShape.Rectangular)
                .AddClass("ph-reagent-shapes__shape--slanted", Shape == ReagentIndicatorShape.Slanted)
                .AddClass("ph-reagent-shapes__shape--circle", Shape == ReagentIndicatorShape.Round)
                .Build();
        }
    }

    private static string GetShapeColor(string color)
    {
        return $"background-color: {color};";
    }

    internal static bool ShouldShowOutline(
        IEnumerable<string> colors,
        ContrastSurface surface)
    {
        return surface switch
        {
            ContrastSurface.Dark =>
                ContrastChecker.AnyColorBelowMinimumContrastRatio(
                    colors,
                    surface,
                    _darkSurfaceMinimumContrastRatio),

            // All icons receive outlines in light and print modes
            // for visual uniformity, since most icons are bright.
            ContrastSurface.Light or ContrastSurface.Print => true,

            _ => throw new ArgumentOutOfRangeException(nameof(surface), surface, null)
        };
    }
}

public enum ReagentShapeGap
{
    Xs,
    Sm,
}

public enum ReagentIndicatorShape
{
    None = 0,
    Round = 1,
    Rectangular = 2,
    Slanted = 3,
}
