using System.ComponentModel;
using Pharos.Client.App.Result.Models;

namespace Pharos.Client.App.Result.Views.QualityControl;
public partial class QualityControlChartCardListView : IDisposable
{
    [Inject] private QualityControlViewModel ViewModel { get; set; } = default!;
    [Parameter] public ChartStateService ChartState { get; set; } = default!;
    [Parameter] public int AnalyticalRunId { get; set; }
    [Parameter] public string MethodName { get; set; } = string.Empty;
    [Parameter] public string InstrumentSerial { get; set; } = string.Empty;
    [Parameter] public bool Narrow { get; set; }
    [Parameter] public int NrOfPoints { get; set; } = 5;

    protected override async Task OnInitializedAsync()
    {
        ViewModel.AnalyticalRunId = AnalyticalRunId;
        ViewModel.MethodName = MethodName;
        ViewModel.InstrumentSerial = InstrumentSerial;
        ViewModel.Miniature = true;
        await ViewModel.LoadAsync();

        ChartState.QcChartCount = ViewModel.ChartHolders!.Count;
        ViewModel.PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.ChartHolders))
        {
            ChartState.QcChartCount = ViewModel.ChartHolders!.Count;
            StateHasChanged();
        }
    }
    private string NavigateToQualityControlDetails(string qualityControlId, string testName)
    {
        return ($"{PageRoute.QualityControlDetails.Default}/{AnalyticalRunId}/{qualityControlId}/{testName}");
    }
    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnPropertyChanged;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}