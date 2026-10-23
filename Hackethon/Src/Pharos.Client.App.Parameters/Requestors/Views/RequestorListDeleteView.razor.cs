using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.Requestors.Views;
public partial class RequestorListDeleteView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [CascadingParameter] private ModalInstance Modal { get; set; } = default!;
    [Parameter] public string PrimaryButtonText { get; set; } = default!;

    private async Task Close(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}