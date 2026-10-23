using Microsoft.Extensions.Localization;
using Pharos.Client.App.Quality.ViewModels;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Quality.Views.Calibrators;

public partial class CalibratorCurveChartView : IDisposable
{
    [Parameter] public string MethodName { get; set; } = string.Empty;
    [Parameter] public string InstrumentNumber { get; set; } = string.Empty;
    [Inject] private CalibratorCurveViewModel CalibratorCurveViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    protected override void OnInitialized()
    {
        CalibratorCurveViewModel.PropertyChanged += OnPropertyChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!string.IsNullOrEmpty(MethodName) && !string.IsNullOrEmpty(InstrumentNumber))
        {
            CalibratorCurveViewModel.MethodName = MethodName;
            CalibratorCurveViewModel.InstrumentNumber = InstrumentNumber;

            await CalibratorCurveViewModel.LoadAsync();
            CalibratorCurveViewModel.IsLoaded = true;
        }
    }

    private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is CalibratorCurveViewModel { IsBusy: true })
        {
            // No need to refresh until the vm is not busy
            return;
        }

        StateHasChanged();
    }

    public void Dispose()
    {
        CalibratorCurveViewModel.PropertyChanged -= OnPropertyChanged;
        GC.SuppressFinalize(this);
    }

    private string NavigateToCalibratorDetails()
    {
        return $"{PageRoute.Quality.Default}/{InstrumentNumber}/{MethodName}/{RouteItemBase.Calibrator}";
    }
}
