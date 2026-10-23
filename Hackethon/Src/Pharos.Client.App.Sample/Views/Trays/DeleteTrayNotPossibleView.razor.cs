using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Sample.Views.Trays;

public partial class DeleteTrayNotPossibleView
{
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    [Parameter]
    public string Message { get; set; } = string.Empty;

    [Parameter]
    public string CloseButtonText { get; set; } = string.Empty;

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}
