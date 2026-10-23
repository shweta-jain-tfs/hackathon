using System.ComponentModel;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Quality.ViewModels;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Quality.Views.CurveControls;

public partial class CurveControlChartCardListView : IDisposable
{
    [Inject] private CurveControlViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Parameter] public string MethodName { get; set; } = string.Empty;
    [Parameter] public string InstrumentNumber { get; set; } = string.Empty;
    [Parameter] public bool Narrow { get; set; }
    [Parameter] public int LastDays { get; set; } = 30;

    protected override void OnInitialized()
    {
        ViewModel.PropertyChanged += OnPropertyChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!string.IsNullOrEmpty(MethodName) && !string.IsNullOrEmpty(InstrumentNumber))
        {
            ViewModel.MethodName = MethodName;
            ViewModel.InstrumentNumber = InstrumentNumber;
            ViewModel.Miniature = true;
            ViewModel.LastDays = LastDays;
            await ViewModel.LoadAsync();
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is CurveControlViewModel { IsBusy: true })
        {
            // No need to refresh until the vm is not busy
            return;
        }

        if (e.PropertyName == nameof(ViewModel.ChartHolders))
        {
            //ChartState.CcChartCount = ViewModel.ChartHolders.Count;
            StateHasChanged();
        }
    }

    private string NavigateToCurveControlDetails()
    {
        return $"{PageRoute.Quality.Default}/{InstrumentNumber}/{MethodName}/{RouteItemBase.CurveControl}";
    }

    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnPropertyChanged;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}
