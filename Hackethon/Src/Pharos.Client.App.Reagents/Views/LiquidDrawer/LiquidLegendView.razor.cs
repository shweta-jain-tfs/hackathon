using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Reagents.ViewModels.LiquidDrawer;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Legend;
using Pharos.Common.Web.Components.Reagent;

namespace Pharos.Client.App.Reagents.Views.LiquidDrawer;

public partial class LiquidLegendView : ComponentBase, IDisposable
{
    [Inject]
    private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [Parameter]
    public LiquidLegendViewModel ViewModel { get; set; } = default!;

    private PhLegend<LiquidBottleStatus>? _phLegend;

    protected override void OnInitialized()
    {
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        ViewModel.ResetLegendSelectionRequested += OnResetLegendSelection;
    }

    private string GetStatusText(LiquidBottleStatus status)
    {
        return status switch
        {
            LiquidBottleStatus.InUse => Loc["Reagent_ReagentPositionStatus_InUse"],
            LiquidBottleStatus.Disabled => "Disabled",
            _ => status.ToString()
        };
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