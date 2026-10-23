namespace Pharos.Client.App.Result.Models;

public class ChartStateService
{
    public event Action? OnChange;

    public int TotalCharts
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                NotifyStateChanged();
            }
        }
    }

    public int QcChartCount
    {
        get;
        set
        {
            field = value;
            UpdateTotalCharts();
        }
    }

    public int CcChartCount
    {
        get;
        set
        {
            field = value;
            UpdateTotalCharts();
        }
    }

    private void UpdateTotalCharts()
    {
        // Update TotalCharts 
        TotalCharts = QcChartCount + CcChartCount;
    }

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}