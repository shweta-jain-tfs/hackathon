using Pharos.Client.App.Request.ViewModels;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Request.Views.RequestDetail;

public partial class TestDeleteView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    [Parameter] public HashSet<TestOrderViewModel> SelectedRequestTests { get; set; } = default!;

    private string _primaryButtonText = default!;

    protected override void OnInitialized()
    {
        _primaryButtonText = Loc["TestListView_DeleteDialogButtonPrimaryDisabled"];

        if (SelectedRequestTests is { Count: > 0 })
        {
            _primaryButtonText = string.Format(@Loc["TestListView_DeleteDialogButtonPrimary"], SelectedRequestTests.Count);
        }
    }

    private async Task Close(ModalResult modalResult)
    {
        await Modal.CloseAsync(modalResult);
    }
}