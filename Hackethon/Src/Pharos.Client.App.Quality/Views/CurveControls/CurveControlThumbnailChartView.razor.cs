using Pharos.Client.App.Result.ViewModels;
using Pharos.Common.Web.Components.Charts.ChartBase;

namespace Pharos.Client.App.Quality.Views.CurveControls;

public partial class CurveControlThumbnailChartView 
{
    [Parameter] public List<PhChartLinear.IDrawable>? ChartComponents { get; set; }
    [Parameter] public OuterParameters OuterParameters { get; set; } = default!;
    [Parameter] public List<CurveControlTestResult> CurveControlTestResults { get; set; } = default!;
    [Parameter] public bool SingleChart { get; set; } = false;
    [Parameter] public bool Interactive { get; set; }
    [Parameter] public bool Miniature { get; set; } = false;

    private int _width;
    private int _height;

    protected override void OnInitialized()
    {
        _width = Miniature ? 500 : SingleChart ? 1800 : 900;
        _height = Miniature ? 180 : 400;
    }
}