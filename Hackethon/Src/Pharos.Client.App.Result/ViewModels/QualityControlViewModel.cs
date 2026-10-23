using Pharos.Client.App.Result.Models;
using Pharos.Common.Web.Components.Charts.ChartBase;

namespace Pharos.Client.App.Result.ViewModels;

internal class QualityControlViewModel : ResultsViewModel, IDisposable
{
    //
    // utility class for the conversion from test result to chart region
    //
    private record QcLimit(DateTimeOffset Timestamp, double? Min, double? Max, double? ExpectedConcentration)
    {
        // compare using only min max values
        public bool CompareLimits(QcLimit? limit)
        {
            return limit != null && ToleranceCompare(Min, limit.Min) && ToleranceCompare(Max, limit.Max);
        }
    }

    public List<QualityControlChartHolder>? ChartHolders { get; set; }

    private readonly int[] _orderOfSeverityQcMap =
    [
        (int)QualityControlJudgement.NotDefined,
        (int)QualityControlJudgement.High,
        (int)QualityControlJudgement.Low,
        (int)QualityControlJudgement.HighCv,
        (int)QualityControlJudgement.FlaggedHigh,
        (int)QualityControlJudgement.FlaggedLow,
        (int)QualityControlJudgement.Ok
    ];

    public (string, string) SelectedLeftTestResult
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(SelectedLeftTestResult));
        }
    }

    public (string, string) SelectedRightTestResult
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(SelectedRightTestResult));
        }
    }

    private readonly IQualityControlDataSource _dataSource;
    private readonly ITestResultChartNotificationProvider _chartNotificationProvider;

    public QualityControlViewModel(ITestResultChartNotificationProvider chartNotificationProvider, IQualityControlDataSource qualityControlDataSource)
    {
        _chartNotificationProvider = chartNotificationProvider;
        _dataSource = qualityControlDataSource;
        _chartNotificationProvider.QualityControlChartUpdated += OnQualityControlChartUpdated;
    }

    private async void OnQualityControlChartUpdated(object? sender, (string instrumentNumber, string methodName) e)
    {
        //Only update the chart if the change is for the current method and instrument
        if (e.methodName == MethodName && e.Item1 == InstrumentSerial)
        {
            await DoLoadAsync();
            OnPropertyChanged(nameof(ChartHolders));
        }
    }

    protected override async Task DoLoadAsync()
    {
        var chartList = new List<QualityControlChartHolder>();

        _dataSource.AnalyticalRunId = AnalyticalRunId;
        var result = await _dataSource.GetAsync(new DataSourceRequest());

        var grouped = result.Items.GroupBy(x => new
            {
                x.Identity,
                x.TestName
            })
            .Select(y => y.OrderByDescending(x => x.MeasurementDateTime).ToList())
            .OrderBy(innerList => _orderOfSeverityQcMap[(int)innerList.First().Judgment!])
            .ThenByDescending(x => x.First().MeasurementDateTime)
            .ToList();

        foreach (var qc in grouped)
        {
            var chart = new QualityControlChartHolder
            {
                QualityControlName = qc.First().Identity,
                TestName = qc.First().TestName,
                Status = GetStatus(qc.First().Judgment),
                QualityControlId = qc.First().Id,
                TestResults = qc
            };

            var content = await GetQualityControlChartContent(AnalyticalRunId, chart.QualityControlName, chart.TestName, 5, Miniature);

            var outerParameters = new OuterParameters
            {
                StartDate = content.start,
                EndDate = content.end,
                YMax = content.yMax,
                YMin = content.yMin
            };

            chart.ChartComponents = content.Item1;
            chart.OuterParameters = outerParameters;
            chartList.Add(chart);
        }

        ChartHolders = chartList;
    }

    public async Task<(List<PhChartLinear.IDrawable>? content, DateTimeOffset start, DateTimeOffset end, double yMin, double yMax)>
        GetQualityControlChartContent(int id, string qualityControlName, string testName, int points, bool thumbnail)
    {
        var chartData = await _dataSource.GetQualityControls(id, qualityControlName, testName, points);
        chartData.Results = [.. chartData.Results.OrderBy(r => r.MeasurementDateTime)];
        var content = ChartDataToDrawables(chartData, thumbnail);
        return (content.Item1, chartData.StartDate, chartData.EndDate, content.Item2.yMin, content.Item2.yMax);
    }

    private static (List<PhChartLinear.IDrawable>, (double yMin, double yMax)) ChartDataToDrawables(QualityControlChart chartData, bool thumbnail)
    {
        var limits = TestResultsToLimits(chartData.Results, chartData.StartDate);
        var chartYMinMax = GetChartMinMax(limits, chartData.Results);
        var (regions, lines) = LimitsToRegionsAndLines(chartData, limits, chartYMinMax.yMin, chartYMinMax.yMax);

        var content = new List<PhChartLinear.IDrawable>
        {
            regions
        };

        if (lines is { Count: > 0 })
        {
            content.AddRange(lines);

            if (!thumbnail)
            {
                content.Add(new PhChartLinear.VerticalAxis
                {
                    TickDistance = 0.1,
                    DrawAxisLine = false,
                    DrawValues = false,
                    DrawTicks = false
                });
            }
        }

        var points = CreatePoints(chartData);
        if (points.PointList.Count > 0)
        {
            if (thumbnail)
            {
                points.Small = true;
            }

            content.Add(points);
        }

        if (!thumbnail)
        {
            content.Add(new PhChartLinear.Border());
            content.Add(new PhChartLinear.HorizontalAxis
            {
                DrawAxisLine = true
            });
        }

        return (content, chartYMinMax);
    }

    private static PhChartLinear.HorizontalLine CreateLine(double value, DateTimeOffset endDate, DateTimeOffset startDate)
    {
        return new PhChartLinear.HorizontalLine
        {
            Value = value,
            Label = true,
            Dashed = false,
            DrawTick = false,
            EndX = endDate,
            StartX = startDate
        };
    }

    private static PhChartLinear.Points CreatePoints(QualityControlChart chartData)
    {
        var points = new PhChartLinear.Points();

        foreach (var testResult in chartData.Results)
        {
            var repList = new List<PhChartLinear.Point>();

            //group the replicates together 
            foreach (var rep in testResult.Replicates)
            {
                if (testResult.Concentration != null)
                {
                    repList.Add(new PhChartLinear.Point
                    {
                        X = testResult.MeasurementDateTime,
                        Y = testResult.Concentration.Value,
                        Active = chartData.ActivePoints.Contains(rep.Id),
                        Status = MapStatus(testResult.Judgment)
                    });
                }
            }

            if (repList.Count > 0)
            {
                points.PointList.Add(repList.First().X, repList);
            }
        }

        return points;
    }

    private static PointStatus MapStatus(QualityControlJudgement? judgment)
    {
        return judgment switch
        {
            QualityControlJudgement.Ok => PointStatus.normal,
            QualityControlJudgement.FlaggedHigh or QualityControlJudgement.FlaggedLow => PointStatus.warning,
            _ => PointStatus.alert
        };
    }


    private static StatusType GetStatus(QualityControlJudgement? qcJudgment)
    {
        return qcJudgment switch
        {
            QualityControlJudgement.Ok => StatusType.Success,
            QualityControlJudgement.FlaggedHigh or QualityControlJudgement.FlaggedLow => StatusType.Warning,
            _ => StatusType.Alarm
        };
    }

    private static (PhChartLinear.Regions regions, List<PhChartLinear.HorizontalLine> lines) LimitsToRegionsAndLines(QualityControlChart chartData, List<QcLimit> limits, double chartMin, double chartMax)
    {
        var regions = new PhChartLinear.Regions();
        var lines = new List<PhChartLinear.HorizontalLine>();

        if (limits.Count > 0)
        {
            regions.RegionList = [];

            for (var i = 0; i < limits.Count; i++)
            {
                var startDate = limits[i].Timestamp;
                var endDate = i < limits.Count - 1 ? limits[i + 1].Timestamp : chartData.EndDate;

                regions.RegionList.Add
                (
                    new PhChartLinear.Region
                    {
                        Start = startDate,
                        End = endDate,
                        UpperWarning = limits[i].Max ?? chartMax,
                        UpperAlert = limits[i].Max ?? chartMax,
                        LowerWarning = limits[i].Min ?? chartMin,
                        LowerAlert = limits[i].Min ?? chartMin
                    }
                );

                if (limits[i].Max != null)
                {
                    lines.Add(CreateLine(limits[i].Max!.Value, endDate, startDate));
                }

                if (limits[i].Min != null)
                {
                    lines.Add(CreateLine(limits[i].Min!.Value, endDate, startDate));
                }

                if (limits[i].ExpectedConcentration != null)
                {
                    lines.Add(CreateLine(limits[i].ExpectedConcentration!.Value, endDate, startDate));
                }
            }
        }

        return (regions, lines);
    }

    //
    // return unique limits
    //
    private static List<QcLimit> TestResultsToLimits(ICollection<QualityControlTestResult> testResults, DateTimeOffset chartStart)
    {
        List<QcLimit> limits = [];
        foreach (var limit in testResults.Select(TestResultToLimit).Where(limit => limits.Count == 0 || !limit.CompareLimits(limits.Last())))
        {
            limits.Add(limits.Count != 0
                ? limit
                : limit with
                {
                    Timestamp = chartStart
                });
        }

        return limits;
    }

    private static QcLimit TestResultToLimit(QualityControlTestResult testResult)
    {
        return testResult switch
        {
            { ExpectedStdDev: not null, StdDevLimit: not null } =>
                new QcLimit(testResult.MeasurementDateTime,
                    testResult.ExpectedConcentration - (testResult.ExpectedStdDev * testResult.StdDevLimit),
                    testResult.ExpectedConcentration + (testResult.ExpectedStdDev * testResult.StdDevLimit), testResult.ExpectedConcentration),
            { MinValue: null, MaxValue: null } =>
                new QcLimit(testResult.MeasurementDateTime, null, null, null),
            _ => new QcLimit(testResult.MeasurementDateTime, testResult.MinValue, testResult.MaxValue, null)
        };
    }

    //
    // set chart min max based on
    // 1. limits if available
    // 2. results if available
    // 3. default to 0.0
    //
    private static (double yMin, double yMax) GetChartMinMax(ICollection<QcLimit> limits, ICollection<QualityControlTestResult> results)
    {
        var min = limits.Min(x => x.Min) ?? results.Min(x => x.Concentration) ?? 0.0;
        var max = limits.Max(x => x.Max) ?? results.Max(x => x.Concentration) ?? 0.0;
        var offset = (max - min) * 0.3;
        return (min - offset, max + offset);
    }

    //
    // null,null -> true | null,value -> false | value,null -> false | value,value -> compare
    //
    private static bool ToleranceCompare(double? d1, double? d2, double tolerance = 0.00001)
    {
        return d1 == null || d2 == null ? d1 == null && d2 == null : d1.Value - d2.Value < tolerance;
    }


    public void Dispose()
    {
        _chartNotificationProvider.QualityControlChartUpdated -= OnQualityControlChartUpdated;
    }
}

public class QualityControlChartHolder
{
    public List<PhChartLinear.IDrawable>? ChartComponents { get; set; }
    public OuterParameters OuterParameters { get; set; } = default!;
    public string QualityControlName { get; set; } = default!;
    public int QualityControlId { get; set; } = default!;
    public string TestName { get; set; } = default!;
    public StatusType Status { get; set; }
    public List<QualityControlTestResult> TestResults { get; set; } = default!;
}