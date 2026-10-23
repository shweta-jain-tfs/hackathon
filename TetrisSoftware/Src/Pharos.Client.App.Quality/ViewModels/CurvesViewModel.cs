using Pharos.Client.App.Quality.Models;
using Pharos.Client.Common.ViewModels;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.Quality.ViewModels;

public class CurvesViewModel : VirtualGridViewModel<CurvePresentation>
{
    private readonly ICurvesDataSource _dataSource;
    public bool IsLoaded { get; set; }

    public List<CalibratorTestResult> CalibratorResults { get; set; } = [];
    public CalibratorCurveViewModel CalibratorCurveViewModel { get; }

    public string InstrumentNumber
    {
        get => _dataSource.InstrumentNumber;
        set => _dataSource.InstrumentNumber = value;
    }
    public string MethodName
    {
        get => _dataSource.MethodName;
        set => _dataSource.MethodName = value;
    }

    public CurvesViewModel(ICurvesDataSource dataSource, CalibratorCurveViewModel calibratorCurveViewModel, IExceptionHandlerService exceptionHandler) : base(dataSource, exceptionHandler)
    {
        _dataSource = dataSource;
        _dataSource.PropertyChanged += DataSource_PropertyChanged;
        CalibratorCurveViewModel = calibratorCurveViewModel;
    }

    private void SetSelection(CurvePresentation? selectedCurve)
    {
        if (_dataSource.Data == null)
        {
            return;
        }

        if (_dataSource.Data.Count == 1)
        {
            _dataSource.Data.First().SelectedCurve = true;
        }
        else
        {
            foreach (var curve in _dataSource.Data)
            {
                if (selectedCurve != null && selectedCurve.Id == curve.Id)
                {
                    curve.SelectedCurve = true;
                }
                else
                {
                    curve.SelectedCurve = false;
                }
            }
        }
    }

    private void DataSource_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (CalibratorCurveViewModel.Data == null)
        {
            SetSelection(null);
            RefreshCurves();
        }
    }

    private void RefreshCurves()
    {
        CalibratorCurveViewModel.Data = _dataSource.Data;
        CalibratorCurveViewModel.IsLoaded = true;
        OnViewRefreshNeeded();
        IsLoaded = true;
    }

    public void OnHighlightedItemChanged(CurvePresentation? selectedCurve)
    {
        if (selectedCurve == null)
        {
            CalibratorResults.Clear();
        }
        else
        {
            CalibratorResults = selectedCurve.CalibratorTestResults.ToList();
        }
        SetSelection(selectedCurve);
        RefreshCurves();
    }
}