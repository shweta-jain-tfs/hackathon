using Microsoft.AspNetCore.Components;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Utilities;

namespace Pharos.Client.App.Reagents.Views.BottleDrawer;

public partial class BottleContentView : ComponentBase
{
    [Parameter] public BottlePositionSize Size { get; set; }
    [Parameter] public BottleDetail Bottle { get; set; } = new();
    [Parameter] public bool IsFlashing { get; set; }

    private bool HasError => Bottle.Status == ReagentPositionStatus.UndetectedVolume ||
                             Bottle.Status == ReagentPositionStatus.DisconnectedBottle ||
                             Bottle.Status == ReagentPositionStatus.UnexpectedVolume ||
                             Bottle.Status == ReagentPositionStatus.UnknownBottle ||
                             Bottle.Status == ReagentPositionStatus.Expired;

    private string FillStyle => $"--bottle-content-level-fill: {Bottle.FillPercentage}%";

    private string BottleShapeContainerCss => new CssBuilder("bottle-content__shape")
        .AddClass("bottle-content__shape--rect", Size == BottlePositionSize.Large)
        .AddClass("bottle-content__shape--circle", Size == BottlePositionSize.Small)
        .AddClass("bottle-content__shape--content", Bottle.IsPresent && Bottle.FillPercentage >= 0)
        .AddClass("bottle-content__shape--empty", !Bottle.IsPresent)
        .AddClass("bottle-content__shape--highlight", IsFlashing)
        .AddClass("bottle-content__shape--error", HasError)
        .Build();

    private string BottleContentLiquidCss => new CssBuilder("")
        .AddClass("bottle-content__liquid-fill", !HasError && Bottle.IsPresent)
        .AddClass("bottle-content__liquid-fill--dim", Bottle.IsPresent && Bottle.UseRemainingQuantity)
        .AddClass("bottle-content__liquid-fill--bright", Bottle.IsPresent && !Bottle.UseRemainingQuantity)
        .Build();
}