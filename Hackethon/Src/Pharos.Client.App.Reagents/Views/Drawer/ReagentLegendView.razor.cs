using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Reagents.ViewModels;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Legend;
using Pharos.Common.Web.Components.Reagent;

namespace Pharos.Client.App.Reagents.Views.Drawer;
public partial class ReagentLegendView : IDisposable
{
    [Inject]
    private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [Parameter]
    public ReagentLegendViewModel ViewModel { get; set; } = default!;

    private PhLegend<ReagentStatus>? _phLegend;

    protected override void OnInitialized()
    {
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        ViewModel.RemoveLegendSelection += OnResetLegendSelection;
    }

    private void OnResetLegendSelection()
    {
        _phLegend?.ResetSelection();
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        StateHasChanged();
    }

    public void Dispose()
    {
        ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        ViewModel.RemoveLegendSelection -= OnResetLegendSelection;
        GC.SuppressFinalize(this);
    }
}