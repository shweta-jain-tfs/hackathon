using Microsoft.Extensions.Localization;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Result.Views.CurveControl;
public partial class AcceptCurveModalView
{
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}
