using System.ComponentModel;
using Pharos.Client.App.Result.Models;
using Pharos.Common.Web.Components.Charts.ChartBase;

namespace Pharos.Client.App.Result.Views.CalibratorCurve;

public partial class CalibratorCurveView
{
    [Parameter] public bool Miniature { get; set; } = false;
    [Parameter] public CurveViewModel ViewModel { get; set; } = default!;

    private List<PhChartLogarithmic.IDrawable>? _content;
    private LogDiagramAxis? _axis;
    private int _width;
    private int _height;

    protected override Task OnInitializedAsync()
    {
        var graphInfo = GetCalibrationCurveChartContent(Miniature);

        if (graphInfo != null)
        {
            _axis = graphInfo.Value.axis;
            _content = graphInfo.Value.content;
            _width = Miniature ? 192 : 806;
            _height = Miniature ? 140 : 720;
        }

        ViewModel.PropertyChanged += OnPropertyChanged;
        return base.OnInitializedAsync();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var newContent = GetCalibrationCurveChartContent(Miniature);

        if (newContent != null)
        {
            _axis = newContent.Value.axis;
            _content = newContent.Value.content;
        }
        else
        {
            _axis = null;
            _content = null;
        }

        StateHasChanged();
    }

    public (List<PhChartLogarithmic.IDrawable>? content, LogDiagramAxis axis)? GetCalibrationCurveChartContent(bool miniature)
    {
        if (!ViewModel.HasCurveDetails)
        {
            return null;
        }

        var curve = ViewModel.Data!;
        if (curve.CalibratorTestResults is null)
        {
            return null;
        }
        //set limits for the chart
        var expectedConcentrations = curve.CalibratorTestResults.Select(x => x.ExpectedConcentration).Order().ToList();
        var (xMin, xMax) = SetXLimits(expectedConcentrations);
        var (yMin, yMax) = SetYLimits(curve.MethodPresentation, curve.CalibratorTestResults.SelectMany(x => x.Replicates).ToList());


        var axis = new LogDiagramAxis
        {
            XMin = xMin,
            XMax = xMax,
            YMin = yMin,
            YMax = yMax
        };

        //Get content
        var content = miniature ? GetMiniatureContent(curve, yMin) : GetContent(curve, yMin, yMax);

        return (content, axis);
    }

    private static List<PhChartLogarithmic.IDrawable>? GetMiniatureContent(CurvePresentation curve, double yMin)
    {
        var calibratorPoints = CreateCalibratorPoints(curve.CalibratorTestResults.SelectMany(x => x.Replicates).ToList(), curve.CalibratorTestResults, yMin);

        var content = new List<PhChartLogarithmic.IDrawable> { };

        if (curve.CurveFittedFlag)
        {
            AddCalibratorPoints(curve, content);
        }

        content.Add(calibratorPoints);

        return content;
    }

    private static void AddCalibratorPoints(CurvePresentation curve, List<PhChartLogarithmic.IDrawable> content)
    {
        if (curve.CalculatedPoints is { Count: > 0 })
        {
            var curvePoints = CreateCurvePoints(curve.CalculatedPoints);
            curvePoints.PointList = curvePoints.PointList.OrderBy(x => x.X).ToList();
            content.Add(curvePoints);
        }
    }

    private static List<PhChartLogarithmic.IDrawable>? GetContent(CurvePresentation curve, double yMin, double yMax)
    {
        var axisH = new PhChartLogarithmic.HorizontalAxis
        {
            Label = $"{curve.MethodPresentation.XAxisText} ({curve.MethodPresentation.XAxisUnit})",
            DrawAxisLine = true
        };

        var axisV = new PhChartLogarithmic.VerticalAxis
        {
            Label = $"{curve.MethodPresentation.YAxisText} ({curve.MethodPresentation.YAxisUnit})",
            DrawAxisLine = true
        };

        var calibratorPoints = CreateCalibratorPoints(curve.CalibratorTestResults.SelectMany(x => x.Replicates).ToList(), curve.CalibratorTestResults, yMin);

        var content = new List<PhChartLogarithmic.IDrawable>
        {
            axisH,
            axisV
        };

        var currentValue = Math.Pow(10, Math.Ceiling(Math.Log10(yMin)));

        while (currentValue > 0 && double.IsFinite(currentValue) && currentValue <= yMax)
        {
            var l = new PhChartLogarithmic.HorizontalLine()
            {
                Value = currentValue,
                Label = true
            };
            content.Add(l);
            currentValue *= 10;
        }

        content.AddRange(from result in curve.CalibratorTestResults
                         where result.ExpectedConcentration > 0
                         select new PhChartLogarithmic.VerticalLine
                         {
                             Value = result.ExpectedConcentration,
                             Label = true
                         });

        if (curve.CurveFittedFlag)
        {
            AddCalibratorPoints(curve, content);
        }

        content.Add(calibratorPoints);

        return content;
    }

