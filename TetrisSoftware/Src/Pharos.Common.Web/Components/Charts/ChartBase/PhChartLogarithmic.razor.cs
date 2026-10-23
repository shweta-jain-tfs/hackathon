using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Charts.ChartBase;

/// <summary>
/// This is a chart component for displaying data in a logarithmic chart.
/// </summary>
public partial class PhChartLogarithmic
{
    private const double _minimumLogarithmicValue = 1e-10;

    [Parameter(CaptureUnmatchedValues = true)]
    public required Dictionary<string, object> CapturedAttributes { get; set; }

    /// <summary>
    /// Miniature makes the chart area fill the whole space (ignoring axis and labels)
    /// </summary>
    [Parameter] public bool Miniature { get; set; }

    /// <summary>
    /// Add the content of the chart here.
    /// </summary>
    [Parameter, EditorRequired] public List<IDrawable> Content { get; set; } = new();

    /// <summary>
    /// The width of the chart (must be fixed).
    /// </summary>
    [Parameter, EditorRequired] public int ChartWidth { get; set; }

    /// <summary>
    /// The height of the chart (must be fixed).
    /// </summary>
    [Parameter, EditorRequired] public int ChartHeight { get; set; }

    /// <summary>
    /// The value representing the left end of the displayed range (min x).
    /// </summary>
    [Parameter, EditorRequired] public double DataRangeXMin { get; set; }

    /// <summary>
    /// The value representing the right end of the displayed range (max x).
    /// </summary>
    [Parameter, EditorRequired] public double DataRangeXMax { get; set; }

    /// <summary>
    /// The value representing the lower end of the displayed range (min y).
    /// </summary>
    [Parameter, EditorRequired] public double DataRangeYMin { get; set; }

    /// <summary>
    /// The value representing the upper end of the displayed range (max y).
    /// </summary>
    [Parameter, EditorRequired] public double DataRangeYMax { get; set; }

    private double _viewboxLeft;
    private double _viewboxTop;
    private double _viewboxWidth;
    private double _viewboxHeight;

    private double _internalMarginLeft = 70;
    private double _internalMarginRight = 70;
    private double _internalMarginUpper = 45;
    private double _internalMarginLower = 60;

    private double _scaleX = 1.0; // data values to SVG coordinates
    private double _scaleY = 1.0; // data values to SVG coordinates

    private double _chartInnerWidth = 1.0;
    private double _chartInnerHeight = 1.0;
    private double _logDataRangeXMin;
    private double _logDataRangeXMax;
    private double _logDataRangeYMin;
    private double _logDataRangeYMax;
    private double _horizontalLineX1;
    private double _horizontalLineX1Label;
    private double _horizontalLineX2;
    private double _verticalLineY1;
    private double _verticalLineY2;

    protected string CssClassItems => new CssBuilder("ph-chartbase-logarithmic")
        .AddClass(CapturedAttributes != null && CapturedAttributes.TryGetValue("class", out var value) ? (string)value : "")
        .Build();

