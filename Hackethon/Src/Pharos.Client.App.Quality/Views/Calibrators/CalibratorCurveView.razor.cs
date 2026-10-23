using System.ComponentModel;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Quality.ViewModels;
using Pharos.Client.App.Result.Models;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Charts.ChartBase;

namespace Pharos.Client.App.Quality.Views.Calibrators;

public partial class CalibratorCurveView : IDisposable
{
    [Parameter] public bool Miniature { get; set; }
    [Parameter] public CalibratorCurveViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    private List<PhChartLogarithmic.IDrawable>? _content;
    private LogDiagramAxis _axis = default!;
    private int _width;
    private int _height;

    protected override void OnInitialized()
    {
        ViewModel.PropertyChanged += OnPropertyChanged;
    }

    protected override void OnParametersSet()
    {
        var (content, axis) = GetCalibrationCurveChartContent(Miniature);

        _axis = axis;
        _content = content;
        _width = Miniature ? 500 : 806;
        _height = Miniature ? 180 : 800;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is CalibratorCurveViewModel { IsBusy: true })
        {
            // No need to refresh until the vm is not busy
            return;
        }

        (_content, _axis) = GetCalibrationCurveChartContent(Miniature);
        StateHasChanged();
    }

    public (List<PhChartLogarithmic.IDrawable>? content, LogDiagramAxis axis) GetCalibrationCurveChartContent(bool miniature)
    {
        var curves = ViewModel.Data!;

        if (curves == null || curves.Count == 0)
        {
            return (null, _axis);
        }

        var curve = curves.First();
        if (curves.Any(x => x.CurveFittedFlag))
        {
            curve = curves.First(x => x.CurveFittedFlag);
        }

        //set limits for the chart
        var expectedConcentrations = (curve.CalibratorTestResults ?? [])
                                        .Select(x => x.ExpectedConcentration).Order().ToList();
        var (xMin, xMax) = SetXLimits(expectedConcentrations);
        var (yMin, yMax) = SetYLimits(curve.MethodPresentation, (curve.CalibratorTestResults ?? []).SelectMany(x => x.Replicates ?? []).ToList());


        var axis = new LogDiagramAxis
        {
            XMin = xMin,
            XMax = xMax,
            YMin = yMin,
            YMax = yMax
        };

        //Get content for all curves
        var content = miniature ? GetMiniatureContent(curves) : GetContent(curves, yMin, yMax);

        return (content, axis);
    }

    private static List<PhChartLogarithmic.IDrawable> GetMiniatureContent(ICollection<CurvePresentation> curves)
    {
        var axisH = new PhChartLogarithmic.HorizontalAxis
        {
            DrawAxisLine = true
        };

        var axisV = new PhChartLogarithmic.VerticalAxis
        {
            DrawAxisLine = true
        };
        var content = new List<PhChartLogarithmic.IDrawable>
        {
            axisH,
            axisV
        };

        foreach (var curve in curves)
        {
            var calibratorPoints = CreateCalibratorPoints((curve.CalibratorTestResults ?? []).SelectMany(x => x.Replicates ?? []).ToList(), curve.CalibratorTestResults ?? []);
            if (curve.CurveFittedFlag)
            {
                AddCalibratorPoints(curve, content);
            }

            content.Add(calibratorPoints);
        }

        return content;
    }

    private static void AddCalibratorPoints(CurvePresentation curve, List<PhChartLogarithmic.IDrawable> content)
    {
        if (curve.CalculatedPoints is { Count: > 0 })
        {
            var curvePoints = CreateCurvePoints(curve.CalculatedPoints, curve.SelectedCurve, curve.ActiveCurve);
            curvePoints.PointList = curvePoints.PointList.OrderBy(x => x.X).ToList();
            content.Add(curvePoints);
        }
    }

    private static List<PhChartLogarithmic.IDrawable> GetContent(ICollection<CurvePresentation> curves, double yMin, double yMax)
    {
        var firstCurve = curves.First();
        var axisH = new PhChartLogarithmic.HorizontalAxis
        {
            Label = $"{firstCurve.MethodPresentation.XAxisText} ({firstCurve.MethodPresentation.XAxisUnit})",
            DrawAxisLine = true
        };

        var axisV = new PhChartLogarithmic.VerticalAxis
        {
            Label = $"{firstCurve.MethodPresentation.YAxisText} ({firstCurve.MethodPresentation.YAxisUnit})",
            DrawAxisLine = true
        };

        var content = new List<PhChartLogarithmic.IDrawable>
        {
            axisH,
            axisV
        };
        
        foreach (var curve in curves.OrderBy(x => x.SelectedCurve))
        {
            var calibratorPoints = CreateCalibratorPoints((curve.CalibratorTestResults ?? []).SelectMany(x => x.Replicates ?? []).ToList(), curve.CalibratorTestResults ?? []);

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

            if (curve.SelectedCurve)
            {
                content.Add(calibratorPoints);
            }
        }

        return content;
    }

    private static PhChartLogarithmic.Points CreateCalibratorPoints(ICollection<CalibratorTestReplicate> replicates, ICollection<CalibratorTestResult> testResults)
    {
        var points = new PhChartLogarithmic.Points();

        foreach (var point in replicates)
        {
            if (point is { Response: > 0 })
            {
                var p = new PhChartLogarithmic.Point
                {
                    X = testResults.Single(x => x.Id == point.TestResultId).ExpectedConcentration,
                    Y = point.Response.Value,
                    Status = GetStatus(point.NormDoseCheckResult)
                };

                points.PointList.Add(p);
            }
        }

        return points;
    }

    private static PointStatus GetStatus(NormDoseCheckResult pointConcentrationStatus)
    {
        return pointConcentrationStatus switch
        {
            NormDoseCheckResult.NotDefined => PointStatus.alert,
            NormDoseCheckResult.NotOk => PointStatus.warning,
            NormDoseCheckResult.Ok => PointStatus.normal,
            _ => throw new ArgumentOutOfRangeException(nameof(pointConcentrationStatus), pointConcentrationStatus, null)
        };
    }

    private static PhChartLogarithmic.Curve CreateCurvePoints(ICollection<CalculatedPoint> logChartCalculatedPoints, bool curveSelected, bool curveActive)
    {
        var points = new PhChartLogarithmic.Curve
        {
            Selected = curveSelected,
            Active = curveActive
        };

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
            if (replicates.All(r => r.Response.HasValue))
            {
                yMin = replicates.Where(r => r.Response is > 0).Min(d => d.Response)!.Value;
                yMax = replicates.Where(r => r.Response is > 0).Max(d => d.Response)!.Value;
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

    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnPropertyChanged;
        GC.SuppressFinalize(this);
    }
}