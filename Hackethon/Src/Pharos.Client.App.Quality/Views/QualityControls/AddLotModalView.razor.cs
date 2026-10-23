using Microsoft.Extensions.Localization;
using Pharos.Client.App.Quality.Views.QualityControls.Helpers;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Quality.Views.QualityControls;
public partial class AddLotModalView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public bool IsUseQcDefaultsEnabled { get; set; } = false;
    [Parameter] public bool IsUseDefined { get; set; } = false;

    private readonly QualityControlLot _newLot = new();

    private bool IsSaveEnabled()
    {
        if(!IsUseQcDefaultsEnabled && _newLot.LotType == LotType.UseDefaultQcValues)
        {
            return false;
        }
        return MethodQualityControlHelper.IsLotValid(_newLot);
    }
    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }

}
