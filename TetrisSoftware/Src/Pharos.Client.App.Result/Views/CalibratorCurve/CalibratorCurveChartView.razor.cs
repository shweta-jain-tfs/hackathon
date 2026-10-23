using Microsoft.Extensions.Localization;

namespace Pharos.Client.App.Result.Views.CalibratorCurve;

public partial class CalibratorCurveChartView : IDisposable
{
    [Parameter] public int AnalyticalRunId { get; set; }
    [Inject] private CurveViewModel CurveViewModel { get; set; } = default!;
    [Parameter] public bool Narrow { get; set; }
    [Parameter] public string MethodName { get; set; } = string.Empty;
    [Parameter] public string InstrumentSerial { get; set; } = string.Empty;
    [Parameter] public EventCallback OnSelectCurve { get; set; }
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        CurveViewModel.AnalyticalRunId = AnalyticalRunId;
        CurveViewModel.MethodName = MethodName;
        CurveViewModel.InstrumentSerial = InstrumentSerial;

        await CurveViewModel.LoadAsync();
        CurveViewModel.PropertyChanged += OnPropertyChanged;
        CurveViewModel.IsLoaded = true;
    }

    private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        StateHasChanged();
    }

    private string NavigateToCalibratorDetails()
    {
        return ($"{PageRoute.CalibratorDetails.Default}/{AnalyticalRunId}");
    }
    private StatusType GetHealthStatus(CurveJudgment curveJudgment)
    {
        if (CurveViewModel.Data is { CurveState: CalculationStateStatus.Frozen })
        {
            return StatusType.Success;
        }
        if (CurveViewModel.Data is { CurveControlDisabledFlag: true })
        {
            return StatusType.Disconnected;
        }

        return curveJudgment switch
        {
            CurveJudgment.OK => StatusType.Success,
            CurveJudgment.Flagged => StatusType.Warning,
            CurveJudgment.NotOK => StatusType.Alarm,
            _ => StatusType.Alarm
        };
    }

    private async Task SelectCurveOpen()
    {
        if (OnSelectCurve.HasDelegate)
        {
            await OnSelectCurve.InvokeAsync();
        }
    }

    public void Dispose()
    {
        CurveViewModel.PropertyChanged -= OnPropertyChanged;
        CurveViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}
