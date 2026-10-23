using Microsoft.JSInterop;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Charts.ChartBase;

/// <summary>
/// This is a chart component for displaying linear data on a time scale.
/// </summary>
public partial class PhChartLinear
{
    [Inject] protected IJSRuntime JS { get; set; } = default!;

    [Parameter(CaptureUnmatchedValues = true)]
    public required Dictionary<string, object> CapturedAttributes { get; set; }

    /// <summary>
    /// Allow user interactions such as zoom and pan.
    /// </summary>
    [Parameter] public bool Interactive { get; set; }

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
    /// The time representing the left end of the displayed range (min x).
    /// </summary>
    [Parameter, EditorRequired] public DateTimeOffset DataRangeXMin { get; set; }

    /// <summary>
    /// The time representing the right end of the displayed range (max x).
    /// </summary>
    [Parameter, EditorRequired] public DateTimeOffset DataRangeXMax { get; set; }

    /// <summary>
    /// The value representing the lower end of the displayed range (min y).
    /// </summary>
    [Parameter, EditorRequired] public double DataRangeYMin { get; set; }

    /// <summary>
    /// The value representing the upper end of the displayed range (max y).
    /// </summary>
    [Parameter, EditorRequired] public double DataRangeYMax { get; set; }

    /// <summary>
    /// Set callback for when a point is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<PointBase> PointClicked { get; set; }

    private double DisplayRange => _visibleRangeMax - _visibleRangeMin;
    private DateTimeOffset FirstPointTime => Content.OfType<Points>().First().PointList.First().Value[0].X;
    private DateTimeOffset LastPointTime => Content.OfType<Points>().First().PointList.Last().Value[0].X;
    private double FirstPointRatio => TimeToRatio(FirstPointTime);
    private double LastPointRatio => TimeToRatio(LastPointTime);

    private DateTimeOffset _oldDataRangeXMin = DateTimeOffset.MinValue;
    private DateTimeOffset _oldDataRangeXMax = DateTimeOffset.MinValue;
    private double _visibleRangeMin;
    private double _visibleRangeMax = 1;

    private double _viewboxLeft;
    private double _viewboxTop;
    private double _viewboxWidth;
    private double _viewboxHeight;

    private double _internalMarginLeft = 100;
    private double _internalMarginRight = 70;
    private double _internalMarginUpper = 45;
    private double _internalMarginLower = 90;

    private double _scaleX = 1.0; // days to coordinates
    private double _scaleY = 1.0; // data values to coordinates

    private double _chartInnerWidth = 1.0;
    private double _chartInnerHeight = 1.0;
    private double _dataMarginX;

    private double _startX1;
    private double _startX2;
    private double _startMid;

    protected string CssClassItems => new CssBuilder("ph-chartbase-linear")
        .AddClass(CapturedAttributes != null && CapturedAttributes.TryGetValue("class", out var value) ? (string)value : "")
        .Build();

    // used for calculating local touch coordinates
    private ElementCoordinates? _touchAreaCoordinates;

    // Needed for retrieving chart coordinates
    public class ElementCoordinates
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class Points : IDrawable
    {
        public SortedList<DateTimeOffset, List<Point>> PointList { get; set; } = new();
        public bool FastRendering { get; set; } // draw points with less precise circles - fast and ugly
        public bool Small { get; set; } // draw small points (as in the thumbnails)
    }

    public class Point : PointBase
    {
        public DateTimeOffset X { get; set; }
        public double Y { get; set; }
        public bool Active { get; set; } // an active point is displayed with color
    }

