using Microsoft.AspNetCore.Components;
using Pharos.Client.App.Reagents.ViewModels;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Icons;
using Pharos.Common.Web.Utilities;

namespace Pharos.Client.App.Reagents.Views.Drawer.Carrier;
public partial class ReagentCarrierView
{
    [Parameter]
    public CompartmentCellContent CellContent { get; set; } = default!;

    [CascadingParameter]
    public bool IsScanningOnGoing { get; set; } = default!;

    [Parameter]
    public ReagentIconSize Size { get; set; } = default!;

    [Parameter]
    public bool? IsReagentSelected { get; set; } = default!;

    [Parameter]
    public ReagentPositionStatus? LegendSelectedStatus { get; set; }

    private string _reagentCarrierSlotCss = string.Empty;

    private string _reagentCarrierCss = string.Empty;

    private StatusIconType? _statusIconType;

    private StatusIconSize? _statusIconSize = StatusIconSize.Sm;

    protected override void OnParametersSet()
    {
        SetStatusIcon();
        SetReagentCarrierSlotCss();
        SetReagentCarrierCss();
    }

    private void SetStatusIcon()
    {
        SetStatusIconType();
        SetStatusIconSize();
    }

    private void SetStatusIconSize()
    {
        _statusIconSize = Size switch
        {
            ReagentIconSize.Small => StatusIconSize.Sm,
            ReagentIconSize.Large => StatusIconSize.Md,
            _ => StatusIconSize.Sm
        };
    }

    private void SetStatusIconType()
    {
        switch (CellContent.Status)
        {
            case ReagentPositionStatus.Identified:
                _statusIconType = StatusIconType.Progress;
                break;
            case ReagentPositionStatus.TransferNotPossible:
            case ReagentPositionStatus.UnknownArticle:
            case ReagentPositionStatus.UnreadableBarcode:
            case ReagentPositionStatus.InternalStorageFull:
                _statusIconType = StatusIconType.Warning;
                break;
            default:
                _statusIconType = null;
                break;
        }
    }

    private void SetReagentCarrierSlotCss()
    {
        _reagentCarrierSlotCss = new CssBuilder("reagent-carrier__carrier-slot")
            .AddClass("reagent-carrier__carrier-slot--not-identified", CellContent.Status == ReagentPositionStatus.NotIdentified)
            .Build();
    }

    private void SetReagentCarrierCss()
    {
        _reagentCarrierCss = new CssBuilder("reagent-carrier")
                    .AddClass("reagent-carrier--scan", IsScanningOnGoing)
                    .AddClass("reagent-carrier--small", Size == ReagentIconSize.Small)
                    .AddClass("reagent-carrier--large", Size == ReagentIconSize.Large)
                    .AddClass("reagent-carrier--highlighted", IsLegendSelectionMatch())
                    .AddClass("reagent-carrier--dim", LegendSelectedStatus.HasValue && !IsLegendSelectionMatch() && Size == ReagentIconSize.Large)
                    .AddClass("reagent-carrier--selected", IsReagentSelected)
                    .Build();
    }

    private bool IsLegendSelectionMatch()
    {
        if (!LegendSelectedStatus.HasValue)
        {
            return false;
        }

        return LegendSelectedStatus.Value == ReagentPositionStatus.UnloadRequested
            ? CellContent.IsUnloadRequested
            : LegendSelectedStatus.Value == CellContent.Status;
    }
}
