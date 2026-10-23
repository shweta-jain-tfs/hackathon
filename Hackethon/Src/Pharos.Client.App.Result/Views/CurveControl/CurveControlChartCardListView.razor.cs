using System.ComponentModel;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Result.Models;

namespace Pharos.Client.App.Result.Views.CurveControl;

public partial class CurveControlChartCardListView : IDisposable
{
    [Inject] private CurveControlChartViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
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
        ChartState.CcChartCount = ViewModel.ChartHolders.Count;
        ViewModel.PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.ChartHolders))
        {
            ChartState.CcChartCount = ViewModel.ChartHolders.Count;
            StateHasChanged();
        }
    }

    private string NavigateToCurveControlDetails()
    {
        return $"{PageRoute.CurveControlDetails.Default}/{AnalyticalRunId}";
    }

    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnPropertyChanged;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }

    private static StatusType GetStatusSymbol(ChartHolder chart)
    {
        var isAccepted = chart.CurveControlTotalJudgment == CurveControlJudgmentResult.Flagged
                         && chart.FrozenAt != null;
        if (isAccepted)
        {
            return StatusType.Success;
        }

        return chart.CurveControlTotalJudgment switch
        {
            CurveControlJudgmentResult.NotJudged => StatusType.None,
            CurveControlJudgmentResult.Flagged => StatusType.Warning,
            CurveControlJudgmentResult.NotOK or CurveControlJudgmentResult.ReRunNotOK => StatusType.Alarm,
            _ => chart.Status
        };
    }
}