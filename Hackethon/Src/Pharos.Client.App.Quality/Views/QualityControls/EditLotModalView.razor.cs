using Pharos.Common.Web.Components.Modal;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;
using Pharos.Client.App.Quality.Views.QualityControls.Helpers;

namespace Pharos.Client.App.Quality.Views.QualityControls;

public partial class EditLotModalView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public required QualityControlLot LotToEdit { get; set; }
    [Parameter] public bool IsUseQcDefaultsEnabled { get; set; } = false;
    [Parameter] public bool IsUseDefined { get; set; } = false;

    private bool IsSaveEnabled()
    {
        if (!IsUseQcDefaultsEnabled && LotToEdit.LotType == LotType.UseDefaultQcValues)
        {
            return false;
        }
        return MethodQualityControlHelper.IsLotValid(LotToEdit);
    }

    private string ManufacturingRangeString()
    {
        var min = LotToEdit.ManufacturingRangeMin;
        var max = LotToEdit.ManufacturingRangeMax;
        if (min.HasValue && max.HasValue && !IsUseDefined)
        {
            return string.Format(Loc["MethodQualityControlDetails_ManufacturingRange"], min.Value, max.Value);
        }
        return "";
    }

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}