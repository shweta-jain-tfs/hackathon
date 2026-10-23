using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Reagents.ViewModels.SolidWasteDrawer;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Reagents.Views.SolidWasteDrawer;

public partial class SolidWasteLegendView : ComponentBase, IDisposable
{
    [Inject]
    private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [Parameter]
    public SolidWasteLegendViewModel ViewModel { get; set; } = default!;

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
        GC.SuppressFinalize(this);
    }
}
