using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Layout;

/// <summary>
/// 12 column full height grid. Use with <see cref="PhGridColumn"/>.
/// </summary>
public partial class PhGrid
{
    /// <summary>
    /// Constrain row height to minimum content (don't grow to fill the height of the grid).
    /// Aligns the content to the top of the grid.
    /// </summary>
    [Parameter] public bool AlignTop { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public bool FillHeight { get; set; }

    private string ClassNames { get; set; } = "";

    protected override void OnParametersSet()
    {
        ClassNames = new CssBuilder("ph-grid-layout")
            .AddClass("ph-grid-layout--shrink-wrap", AlignTop)
            .AddClass("ph-grid-layout--fill-height", FillHeight)
            .Build();
    }
}