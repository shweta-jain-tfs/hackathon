using Pharos.Client.App.Result.ViewModels;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.Charts.ChartBase;
using Pharos.Common.Web.Components.Status;
using ICurveControlResultDataSource = Pharos.Client.App.Quality.Models.ICurveControlResultDataSource;

namespace Pharos.Client.App.Quality.ViewModels;

public class CurveControlViewModel(ICurveControlResultDataSource dataSource) : LoadingViewModel, IDisposable
{
    public List<ChartHolder> ChartHolders { get; set; } = default!;
    public string MethodName { get; set; } = string.Empty;
    public string InstrumentNumber { get; set; } = string.Empty;
    public bool Miniature { get; set; }
    public int LastDays { get; set; } = 30;

    protected override async Task DoLoadAsync()
    {
        var chartList = new List<ChartHolder>();
        var ccNames = await dataSource.GetCurveControlNames(MethodName);

        foreach (var cc in ccNames)
        {
            var chart = new ChartHolder
            {
                CurveControlName = cc,
                TestName = "test",
                Status = StatusType.None,
                CurveControlTestResults = []
            };

            if (!string.IsNullOrEmpty(chart.CurveControlName))
            {
                var linearChart = await dataSource.GetCurveControls(InstrumentNumber, MethodName, chart.CurveControlName, LastDays);

                if (linearChart.CurveControls.Count > 0)
                {
                    var lastResult = linearChart.CurveControls.First();
                    chart.TestName = lastResult.TestName;
                    chart.Status = GetStatus(lastResult.JudgmentSummary);
                    chart.CurveControlTestResults = linearChart.CurveControls.ToList();
                }

                chart.CurveControlTotalJudgment = linearChart.CurveControlTotalJudgment;
                chart.FrozenAt = linearChart.FrozenAt;

                // Override status to Success when the flagged curve control has been accepted
                if (chart.IsAccepted)
                {
                    chart.Status = StatusType.Success;
                }

                var outerParameters = new OuterParameters
                {
                    StartDate = linearChart.StartDate,
                    EndDate = linearChart.EndDate,
                    YMax = linearChart.YMax,
                    YMin = linearChart.YMin,
                };
                chart.OuterParameters = outerParameters;
                chart.ChartComponents = Miniature ? GetThumbNailContent(linearChart) : GetContent(linearChart);
            }

            chartList.Add(chart);
        }

        ChartHolders = chartList;
    }

    private static List<PhChartLinear.IDrawable>? GetThumbNailContent(CurveControlChart chart)
    {
        var points = CreatePoints(chart);
        var expectedConcentrationLine = CreateLine(chart.ExpectedConcentration);
        var regions = CreateRegions(chart);
        var content = new List<PhChartLinear.IDrawable>
        {
            regions,
            expectedConcentrationLine
        };

        if (points.PointList.Count > 0)
        {
            points.Small = true;
            content.Add(points);
        }

        return content;
    }

    private static List<PhChartLinear.IDrawable>? GetContent(CurveControlChart chart)
    {
        var points = CreatePoints(chart);
        var regions = CreateRegions(chart);

        var upperAlertLine = CreateLine(chart.UpperAlertLimit);
        var upperWarningLine = CreateLine(chart.UpperWarning);
        var lowerWarningLine = CreateLine(chart.LowerWarning);
        var lowerAlertLine = CreateLine(chart.LowerAlert);
        var expectedConcentrationLine = CreateLine(chart.ExpectedConcentration);

        var border = new PhChartLinear.Border();

        var axisX = new PhChartLinear.HorizontalAxis
        {
            DrawAxisLine = true
        };

        var axisY = new PhChartLinear.VerticalAxis
        {
            TickDistance = 0.1,
            DrawAxisLine = false,
            DrawValues = false,
            DrawTicks = false
        };

        var content = new List<PhChartLinear.IDrawable>
        {
            regions,
            border,
            axisX,
            axisY,
            upperAlertLine,
            upperWarningLine,
            lowerAlertLine,
            lowerWarningLine,
            expectedConcentrationLine
        };

        //Add vertical line for calibration curve dates
        content.AddRange(chart.CalibrationCurveDates.Select(date => new PhChartLinear.VerticalLine()
        {
            Value = date,
            Label = false,
            Dashed = true
        }));

        if (points.PointList.Count > 0)
        {
            content.Add(points);
        }

        return content;
    }
    private static PhChartLinear.HorizontalLine CreateLine(double value)
    {
        var line = new PhChartLinear.HorizontalLine
        {
            Value = value,
            Label = true,
            Dashed = false,
            DrawTick = false
        };
        return line;
    }

