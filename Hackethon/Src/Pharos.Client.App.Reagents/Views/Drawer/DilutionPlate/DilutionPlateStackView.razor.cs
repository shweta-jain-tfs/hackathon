using Microsoft.AspNetCore.Components;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Utilities;

namespace Pharos.Client.App.Reagents.Views.Drawer.DilutionPlate;
public partial class DilutionPlateStackView
{
    [Parameter]
    public DilutionPlateStack DilutionPlate { get; set; } = null!;

    [CascadingParameter]
    public bool IsScanningOnGoing { get; set; } = default!;

    [Parameter]
    public ReagentPositionStatus? LegendSelectedStatus { get; set; }

    private string _plateSlotCss = string.Empty;

    private string _plateSlotStatusCss = string.Empty;

    private bool _hasError;

    protected override void OnParametersSet()
    {
        SetHasError();
        SetPlateSlotCss();
        SetPlateSlotStatusCss();
    }

    private void SetPlateSlotCss()
    {
        _plateSlotCss = new CssBuilder("ph-dilution-plate-slot")
            .AddClass("ph-dilution-plate-slot--empty", DilutionPlate.IsEmpty && !IsScanningOnGoing)
            .AddClass("ph-dilution-plate-slot--scan", IsScanningOnGoing)
            .AddClass("ph-dilution-plate-slot--error", _hasError && !IsScanningOnGoing)
            .AddClass("ph-dilution-plate-slot--highlighted", LegendSelectedStatus.HasValue && LegendSelectedStatus.Value == DilutionPlate.Status)
            .Build();
    }

    private void SetPlateSlotStatusCss()
    {
        _plateSlotStatusCss = new CssBuilder("ph-dilution-plate-slot__status")
            .AddClass("ph-dilution-plate-slot__status--scan", IsScanningOnGoing)
            .Build();
    }

    private void SetHasError()
    {
        _hasError = DilutionPlate.Status == ReagentPositionStatus.DilutionPlateNotDetected ||
            DilutionPlate.Status == ReagentPositionStatus.DilutionPlateNotPickedUp;
    }
}
