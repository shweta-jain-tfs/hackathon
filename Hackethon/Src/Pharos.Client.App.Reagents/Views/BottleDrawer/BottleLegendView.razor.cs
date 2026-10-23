using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Reagents.ViewModels.BottleDrawer;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Legend;
using Pharos.Common.Web.Components.Reagent;

namespace Pharos.Client.App.Reagents.Views.BottleDrawer;

public partial class BottleLegendView : ComponentBase, IDisposable
{
    [Inject]
    private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [Parameter]
    public BottleLegendViewModel ViewModel { get; set; } = default!;

    private PhLegend<BottleStatus>? _phLegend;

    protected override void OnInitialized()
    {
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        ViewModel.ResetLegendSelectionRequested += OnResetLegendSelection;
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        StateHasChanged();
    }

    private void OnResetLegendSelection()
    {
        _phLegend?.ResetSelection();
    }

    public void Dispose()
    {
        ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        ViewModel.ResetLegendSelectionRequested -= OnResetLegendSelection;
        GC.SuppressFinalize(this);
    }
}