    private static PhChartLinear.Points CreatePoints(CurveControlChart chart)
    {
        var points = new PhChartLinear.Points();
        var ccList = chart.CurveControls.ToList();

        foreach (var testResult in ccList)
        {
            var repList = new List<PhChartLinear.Point>();

            //group the replicates together 
            foreach (var rep in testResult.Replicates)
            {
                if (rep.Concentration != null)
                {
                    var point = new PhChartLinear.Point
                    {
                        X = repList.FirstOrDefault()?.X ?? rep.MeasurementDateTime,
                        Y = rep.Concentration.Value,
                        Active = false,
                        Status = MapStatus(rep.CurveControlJudgment),
                        PointRef = testResult
                    };

                    //Set points belonging to this analytical run as active
                    if (chart.ActivePoints.Contains(rep.Id))
                    {
                        point.Active = true;
                    }

                    repList.Add(point);
                }
            }

            if (repList.Count > 0)
            {
                points.PointList.Add(repList.First().X, repList);
            }
        }

        return points;
    }

    private static PointStatus MapStatus(CurveControlJudgment? replicateCurveControlJudgment)
    {
        return replicateCurveControlJudgment switch
        {
            CurveControlJudgment.OK => PointStatus.normal,
            CurveControlJudgment.FlaggedHigh or CurveControlJudgment.FlaggedLow => PointStatus.warning,
            _ => PointStatus.alert
        };
    }
    public static StatusType GetStatus(CurveControlJudgment? ccJudgmentSummary)
    {
        return ccJudgmentSummary switch
        {
            CurveControlJudgment.OK => StatusType.Success,
            CurveControlJudgment.FlaggedHigh or CurveControlJudgment.FlaggedLow => StatusType.Warning,
            _ => StatusType.Alarm
        };
    }

    public static StatusType GetRowStatus(CurveControlJudgment? ccJudgmentSummary)
    {
        return ccJudgmentSummary switch
        {
            CurveControlJudgment.OK => StatusType.Success,
            CurveControlJudgment.FlaggedHigh or CurveControlJudgment.FlaggedLow => StatusType.Warning,
            _ => StatusType.Alarm
        };
    }

    private static PhChartLinear.Regions CreateRegions(CurveControlChart chart)
    {
        var regions = new PhChartLinear.Regions
        {
            RegionList =
            [
                new PhChartLinear.Region
                {
                    Start = chart.StartDateRegion,
                    End = chart.EndDateRegion,
                    UpperAlert = chart.UpperAlertLimit,
                    UpperWarning = chart.UpperWarning,
                    LowerWarning = chart.LowerWarning,
                    LowerAlert = chart.LowerAlert
                }
            ]
        };
        return regions;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public void OnHighlightedItemChanged(CurveControlTestResult? result)
    {
        foreach (var chart in ChartHolders)
        {
            if (result != null && chart.CurveControlName == result.CurveControlPointName && chart.ChartComponents != null)
            {
                foreach (var component in chart.ChartComponents)
                {
                    if (component is PhChartLinear.Points points)
                    {
                        foreach (var point in points.PointList)
                        {
                            foreach (var point1 in point.Value)
                            {
                                if (point1.PointRef == result)
                                {
                                    point1.Selected = true;
                                }
                                else
                                {
                                    point1.Selected = false;
                                }
                            }
                        }
                    }
                }
                break;
            }
        }
    }
}

public class ChartHolder
{
    public List<PhChartLinear.IDrawable>? ChartComponents { get; set; }
    public OuterParameters OuterParameters { get; set; } = default!;
    public string CurveControlName { get; set; } = default!;
    public string TestName { get; set; } = default!;
    public StatusType Status { get; set; }
    public List<CurveControlTestResult> CurveControlTestResults { get; set; } = default!;
    public CurveControlJudgmentResult CurveControlTotalJudgment { get; set; } = CurveControlJudgmentResult.NotJudged;
    public DateTimeOffset? FrozenAt { get; set; }
    public bool IsAccepted => CurveControlTotalJudgment == CurveControlJudgmentResult.Flagged && FrozenAt != null;
}