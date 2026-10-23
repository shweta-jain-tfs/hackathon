using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Utilities;

namespace Pharos.Client.App.Reagents.Views.SolidWasteDrawer;

public partial class SolidWasteContainerView : ComponentBase
{
    [Parameter] public WasteContainer Container { get; set; } = new();
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    private string WasteContainerCss => new CssBuilder("ph-solid-waste-container-view")
        .AddClass("ph-solid-waste-container-view--empty", !Container.IsPresent)
        .AddClass("ph-solid-waste-container-view--filled", !Container.IsEmpty && !Container.IsPresent)
        .AddClass("ph-solid-waste-container-view--indicator", !IsErrorStatus && Container.EmptyIndicationType == FillOrEmptyIndicationType.Pending)
        .AddClass("ph-solid-waste-container-view--error", IsErrorStatus || Container.EmptyIndicationType == FillOrEmptyIndicationType.Warning)
        .AddClass("ph-solid-waste-container-view--alarm", Container.EmptyIndicationType == FillOrEmptyIndicationType.Alarm)
        .Build();

    private bool IsErrorStatus => Container.Status == ReagentPositionStatus.UndetectedVolume
        || Container.Status == ReagentPositionStatus.MissingBottle;

    private bool ShowWasteFill => !IsErrorStatus && Container.IsPresent;

    private string FillStyle => $"--waste-fill-level: {Container.Percentage}%";

    private string GetErrorText() => Container.Status switch
    {
        ReagentPositionStatus.UndetectedVolume => Loc["SolidWasteDrawer_UndetectedVolume_Error"],
        ReagentPositionStatus.MissingBottle => Loc["SolidWasteDrawer_MissingBin_Error"],
        _ => string.Empty
    };
}
