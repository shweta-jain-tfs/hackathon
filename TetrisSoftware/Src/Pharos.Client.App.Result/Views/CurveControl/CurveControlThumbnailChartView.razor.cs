using Pharos.Common.Web.Components.Charts.ChartBase;

namespace Pharos.Client.App.Result.Views.CurveControl;

public partial class CurveControlThumbnailChartView 
{
    [Parameter] public List<PhChartLinear.IDrawable>? ChartComponents { get; set; }
    [Parameter] public OuterParameters OuterParameters { get; set; } = default!;
    [Parameter] public List<CurveControlTestResult> CurveControlTestResults { get; set; } = default!;
    [Parameter] public bool SingleChart { get; set; } = false;
    [Parameter] public bool Interactive { get; set; }
    [Parameter] public bool Miniature { get; set; } = false;
    [Parameter] public int NrOfPoints { get; set; }

    private int _width;
    private int _height;

    protected override void OnInitialized()
    {
        _width = GetWidth();
        _height = Miniature ? 140 : 525;
    }
    private int GetWidth()
    {
        if (Miniature)
        {
            return 192;
        }
        return SingleChart ? 1584 : 758;
    }
}