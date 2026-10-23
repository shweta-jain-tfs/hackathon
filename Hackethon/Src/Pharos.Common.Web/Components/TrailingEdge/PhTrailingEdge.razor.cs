using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.TrailingEdge;

/// <summary>
/// Put inside a scrollable element to show a faded gradient along the edge where more content can be
/// scrolled into view.
/// </summary>
/// <remarks>
/// Intended for use inside other components that have scrollable content.
/// Uses the color from the --ph-bg-color custom property to set the color of the gradient.
/// </remarks>
public partial class PhTrailingEdge
{
    [Parameter] public TrailingEdgeSize Size { get; set; } = TrailingEdgeSize.Default;
    [Parameter] public TrailingEdgeDisplayMode Mode { get; set; } = TrailingEdgeDisplayMode.ShowAllEdges;
    /// <summary>
    /// Used together with <see cref="TrailingEdgeDisplayMode.VerticalOnly"/> when the content clips and shows ellipsis instead of scrolling horizontally
    /// </summary>
    [Parameter] public bool AllowClipping { get; set; }
    [Parameter] public bool Fill { get; set; }
    /// <summary>
    /// Option to apply custom css class names to the content element.
    /// </summary>
    [Parameter] public string ContentClassNames { get; set; } = "flex flex-col";
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private string? CssClasses { get; set; }
    private string? ContentCssClasses { get; set; }

    protected override void OnParametersSet()
    {
        CssClasses = new CssBuilder("ph-trailing-edge")
            .AddClass("ph-trailing-edge--hide-top", Mode == TrailingEdgeDisplayMode.HideTopEdge || Mode == TrailingEdgeDisplayMode.HideTopAndLeftEdge)
            .AddClass("ph-trailing-edge--hide-left", Mode == TrailingEdgeDisplayMode.HideTopAndLeftEdge)
            .AddClass("ph-trailing-edge--vertical-only", Mode == TrailingEdgeDisplayMode.VerticalOnly)
            .AddClass("ph-trailing-edge--clip-content", AllowClipping)
            .AddClass("ph-trailing-edge--wide", Size == TrailingEdgeSize.Wide)
            .AddClass("ph-trailing-edge--extra-wide", Size == TrailingEdgeSize.ExtraWide)
            .AddClass("ph-trailing-edge--fill-height", Fill)
            .Build();

        ContentCssClasses = new CssBuilder("ph-trailing-edge__content")
            .AddClass(ContentClassNames)
            .Build();
    }
}