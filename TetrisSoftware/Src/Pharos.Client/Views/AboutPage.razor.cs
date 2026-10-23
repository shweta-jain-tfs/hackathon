using System.ComponentModel;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;
using Pharos.Client.ViewModels;

namespace Pharos.Client.Views;

public partial class AboutPage : IDisposable
{
    [Inject] private AboutViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        // Subscribe to property changes to trigger UI updates
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        await ViewModel.LoadAsync();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Invoke StateHasChanged on the UI thread to update the component
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}
