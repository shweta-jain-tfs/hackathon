using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Identity.Views.User;
public partial class DeleteUserModalView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    [Parameter] public required string UserNames { get; set; }

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}
