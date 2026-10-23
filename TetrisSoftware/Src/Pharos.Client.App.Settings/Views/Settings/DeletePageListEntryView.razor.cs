using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Settings.Views.Settings;

public partial class DeletePageListEntryView : ComponentBase
{
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}