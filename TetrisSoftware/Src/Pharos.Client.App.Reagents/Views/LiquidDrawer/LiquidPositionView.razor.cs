using Microsoft.AspNetCore.Components;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Utilities;

namespace Pharos.Client.App.Reagents.Views.LiquidDrawer;

public partial class LiquidPositionView
{
    [Parameter]
    public ICollection<LiquidContainer> Positions { get; set; } = default!;

    private static string GetBottlePositionCss(LiquidContainer position)
    {
        return new CssBuilder("liquid-position-view__bottle-position")
        .AddClass("liquid-position-view__bottle-position--waste", position.Type == LiquidContainerType.Waste)
        .AddClass("liquid-position-view__bottle-position--rinse", position.Type == LiquidContainerType.Rinse)
        .AddClass("liquid-position-view__bottle-position--inuse", position.InUse)
        .Build();
    }
}