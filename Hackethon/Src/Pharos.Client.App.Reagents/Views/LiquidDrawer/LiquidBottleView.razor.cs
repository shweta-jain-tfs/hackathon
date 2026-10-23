using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Icons;
using Pharos.Common.Web.Utilities;
using Pharos.Common.Web.Components.Reagent;

namespace Pharos.Client.App.Reagents.Views.LiquidDrawer;

public partial class LiquidBottleView : ComponentBase
{
    [Parameter] public LiquidContainer Container { get; set; } = new();
    [Parameter] public LiquidBottleStatus? SelectedLegendStatus { get; set; }
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    protected override void OnParametersSet()
    {
        StateHasChanged();
    }

    private string LiquidBottleCss => new CssBuilder("ph-liquid-bottle-view")
        .AddClass("ph-liquid-bottle-view--indicator", !IsErrorStatus && Container.FillOrEmpty == FillOrEmptyIndicationType.Pending)
        .AddClass("ph-liquid-bottle-view--disabled", Container.Status == ReagentPositionStatus.Disabled)
        .AddClass("ph-liquid-bottle-view--in-use", Container.InUse)
        .AddClass("ph-liquid-bottle-view--empty", Container.Status == ReagentPositionStatus.Empty)
        .AddClass("ph-liquid-bottle-view--present", ShowLiquidFill)
        .AddClass("ph-liquid-bottle-view--piped", Container.IsPipedEnabled)
        .AddClass("ph-liquid-bottle-view--error", IsErrorStatus || Container.FillOrEmpty == FillOrEmptyIndicationType.Warning)
        .AddClass("ph-liquid-bottle-view--alarm", Container.FillOrEmpty == FillOrEmptyIndicationType.Alarm)
        .AddClass("ph-liquid-bottle-view--highlighted", IsHighlighted())
        .AddClass("ph-liquid-bottle-view--dim", IsDimmed())
        .Build();

    private bool IsErrorStatus => Container.Status == ReagentPositionStatus.DisconnectedBottle ||
                                  Container.Status == ReagentPositionStatus.UndetectedVolume ||
                                  Container.Status == ReagentPositionStatus.MissingBottle;

    private bool ShowLiquidFill => !IsErrorStatus && Container.Status != ReagentPositionStatus.Disabled && !Container.IsMissing;

    private string FillStyle => $"--liquid-bottle-fill-level: {GetDisplayFillPercentage()}%";

    private StatusIconType GetWasteIconType() => Container.FillOrEmpty switch
    {
        FillOrEmptyIndicationType.None => StatusIconType.Off,
        FillOrEmptyIndicationType.Alarm => StatusIconType.Alarm,
        FillOrEmptyIndicationType.Warning => StatusIconType.Warning,
        FillOrEmptyIndicationType.Pending => StatusIconType.PendingAction,
        _ => throw new InvalidOperationException("Invalid FillOrEmptyIndicationType")
    };

    private string GetPipedText() => Container.Type switch
    {
        LiquidContainerType.Waste => Loc["LiquidSettings_Waste_PipedOut"],
        LiquidContainerType.Rinse => Loc["LiquidSettings_Rinse_PipedIn"],
        _ => string.Empty
    };

    private string GetEmptyOrRefillText() => Container.Type switch
    {
        LiquidContainerType.Waste => Loc["LiquidsPage_Waste_Empty"],
        LiquidContainerType.Rinse => Loc["LiquidsPage_Rinse_Refill"],
        _ => string.Empty
    };

    private string GetErrorText() => Container.Status.ToLocalizedString("Reagent");

    private int GetDisplayFillPercentage() => Container.Percentage > 100 ? 100 : Container.Percentage;

    private bool IsHighlighted()
    {
        return SelectedLegendStatus.HasValue
               && Container.ToLiquidBottleStatus() == SelectedLegendStatus;
    }

    private bool IsDimmed()
    {
        return SelectedLegendStatus.HasValue
               && Container.ToLiquidBottleStatus() != SelectedLegendStatus;
    }
}