    protected override void OnParametersSet()
    {
        if (Miniature)
        {
            _internalMarginLeft = 8;
            _internalMarginRight = 8;
            _internalMarginUpper = 8;
            _internalMarginLower = 8;
        }

        DataRangeXMin = Math.Max(_minimumLogarithmicValue, DataRangeXMin); // Make sure not to use non-positive numbers for log
        DataRangeYMin = Math.Max(_minimumLogarithmicValue, DataRangeYMin);

        _viewboxLeft = -_internalMarginLeft;
        _viewboxTop = -ChartHeight + _internalMarginLower;
        _viewboxWidth = ChartWidth;
        _viewboxHeight = ChartHeight;

        _chartInnerWidth = ChartWidth - _internalMarginLeft - _internalMarginRight;
        _chartInnerHeight = ChartHeight - _internalMarginUpper - _internalMarginLower;

        var verticalLineValues = Content
            .OfType<VerticalLine>()
            .Select(line => line.Value)
            .Where(value => value > 0)
            .ToList();

        var point = Content
            .OfType<Curve>()
            .SelectMany(line => line.PointList.Where(x => x.X > DataRangeXMin).Select(p => p.X))
            .ToList();

        var lowestCurvePoint = point.Count > 0 ? point.Min() : DataRangeXMin;

        if (!Miniature)
        {
            Content.Insert(0, new VerticalLine
            {
                Value = lowestCurvePoint,
                Label = false
            });
        }

        var lowestVerticalLineValue = verticalLineValues.Count > 0 ? verticalLineValues.Min() : DataRangeXMin;
        lowestVerticalLineValue = Math.Min(lowestCurvePoint, lowestVerticalLineValue);

        var highestVerticalLineValue = verticalLineValues.Count > 0 ? verticalLineValues.Max() : DataRangeXMax;

        var effectiveDataRangeXMin = Math.Max(_minimumLogarithmicValue, lowestVerticalLineValue);
        var effectiveDataRangeXMax = Math.Max(Math.Max(highestVerticalLineValue, DataRangeXMax), effectiveDataRangeXMin * 10);

        _logDataRangeXMin = Math.Log10(effectiveDataRangeXMin);
        _logDataRangeXMax = Math.Log10(effectiveDataRangeXMax);
        _logDataRangeYMin = Math.Log10(DataRangeYMin);
        _logDataRangeYMax = Math.Log10(DataRangeYMax);

        _scaleX = _chartInnerWidth / (_logDataRangeXMax - _logDataRangeXMin);
        _scaleY = _chartInnerHeight / (_logDataRangeYMax - _logDataRangeYMin);

        var horizontalLineValues = Content.OfType<HorizontalLine>().Select(line => line.Value).ToList();
        var lowestHorizontalLineValue = horizontalLineValues.Count > 0 ? horizontalLineValues.Min() : DataRangeYMin;
        var highestHorizontalLineValue = horizontalLineValues.Count > 0 ? horizontalLineValues.Max() : DataRangeYMax;

        _horizontalLineX1 = CalculateX(lowestVerticalLineValue);
        _horizontalLineX1Label = _horizontalLineX1 - 10;
        _horizontalLineX2 = CalculateX(highestVerticalLineValue);
        _verticalLineY1 = CalculateY(lowestHorizontalLineValue);
        _verticalLineY2 = CalculateY(highestHorizontalLineValue);

        base.OnParametersSet();
    }

    private double CalculateX(double value)
    {
        return (Math.Log10(Math.Max(_minimumLogarithmicValue, value)) - _logDataRangeXMin) * _scaleX;
    }

    private double CalculateY(double value)
    {
        return (Math.Log10(Math.Max(_minimumLogarithmicValue, value)) - _logDataRangeYMin) * _scaleY;
    }

    public class Point : PointBase
    {
        public double Y { get; set; }
        public double X { get; set; }
    }

    public interface IDrawable
    {
    }

    public class HorizontalLine : IDrawable
    {
        public double Value { get; set; }
        public bool Dashed { get; set; }
        public bool Label { get; set; }
    }

    public class VerticalLine : IDrawable
    {
        public double Value { get; set; }
        public bool Dashed { get; set; }
        public bool Label { get; set; }
    }

    public class Points : IDrawable
    {
        public List<Point> PointList { get; set; } = new();
        public bool Small { get; set; } // draw small points (as in the thumbnails)
    }

    public class Curve : IDrawable
    {
        public List<Point> PointList { get; set; } = new();
        public bool Selected { get; set; }
        public bool Active { get; set; }
    }

    public class HorizontalAxis : IDrawable
    {
        public string? Label { get; set; }
        public bool DrawAxisLine { get; set; }
    }

    public class VerticalAxis : IDrawable
    {
        public string? Label { get; set; }
        public bool DrawAxisLine { get; set; }
    }
}