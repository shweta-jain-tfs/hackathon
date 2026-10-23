using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Reagents.ViewModels.LiquidDrawer;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Reagents.Views.LiquidDrawer;

public partial class LiquidDrawerView : IDisposable
{
    [Parameter] public LiquidDrawerViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    protected override void OnParametersSet()
    {
        StateHasChanged();
    }

    protected override void OnInitialized()
    {
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
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
