using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.Requestors.Views;

public partial class InformationModal : ComponentBase
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    [Parameter] public string Message { get; set; } = string.Empty;
    [Parameter] public int Skipped { get; set; }

    protected override void OnParametersSet()
    {
        // If Skipped parameter is provided, format the message accordingly
        if (Skipped > 0 && string.IsNullOrEmpty(Message))
        {
            Message = string.Format(Loc["RequestorListPage_SkippedDeletionMessage"], Skipped);
        }
    }

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}