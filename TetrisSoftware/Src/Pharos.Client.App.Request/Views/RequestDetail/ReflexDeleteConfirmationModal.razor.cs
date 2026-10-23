using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Request.Views.RequestDetail;
public partial class ReflexDeleteConfirmationModal
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [CascadingParameter] private ModalInstance Modal { get; set; } = default!;

    private async Task Close(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}