    public class Region
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }

        public double UpperWarning { get; set; }
        public double UpperAlert { get; set; }
        public double LowerWarning { get; set; }
        public double LowerAlert { get; set; }
    }

    public interface IDrawable
    {
    }

    public class HorizontalLine : IDrawable
    {
        public double Value { get; set; }
        public bool Dashed { get; set; }
        public bool Label { get; set; }
        public bool DrawTick { get; set; }
        public DateTimeOffset? StartX { get; set; }
        public DateTimeOffset? EndX { get; set; }
    }

    public class VerticalLine : IDrawable
    {
        public DateTimeOffset Value { get; set; }
        public bool Dashed { get; set; }
        public bool Label { get; set; }
    }

    public class Border : IDrawable
    {
    }

    public class Regions : IDrawable
    {
        public List<Region> RegionList { get; set; } = new();
    }

    public class HorizontalAxis : IDrawable
    {
        public string? Label { get; set; }
        public bool DrawAxisLine { get; set; }
    }

    public class VerticalAxis : IDrawable
    {
        public bool DrawTicks { get; set; }
        public double TickDistance { get; set; }
        public bool DrawValues { get; set; }
        public int ValueEveryNTicks { get; set; }
        public string? Label { get; set; }
        public bool DrawAxisLine { get; set; }
    }

    private bool NoPoints()
    {
        return !Content.OfType<Points>().Any()
            || Content.OfType<Points>().First().PointList.Count == 0;
    }

    private bool SinglePoint()
    {
        return Content.OfType<Points>().Any()
            && Content.OfType<Points>().First().PointList.Count == 1;
    }

    private bool NoRegions()
    {
        return !Content.OfType<Regions>().Any()
            || Content.OfType<Regions>().First().RegionList.Count == 0;
    }

    private double TimeToRatio(DateTimeOffset time)
    {
        if (NoPoints())
        {
            var regionList = Content.OfType<Regions>().First().RegionList;

            var startTime = regionList.First().Start;
            var endTime = regionList.Last().End;

            return (time - startTime).TotalMilliseconds / (endTime - startTime).TotalMilliseconds;
        }
        else
        {
            var pointList = Content.OfType<Points>().First().PointList;

            for (var i = 0; i < pointList.Count; i++)
            {
                if (time == pointList.Values[i][0].X)
                {
                    return ((double)i / pointList.Count) + (0.5 / pointList.Count);
                }

                if (time > pointList.Values[i][0].X)
                {
                    if (i == pointList.Count - 1)
                    {
                        return 1.0;
                    }
                    continue;
                }

                if (i == 0)
                {
                    return 0.0;
                }

                return (0.5 * (i - 1 + i) / pointList.Count) + (0.5 / pointList.Count);
            }
        }
        return 0.0; // should never happen
    }

    /// <summary>
    /// Get the ratio for any given point index. Points are evenly distributed horizontally in the range [0, 1].
    /// </summary>
    private double PointIndexToRatio(int i)
    {
        var pointListCount = Content.OfType<Points>().First().PointList.Count;

        return ((double)i / pointListCount) + (0.5 / pointListCount);
    }

    /// <summary>
    /// Get the coordinates of the chart's touch area in client space. Used for calculating local touch and click points
    /// </summary>
    private async Task FetchTouchAreaCoordinates()
    {
        if (_touchAreaCoordinates == null)
        {
            _touchAreaCoordinates = await JS.InvokeAsync<ElementCoordinates>("getElementCoordinates", "ChartTouchArea");
        }
    }

    private void UpdateScaleX()
    {
        _scaleX = _chartInnerWidth / DisplayRange;
    }

    protected override void OnInitialized()
    {
        if (Miniature)
        {
            _internalMarginLeft = 0;
            _internalMarginRight = 0;
            _internalMarginUpper = 0;
            _internalMarginLower = 0;
        }

        _viewboxLeft = -_internalMarginLeft;
        _viewboxTop = -ChartHeight + _internalMarginLower;
        _viewboxWidth = ChartWidth;
        _viewboxHeight = ChartHeight;

        _chartInnerWidth = ChartWidth - _internalMarginLeft - _internalMarginRight;
        _chartInnerHeight = ChartHeight - _internalMarginUpper - _internalMarginLower;

        _dataMarginX = 0.25 * _chartInnerWidth;

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        if (NoPoints() && NoRegions())
        {
            base.OnInitialized();
            return;
        }

        // Update viewbox if the range has changed
        if (_oldDataRangeXMin != DataRangeXMin)
        {
            _visibleRangeMin = TimeToRatio(DataRangeXMin);
            _oldDataRangeXMin = DataRangeXMin;
        }

        if (_oldDataRangeXMax != DataRangeXMax)
        {
            _visibleRangeMax = TimeToRatio(DataRangeXMax);
            _oldDataRangeXMax = DataRangeXMax;
        }

        UpdateScaleX();
        _scaleY = _chartInnerHeight / (DataRangeYMax - DataRangeYMin);

        base.OnParametersSet();
    }

    private async Task OnClickPoint(PointBase p)
    {
        if (Interactive && PointClicked.HasDelegate)
        {
            await PointClicked.InvokeAsync(p);
        }
    }

    private void OnTouchStart(TouchEventArgs e)
    {
        if (!Interactive)
        {
            return;
        }

        _ = FetchTouchAreaCoordinates();

        _startX1 = e.TargetTouches[0].ClientX;

        if (e.TargetTouches.Length > 1)
        {
            _startX2 = e.TargetTouches[1].ClientX;
            _startMid = (_startX1 + _startX2) / 2;
        }
    }

    private void OnTouchEnd(TouchEventArgs e)
    {
        if (e.TargetTouches.Length == 1)
        {
            _startX1 = e.TargetTouches[0].ClientX; // prevent panning when releasing 2nd digit first
        }
    }

    /// <summary>
    /// Pan the chart by a given delta in pixels.
    /// </summary>
    private void Pan(double deltaX)
    {
        var coordToRatio = DisplayRange / _chartInnerWidth;

        var targetMin = _visibleRangeMin - (deltaX * coordToRatio);
        var targetMax = _visibleRangeMax - (deltaX * coordToRatio);

        TrySetVisibleRange(targetMin, targetMax, true);
    }

    /// <summary>
    /// Zoom the chart around a given point by a given factor.
    /// This could mean panning also when using two digits on a touch screen.
    /// </summary>
    /// <param name="zoomPointClientX">X coordinate for the center of zoom, client coordinate space</param>
    /// <param name="zoomFactor">Ammount of zoom. Above 1 means zooming out. Below 1 means zooming in.</param>
    /// <param name="preventPan">Prevent panning during zoom (used with the mouse wheel).</param>
    private void Zoom(double zoomPointClientX, double zoomFactor, bool preventPan = false)
    {
        if (_startMid == 0)
        {
            _startMid = zoomPointClientX;
        }

        if (_touchAreaCoordinates != null && zoomFactor > 0)
        {
            var zoomPointRect = zoomPointClientX - _touchAreaCoordinates.X;
            var coordToRatio = DisplayRange / _chartInnerWidth;
            var newMidpointRatio = _visibleRangeMin + (zoomPointRect * coordToRatio);
            var midpointDelta = preventPan ? 0 : zoomPointClientX - _startMid;
            var midpointDeltaRatio = midpointDelta * coordToRatio;

            var oldMinMidpointDist = newMidpointRatio - _visibleRangeMin;
            var oldMaxMidpointDist = _visibleRangeMax - newMidpointRatio;

            var targetMin = newMidpointRatio - (oldMinMidpointDist * zoomFactor) - midpointDeltaRatio;
            var targetMax = newMidpointRatio + (oldMaxMidpointDist * zoomFactor) - midpointDeltaRatio;

            TrySetVisibleRange(targetMin, targetMax);
            UpdateScaleX();
        }
    }

    /// <summary>
    /// Set the visible range of the chart, with safety checks that prevents extreme zooming or panning.
    /// The visual range relates to the entire range of points [0, 1]. 
    /// </summary>
    /// <param name="targetMin">the minimun (left) value for the visible range</param>
    /// <param name="targetMax">the maximum (right) value for the visible range</param>
    /// <param name="preventZoom">prevent zooming when correcting the visible range</param>
    private void TrySetVisibleRange(double targetMin, double targetMax, bool preventZoom = false)
    {
        var oldMin = _visibleRangeMin;
        var oldMax = _visibleRangeMax;

        var coordToRatio = DisplayRange / _chartInnerWidth; // represents zoom level before setting the new range

        // Set the range
        _visibleRangeMin = targetMin;
        _visibleRangeMax = targetMax;

        // prevent zooming in too much
        var zoomFactor = Math.Abs(targetMax - targetMin) / Math.Abs(oldMax - oldMin);
        if (zoomFactor < 1 && _visibleRangeMax - _visibleRangeMin < 1.0 / Content.OfType<Points>().First().PointList.Count)
        {
            var oldMidpointRange = (oldMin + oldMax) * 0.5;
            var targetMidpointRange = (targetMin + targetMax) * 0.5;
            var diff = targetMidpointRange - oldMidpointRange;

            // just pan
            _visibleRangeMin = oldMin + diff;
            _visibleRangeMax = oldMax + diff;
        }

        // prevent pan & zoom outside range
        var leftLimit = FirstPointRatio - (coordToRatio * _dataMarginX);
        var rightLimit = LastPointRatio + (coordToRatio * _dataMarginX);
        if (preventZoom)
        {
            // If prevent zooming, do nothing. Just revert to initial values.
            if (_visibleRangeMin < leftLimit || _visibleRangeMax > rightLimit)
            {
                _visibleRangeMin = oldMin;
                _visibleRangeMax = oldMax;
            }
        }
        else
        {
            // Correction by zoom is allowed
            _visibleRangeMin = Math.Max(_visibleRangeMin, leftLimit);
            _visibleRangeMax = Math.Min(_visibleRangeMax, rightLimit);
        }

        // prevent extreme changes during pinch zoom by reverting to initial values.
        if (_visibleRangeMin >= _visibleRangeMax || Double.IsInfinity(coordToRatio) || _visibleRangeMax < 0 || _visibleRangeMin > 1)
        {
            _visibleRangeMin = oldMin;
            _visibleRangeMax = oldMax;
        }
    }

    /// <summary>
    /// Pan and zoom on a touch screen
    /// </summary>
    private void OnTouchMove(TouchEventArgs e)
    {
        if (!Interactive || NoPoints() || SinglePoint())
        {
            return;
        }

        if (e.TargetTouches.Length == 1) // single digit - pan
        {
            var currentX = e.TargetTouches[0].ClientX;
            var deltaX = currentX - _startX1;

            Pan(deltaX);

            _startX1 = currentX;
        }
        else if (e.TargetTouches.Length > 1) // multiple digits - pan and zoom
        {
            var newX1Client = e.TargetTouches[0].ClientX;
            var newX2Client = e.TargetTouches[1].ClientX;
            var zoomPointClient = (newX1Client + newX2Client) / 2;
            var zoomFactor = Math.Abs(_startX2 - _startX1) / Math.Abs(newX2Client - newX1Client);

            Zoom(zoomPointClient, zoomFactor);

            _startX1 = newX1Client;
            _startX2 = newX2Client;
            _startMid = (_startX1 + _startX2) / 2;
        }
    }

    /// <summary>
    /// Pan with left mouse button.
    /// </summary>
    private void OnMouseMove(MouseEventArgs e)
    {
        if (!Interactive || NoPoints() || SinglePoint() || e.Buttons != 1)
        {
            return;
        }

        _ = FetchTouchAreaCoordinates();

        Pan(e.MovementX);
    }

    /// <summary>
    /// Zoom with the mouse wheel
    /// </summary>
    private void OnMouseWheel(WheelEventArgs e)
    {
        if (!Interactive || NoPoints() || SinglePoint())
        {
            return;
        }

        _ = FetchTouchAreaCoordinates();

        var zoomPointClient = e.ClientX;
        var zoomFactor = 1 + (e.DeltaY * 0.001);
        Zoom(zoomPointClient, zoomFactor, true);
    }
}