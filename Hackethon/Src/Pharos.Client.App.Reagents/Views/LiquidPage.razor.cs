using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Pharos.Client.App.Reagents.ViewModels.LiquidDrawer;

namespace Pharos.Client.App.Reagents.Views;

public partial class LiquidPage : ComponentBase, IDisposable
{
    [Inject] private LiquidPageViewModel ViewModel { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        await ViewModel.LoadAsync();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        StateHasChanged();
    }

    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}