namespace Pharos.Common.Web.Components.Charts.ChartBase;

/// <summary>
/// This is a chart point component, used for displaying points in a chart.
/// </summary>
public partial class PhChartPoint
{
    [Parameter] public double X { get; set; }
    [Parameter] public double Y { get; set; }
    [Parameter] public bool Active { get; set; }
    [Parameter] public bool Selected { get; set; }
    [Parameter] public bool Small { get; set; }
    [Parameter] public bool Logarithmic { get; set; }
    [Parameter] public PointState PointState { get; set; }
    [Parameter] public required string ClassName { get; set; }
    [Parameter] public EventCallback<PointBase> ChartPointClicked { get; set; }
    [Parameter] public required PointBase Point { get; set; }

    private async Task OnClickPoint()
    {
        await ChartPointClicked.InvokeAsync(Point);
    }
}

/// Point states needed for drawing different shapes and colors 
public enum PointState
{
    normal,
    warning,
    alert,
    above,
    below,
    fast
}



