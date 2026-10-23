using Microsoft.AspNetCore.Components;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Utilities;

namespace Pharos.Client.App.Reagents.Views.BottleDrawer;

public partial class BottlePositionView : ComponentBase
{
    [Parameter] public BottlePositionSize Size { get; set; }
    [Parameter] public BottleDetail Bottle { get; set; } = new();
    [Parameter] public EventCallback<int> OnBottleSelected { get; set; }
    [Parameter] public bool IsSelected { get; set; }
    [Parameter] public bool IsFlashing { get; set; }
    [Parameter] public bool IsDimmed { get; set; }
    [Parameter] public bool IsHighlighted { get; set; }

    private string BottlePositionCss => new CssBuilder("bottle-position")
        .AddClass("bottle-position--circle", Size == BottlePositionSize.Small)
        .AddClass("bottle-position--circle--selected", IsSelected && Size == BottlePositionSize.Small)
        .AddClass("bottle-position--selected", IsSelected && Size == BottlePositionSize.Large)
        .AddClass("bottle-position--highlighted", IsHighlighted)
        .AddClass("bottle-position--dim", IsDimmed)
        .Build();

    private string BottlePositionLabelCss => new CssBuilder("bottle-position__label")
        .AddClass("bottle-position__label--inuse", Bottle.InUse)
        .Build();

    private bool IsBottleSelectable()
    {
        return Bottle.IsPresent && !string.IsNullOrEmpty(Bottle.ReagentUdi);
    }

    private void BottleClicked()
    {
        if (IsBottleSelectable())
        {
            OnBottleSelected.InvokeAsync(Bottle.Id);
        }
    }

    private string BottleIndicatorPositionCss => new CssBuilder("bottle-position__loading-indicator")
        .AddClass("bottle-position__loading-indicator--rectangle", Size == BottlePositionSize.Large)
        .AddClass("bottle-position__loading-indicator--circle", Size == BottlePositionSize.Small)
        .AddClass($"bottle-position__loading-indicator--circle--top", Size == BottlePositionSize.Small && Bottle.Position == 1)
        .AddClass($"bottle-position__loading-indicator--circle--bottom", Size == BottlePositionSize.Small && Bottle.Position == 2)
        .Build();
}