    private static PhChartLogarithmic.Points CreateCalibratorPoints(ICollection<CalibratorTestReplicate> replicates, ICollection<CalibratorTestResult> testResults, double fallbackY)
    {
        var points = new PhChartLogarithmic.Points();

        foreach (var point in replicates)
        {
            if (point.Response.HasValue || point.IsAborted || point.HasError)
            {
                // Non-positive responses cannot be plotted on the log axis, but still need their status icon.
                var p = new PhChartLogarithmic.Point
                {
                    X = testResults.Single(x => x.Id == point.TestResultId).ExpectedConcentration,
                    Y = point.Response is > 0 ? point.Response.Value : fallbackY,
                    Status = GetStatus(point)
                };

                points.PointList.Add(p);
            }
        }

        return points;
    }

    private static PointStatus GetStatus(CalibratorTestReplicate point)
    {
        if (point.IsAborted || point.HasError || point.IsLowResponse)
        {
            return PointStatus.alert;
        }

        return point.NormDoseCheckResult switch
        {
            NormDoseCheckResult.NotDefined => PointStatus.normal,
            NormDoseCheckResult.NotOk => PointStatus.warning,
            NormDoseCheckResult.Ok => PointStatus.normal,
            _ => throw new ArgumentOutOfRangeException(nameof(point), point.NormDoseCheckResult, null)
        };
    }

    private static PhChartLogarithmic.Curve CreateCurvePoints(ICollection<CalculatedPoint> logChartCalculatedPoints)
    {
        var points = new PhChartLogarithmic.Curve();

        foreach (var point in logChartCalculatedPoints)
        {
            if (point is { Concentration: > 0, Response: > 0 })
            {
                var p = new PhChartLogarithmic.Point
                {
                    X = point.Concentration,
                    Y = point.Response
                };
                points.PointList.Add(p);
            }
        }

        return points;
    }

    private static (double yMin, double yMax) SetYLimits(MethodPresentation methodPresentation, ICollection<CalibratorTestReplicate> replicates)
    {
        double yMin;
        double yMax;

        if (methodPresentation is { FixedYAxis: true, FixedYAxisUpperLimit: not null, FixedYAxisLowerLimit: not null })
        {
            yMin = methodPresentation.FixedYAxisLowerLimit.Value;
            yMax = methodPresentation.FixedYAxisUpperLimit.Value;
        }
        else
        {
            var positiveResponses = replicates
                .Where(r => r.Response is > 0)
                .Select(r => r.Response!.Value)
                .ToList();

            if (positiveResponses.Count > 0)
            {
                yMin = positiveResponses.Min();
                yMax = positiveResponses.Max();
            }
            else
            {
                yMin = 1;
                yMax = 100;
            }

            if (methodPresentation.YAxisScale == AxisScale.Log)
            {
                (yMin, yMax) = LogDiagramAxis.LogAutoScale(yMin, yMax);
            }
        }

        //Add some buffer
        yMax *= 1.1;
        yMin *= 0.9;

        return (yMin, yMax);
    }

    private static (double xMin, double xMax) SetXLimits(List<double> expectedConcentrations)
    {
        var xMin = expectedConcentrations.First();
        var xMax = expectedConcentrations.Last();

        if (xMin < 0.001 && expectedConcentrations.Count > 1)
        {
            xMin = expectedConcentrations[1];
        }

        (xMin, xMax) = LogDiagramAxis.LogAutoScale(xMin, xMax);

        //Add some buffer
        xMax *= 1.1;
        xMin *= 0.9;

        return (xMin, xMax);
    }
}
