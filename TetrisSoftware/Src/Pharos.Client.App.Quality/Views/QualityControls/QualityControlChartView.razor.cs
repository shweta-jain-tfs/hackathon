using Pharos.Client.App.Result.ViewModels;
using Pharos.Common.Web.Components.Charts.ChartBase;

namespace Pharos.Client.App.Quality.Views.QualityControls;

public partial class QualityControlChartView
{
    [Parameter] public List<PhChartLinear.IDrawable>? ChartComponents { get; set; }
    [Parameter] public OuterParameters OuterParameters { get; set; } = default!;
    [Parameter] public List<QualityControlTestResult> TestResults { get; set; } = default!;
    [Parameter] public bool SingleChart { get; set; } = false;
    [Parameter] public bool Interactive { get; set; }
    [Parameter] public bool Miniature { get; set; } = false;
    [Parameter] public int NrOfPoints { get; set; }

    private int _width;
    private int _height;

    protected override void OnInitialized()
    {
        _width = Miniature ? 500 : 806;
        _height = Miniature ? 180 : 800;
    }
}
