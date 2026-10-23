using Microsoft.AspNetCore.Components;
using Pharos.Client.App.Reagents.ViewModels;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Icons;
using Pharos.Common.Web.Utilities;

namespace Pharos.Client.App.Reagents.Views.Drawer.Cartridge;
public partial class ReagentCartridgeView
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

    private string _reagentCartridgeCss = string.Empty;

    private StatusIconType? _statusIconType;

    private StatusIconSize? _statusIconSize = StatusIconSize.Sm;

    private string _reagentCartridgeSlotCss = string.Empty;

    private string _cartridgeSelectionFrameCss = string.Empty;

    private bool _shouldShowOverlay;

    protected override void OnParametersSet()
    {
        SetShouldShowOverlay();
        SetStatusIcon();
        SetReagentCartridgeCss();
        SetReagentCartridgeSlotCss();
    }

    private void SetReagentCartridgeSlotCss()
    {
        _reagentCartridgeSlotCss = new CssBuilder("reagent-cartridge__cartridge-slot")
            .AddClass("reagent-cartridge__cartridge-slot--error", _statusIconType.HasValue && _statusIconType.Value == StatusIconType.Warning)
            .Build();
    }

    private void SetReagentCartridgeCss()
    {
        _reagentCartridgeCss = new CssBuilder("reagent-cartridge")
            .AddClass("reagent-cartridge--small", Size == ReagentIconSize.Small)
            .AddClass("reagent-cartridge--large", Size == ReagentIconSize.Large)
            .AddClass("reagent-cartridge--identified", _statusIconType.HasValue && _statusIconType.Value == StatusIconType.Progress)
            .AddClass("reagent-cartridge--error", _statusIconType.HasValue && _statusIconType.Value == StatusIconType.Warning)
            .AddClass("reagent-cartridge--scan", IsScanningOnGoing)
            .AddClass("reagent-cartridge--highlighted", Size == ReagentIconSize.Small && IsLegendSelectionMatch())
            .AddClass("reagent-cartridge--dim", Size == ReagentIconSize.Small && LegendSelectedStatus.HasValue && !IsLegendSelectionMatch())
            .Build();

        _cartridgeSelectionFrameCss = new CssBuilder("")
            .AddClass("reagent-cartridge__selection-frame", Size == ReagentIconSize.Large && !CellContent.IsEmpty)
            .AddClass("selected", IsReagentSelected)
            .AddClass("reagent-cartridge__selection-frame--highlighted", IsLegendSelectionMatch())
            .AddClass("reagent-cartridge__selection-frame--dim", LegendSelectedStatus.HasValue && !IsLegendSelectionMatch())
            .Build();

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

    private void SetShouldShowOverlay()
    {
        _shouldShowOverlay = LegendSelectedStatus.HasValue && !IsLegendSelectionMatch() && Size == ReagentIconSize.Large;
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
