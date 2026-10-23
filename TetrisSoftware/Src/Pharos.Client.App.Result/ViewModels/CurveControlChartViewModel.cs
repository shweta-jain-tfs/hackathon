using Pharos.Client.App.Result.Models;
using Pharos.Common.Web.Components.Charts.ChartBase;

namespace Pharos.Client.App.Result.ViewModels;

public class CurveControlChartViewModel : ResultsViewModel, IDisposable
{
    private readonly ICurveControlResultDataSource _dataSource;
    private readonly ICurveDataSource _curveDataSource;
    private readonly ITestResultChartNotificationProvider _chartNotificationProvider;

    public List<ChartHolder> ChartHolders { get; set; } = default!;

    public bool IsWaitingForCurveControl { get; private set; }
    
    private bool _isSubscribedToNotifications;

    public CurveControlChartViewModel(ICurveControlResultDataSource dataSource, ICurveDataSource curveDataSource, ITestResultChartNotificationProvider chartNotificationProvider)
    {
        _dataSource = dataSource;
        _curveDataSource = curveDataSource;
        _chartNotificationProvider = chartNotificationProvider;

        if (_isSubscribedToNotifications)
        {
            return;
        }
        _chartNotificationProvider.CurveControlChartUpdated += OnCurveControlChartUpdated;
        _isSubscribedToNotifications = true;
    }

    private async void OnCurveControlChartUpdated(object? sender, (string, string) e)
    {
        //Only update the chart if the change is for the current method and instrument
        if (e.Item2 == MethodName && e.Item1 == InstrumentSerial)
        {
            await DoLoadAsync();
            OnPropertyChanged(nameof(ChartHolders));
        }
    }

    private static List<List<CurveControlTestResult>> GroupAndSortCurveControls(List<CurveControlTestResult> cc)
    {
        var list = cc.GroupBy(x => x.CurveControlPointName)
            .Select(y => y.OrderByDescending(r => r.MeasurementDateTime).ToList())
            .ToList();

        var x = list.OrderByDescending(x => x.First().JudgmentSummary).ToList();
        return x;
    }
    
    protected override async Task DoLoadAsync()
    {
        var curve = await _curveDataSource.GetByRunAsync(AnalyticalRunId);
        IsWaitingForCurveControl = curve?.CurveState == CalculationStateStatus.WaitingForCurveControl;

        if (IsWaitingForCurveControl)
        {
            ChartHolders = [];
            return;
        }

        var chartList = new List<ChartHolder>();

        _dataSource.AnalyticalRunId = AnalyticalRunId;
        var result = await _dataSource.GetAsync(new DataSourceRequest());
        var grouped = GroupAndSortCurveControls(result.Items);

        foreach (var cc in grouped)
        {
            var chart = new ChartHolder
            {
                CurveControlName = cc.First().CurveControlPointName,
                TestName = cc.First().TestName,
                Status = GetStatus(cc.First().JudgmentSummary),
                CurveControlTestResults = cc
            };

            if (!string.IsNullOrEmpty(chart.CurveControlName))
            {
                var linearChart = await _dataSource.GetCurveControls(AnalyticalRunId, chart.CurveControlName, 5); //todo fix nrofpoints
                var outerParameters = new OuterParameters
                {
                    StartDate = linearChart.StartDate,
                    EndDate = linearChart.EndDate,
                    YMax = linearChart.YMax,
                    YMin = linearChart.YMin,
                };
                chart.OuterParameters = outerParameters;
                chart.ChartComponents = Miniature ? GetThumbNailContent(linearChart) : GetContent(linearChart);
                chart.CurveControlTotalJudgment = linearChart.CurveControlTotalJudgment;
                chart.AnyApprovedTestResultInRun = linearChart.AnyApprovedTestResultInRun;
                chart.FrozenAt = linearChart.FrozenAt;
            }

            chartList.Add(chart);
        }

        ChartHolders = chartList;
    }

    private static List<PhChartLinear.IDrawable> GetThumbNailContent(CurveControlChart linearChart)
    {
        var points = CreatePoints(linearChart);
        var expectedConcentrationLine = CreateLine(linearChart.ExpectedConcentration);
        var regions = CreateRegions(linearChart);
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
    private static List<PhChartLinear.IDrawable> GetContent(CurveControlChart linearChart)
    {
        var points = CreatePoints(linearChart);
        var regions = CreateRegions(linearChart);

        var upperAlertLine = CreateLine(linearChart.UpperAlertLimit);
        var upperWarningLine = CreateLine(linearChart.UpperWarning);
        var lowerWarningLine = CreateLine(linearChart.LowerWarning);
        var lowerAlertLine = CreateLine(linearChart.LowerAlert);
        var expectedConcentrationLine = CreateLine(linearChart.ExpectedConcentration);

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
        content.AddRange(linearChart.CalibrationCurveDates.Select(date => new PhChartLinear.VerticalLine()
        {
            Value = date.LocalDateTime,
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

    private static PhChartLinear.Points CreatePoints(CurveControlChart linearChart)
    {
        var points = new PhChartLinear.Points();
        var ccList = linearChart.CurveControls.ToList();

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
                        X = repList.FirstOrDefault()?.X ?? rep.MeasurementDateTime.LocalDateTime,
                        Y = rep.Concentration.Value,
                        Active = false,
                        Status = MapStatus(rep.CurveControlJudgment)
                    };

                    //Set points belonging to this analytical run as active
                    if (linearChart.ActivePoints != null && linearChart.ActivePoints.Contains(rep.Id))
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
            CurveControlJudgment.OK => StatusType.None,
            CurveControlJudgment.FlaggedHigh or CurveControlJudgment.FlaggedLow => StatusType.Warning,
            _ => StatusType.Alarm
        };
    }

    private static PhChartLinear.Regions CreateRegions(CurveControlChart linearChart)
    {
        var regions = new PhChartLinear.Regions
        {
            RegionList =
            [
                new PhChartLinear.Region
                {
                    Start = linearChart.StartDateRegion,
                    End = linearChart.EndDateRegion,
                    UpperAlert = linearChart.UpperAlertLimit,
                    UpperWarning = linearChart.UpperWarning,
                    LowerWarning = linearChart.LowerWarning,
                    LowerAlert = linearChart.LowerAlert
                }
            ]
        };
        return regions;
    }

    public void Dispose()
    {
        _chartNotificationProvider.CurveControlChartUpdated -= OnCurveControlChartUpdated;
        _isSubscribedToNotifications = false;
        GC.SuppressFinalize(this);
    }

    public async Task RerunCurveControl()
    {
        await _dataSource.RerunCurveControl(AnalyticalRunId);
    }

    public async Task SaveAsync(CurveControlTestResult resultData)
    {
        await _dataSource.UpdateCurveControlComment(resultData);
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
    public bool AnyApprovedTestResultInRun { get; set; }
    public DateTimeOffset? FrozenAt { get; set; }
}