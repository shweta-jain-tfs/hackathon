using System.ComponentModel;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Sample.ViewModels;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Drawer;
using Pharos.Common.Web.Components.Legend;

namespace Pharos.Client.App.Sample.Views;
public partial class SampleLegendView : ComponentBase, IDisposable
{
    [Inject]
    private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [Parameter]
    public SampleLegendViewModel ViewModel { get; set; } = default!;

    private PhLegend<TubeStatus>? _phLegend;
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
