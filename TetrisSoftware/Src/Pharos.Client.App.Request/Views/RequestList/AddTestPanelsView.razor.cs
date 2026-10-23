using Pharos.Client.App.Request.ViewModels;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Request.Views.RequestList;

public partial class AddTestPanelsView : IDisposable
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private PanelListViewModel ViewModel { get; set; } = default!;
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    private HashSet<Panel>? _selectedItems;

    protected override void OnInitialized()
    {
        ViewModel.ViewRefreshNeeded += OnViewRefreshNeeded;
    }

    private void OnViewRefreshNeeded(object? sender, EventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    private void SelectedPanelsChanged(HashSet<Panel> selectedPanels)
    {
        _selectedItems = selectedPanels;
    }

    private async Task AddTestPanelsAndCloseDialog(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }

    public void Dispose()
    {
        ViewModel.ViewRefreshNeeded -= OnViewRefreshNeeded;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}