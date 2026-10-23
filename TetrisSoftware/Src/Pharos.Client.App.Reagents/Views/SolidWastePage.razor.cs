using Microsoft.AspNetCore.Components;
using Pharos.Client.App.Reagents.ViewModels.SolidWasteDrawer;

namespace Pharos.Client.App.Reagents.Views;

public partial class SolidWastePage : ComponentBase, IDisposable
{
    [Inject] private SolidWastePageViewModel ViewModel { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await ViewModel.LoadAsync();
    }

    public void Dispose()
    {
        ViewModel?.Dispose();
        GC.SuppressFinalize(this);
    }
}
