using Pharos.Common.Web.Components.Modal;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Quality.Views.QualityControls;
public partial class DeleteLotModalView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public required string LotNumbers { get; set; }

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }

}
