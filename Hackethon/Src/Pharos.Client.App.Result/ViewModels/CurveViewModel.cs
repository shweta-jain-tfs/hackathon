using Microsoft.Extensions.Localization;
using Pharos.Client.App.Result.Models;
using Pharos.Client.App.Result.Views.CalibratorCurve;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Result.ViewModels;

public class CurveViewModel : ResultsViewModel, IDisposable
{
    private readonly ICurveDataSource _dataSource;
    private readonly ITestResultChartNotificationProvider _chartNotificationProvider;
    private readonly IExceptionHandlerService _exceptionHandlerService;

    public CurvePresentation? Data { get; set; }
    public bool IsLoaded { get; set; }
    public bool IsWaitingForOrderedCurve => Data?.CurveState == CalculationStateStatus.WaitingForCurve;
    public bool IsWaitingForOrderedCC => Data?.CurveState == CalculationStateStatus.WaitingForCurveControl;
    public bool HasCurveDetails => Data is not null && !IsWaitingForOrderedCurve;

    public CurveViewModel(ICurveDataSource dataSource, ITestResultChartNotificationProvider chartNotificationProvider, IExceptionHandlerService exceptionHandlerService)
    {
        _dataSource = dataSource;
        _chartNotificationProvider = chartNotificationProvider;
        _chartNotificationProvider.CalibratorCurveChartUpdated += OnCalibratorCurveChartUpdated;
        _exceptionHandlerService = exceptionHandlerService;
    }

    private async void OnCalibratorCurveChartUpdated(object? sender, (string, string) e)
    {
        if (e.Item2 == MethodName && e.Item1 == InstrumentSerial)
        {
            await DoLoadAsync();
            OnPropertyChanged(null);
        }
    }

    protected override async Task DoLoadAsync()
    {
        Data = await _dataSource.GetByRunAsync(AnalyticalRunId);
        IsLoaded = true;
    }

    public void Dispose()
    {
        _chartNotificationProvider.CalibratorCurveChartUpdated -= OnCalibratorCurveChartUpdated;
        GC.SuppressFinalize(this);
    }
    
    public async Task UpdateCalibratorResult(CalibratorTestResult result)
    {
        await _dataSource.UpdateOperatorComment(result);
    }

    public async Task ReRunCurve()
    {
        await _dataSource.ReRunCurve(AnalyticalRunId);
    }

    public async Task<ICollection<AvailableCurve>> GetAvailableCurves()
    {
        return await _dataSource.GetAvailableCurves(AnalyticalRunId);
    }

    public async Task ChangeCurveForAnalyticalRun(int selectedCurveId)
    {
        await _dataSource.ChangeCurveForAnalyticalRun(selectedCurveId, AnalyticalRunId);
    }

    public async Task AcceptCurve()
    {
        await _dataSource.AcceptCurve(AnalyticalRunId);
    }

    public async Task<bool> OpenSelectCurveModal(IModalService modalService, IStringLocalizer localizer)
    {
        var availableCurves = await GetAvailableCurves();

        // Remove currently selected curve.
        var selectableCurves = Data is null
            ? availableCurves
            : availableCurves.Where(curve => curve.Id != Data.Id).ToList();

        var hasSelectableCurves = selectableCurves.Count > 0;
        var modalSize = hasSelectableCurves ? ModalSize.Medium : ModalSize.Small;
        var modalStatus = hasSelectableCurves ? ModalStatus.Default : ModalStatus.Info;

        var modalParameters = new ModalParameters
        {
            { nameof(SelectCurveModalView.AvailableCurves), selectableCurves }
        };

        var modal = modalService.Show<SelectCurveModalView>(new ModalOptions
        {
            Title = localizer["SelectCurveModalView_Title"],
            ModalHeight = modalSize,
            ModalWidth = modalSize,
            Status = modalStatus
        }, modalParameters);

        var result = await modal.Result;
        if (result.Canceled)
        {
            return false;
        }

        try
        {
            await ChangeCurveForAnalyticalRun((int)result.Data!);
        }
        catch (Exception ex)
        {
            // Keep page interactive on unexpected error in this action.
            _exceptionHandlerService.ShowExceptionMessage(ex);
        }

        return true;
    }
}