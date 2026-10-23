using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Request.Views.RequestList;
public partial class RequestListDeleteView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [CascadingParameter] private ModalInstance Modal { get; set; } = default!;
    [Parameter] public string PrimaryButtonText { get; set; } = default!;

    private async Task Close(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }
}