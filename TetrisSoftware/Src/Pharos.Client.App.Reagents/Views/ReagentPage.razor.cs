using Microsoft.AspNetCore.Components;
using Pharos.Client.App.Reagents.ViewModels;

namespace Pharos.Client.App.Reagents.Views;

public partial class ReagentPage : IDisposable
{
    [Inject] private ReagentPageViewModel ViewModel { get; set; } = default!;

    private bool _isReagentLoaderPanelSelected;

    protected override async Task OnInitializedAsync()
    {
        await ViewModel.LoadAsync();
    }

    private void OnLoaderSelected()
    {
        _isReagentLoaderPanelSelected = true;
        ViewModel.ClearLegendSelection();
    }

    private void OnCloseClicked()
    {
        _isReagentLoaderPanelSelected = false;
        ViewModel.ClearLegendSelection();
    }

    public void Dispose()
    {
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}
