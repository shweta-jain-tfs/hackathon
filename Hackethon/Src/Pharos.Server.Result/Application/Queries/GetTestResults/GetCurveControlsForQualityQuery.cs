using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Queries.GetTestReplicateDetails;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetTestResults;

public record GetCurveControlsForQualityQuery(string InstrumentNumber, string MethodName, string CurveControlName, int LastDays) : IRequest<CurveControlChartDto>;

internal sealed class GetCurveControlsForQualityQueryHandler(IResultDbContext context, IMethodsService methodsService, IDateTime dateTime) : IRequestHandler<GetCurveControlsForQualityQuery, CurveControlChartDto>
{
    public async Task<CurveControlChartDto> Handle(GetCurveControlsForQualityQuery request, CancellationToken cancellationToken)
    {
        var methodName = request.MethodName;
        var curveControlName = request.CurveControlName;
        var instrumentNumber = request.InstrumentNumber;
        var lastDays = request.LastDays;

        var limits = await methodsService.GetCurveControl(methodName, curveControlName,
            control => new MethodLimitsDto
            {
                CurveControlName = control.CurveControlName,
                ExpectedStdDev = control.ExpectedStdDev,
                InnerStdDevLimit = control.InnerStdDevLimit,
                OuterStdDevLimit = control.OuterStdDevLimit,
                ExpectedConcentration = control.ExpectedConcentration
            }) ?? throw new NotFoundException($"CurveControl limits not found for method: {methodName}");
        var linearChart = InitEmptyChart(limits, dateTime.Now);

        var measurementDate = dateTime.Now.Date.AddDays(-lastDays);
        var technology = await methodsService.GetMethod(methodName, x => x.Technology);

        var testResults = context.CurveControlTestResults
            .Include(x => x.Replicates)
            .Where(x => x.Replicates != null
                        && x.MethodName == methodName
                        && x.InstrumentNumber == instrumentNumber
                        && x.CurveControlPointName == curveControlName
                        && x.MeasurementDateTime >= measurementDate)
            .OrderByDescending(x => x.Replicates!.FirstOrDefault()!.MeasurementDateTime)
            .Select(x => new CurveControlTestResultDto
            {
                Id = x.Id,
                CurveControlPointName = x.CurveControlPointName,
                TestName = x.TestName,
                MethodName = x.MethodName,
                MeanResponseAsText = x.MeanResponseAsText,
                MeanConcentrationAsText = x.MeanConcentrationAsText,
                MeasurementDateTime = x.MeasurementDateTime,
                Comment = x.OperatorComment,
                JudgmentSummary = x.JudgmentSummary,
                ExpectedConcentration = x.ExpectedConcentration,
                Technology = technology,
                Replicates = x.Replicates!.OfType<CurveControlTestReplicate>().Select(r => new CurveControlTestReplicateDto
                {
                    Id = r.Id,
                    Concentration = r.Concentration,
                    MeasurementDateTime = r.MeasurementDateTime,
                    CurveControlJudgment = r.Judgment,
                    ReplicateNumber = r.ReplicateNumber,
                    ConcentrationAsText = r.ConcentrationAsText,
                    ResponseAsText = r.ResponseAsText,
                    ConjugateLotNumber = r.TestReplicateReagents!.Where(rr => rr.ReagentType == ArticleType.ConjugateCartridge).Select(rr => rr.LotNumber).FirstOrDefault(),
                    HasError = r.TestReplicateErrors != null && r.TestReplicateErrors.Any(),
                    IsAborted = r.IsAborted
                }).ToList()
            }).ToList();

        if (testResults is { Count: 0 })
        {
            return linearChart;
        }

        foreach (var item in testResults)
        {
            //get concentration unit
            item.Unit = await methodsService.GetMethodTestConcentrationUnit(item.MethodName, item.TestName) ?? string.Empty;
        }

        var calibrationCurveDates = await context.Curves
            .Where(c => c.MethodName == methodName && c.InstrumentNumber == instrumentNumber)
            .Select(x => x.MeasurementDateTime)
            .ToListAsync(cancellationToken);

        linearChart = Populate(linearChart, testResults, calibrationCurveDates, dateTime.Now);

        return linearChart;
    }

    private static CurveControlChartDto InitEmptyChart(MethodLimitsDto limits, DateTimeOffset now)
    {
        //Calculate limits
        var expectedConcentration = limits.ExpectedConcentration;
        var expectedStd = limits.ExpectedStdDev;
        var upperAlert = expectedConcentration + (expectedStd * limits.OuterStdDevLimit);
        var upperWarning = expectedConcentration + (expectedStd * limits.InnerStdDevLimit);
        var lowerWarning = expectedConcentration - (expectedStd * limits.InnerStdDevLimit);
        var lowerAlert = expectedConcentration - (expectedStd * limits.OuterStdDevLimit);
        var yMin = lowerAlert - expectedStd;
        var yMax = upperAlert + expectedStd;

        var minMeasurement = now;
        var maxMeasurement = now;

        var startDate = minMeasurement;
        var start = startDate.AddDays(-0.5);
        var startRegion = startDate.AddMonths(-1);

        var endDate = maxMeasurement;
        var end = endDate.AddDays(0.5);
        var endRegion = endDate.AddMonths(1);


        var linearChartEmpty = new CurveControlChartDto
        {
            ExpectedConcentration = expectedConcentration,
            UpperAlertLimit = upperAlert,
            UpperWarning = upperWarning,
            LowerWarning = lowerWarning,
            LowerAlert = lowerAlert,
            StartDate = start,
            EndDate = end,
            StartDateRegion = startRegion,
            EndDateRegion = endRegion,
            YMax = yMax,
            YMin = yMin,
            CurveControls = new List<CurveControlTestResultDto>(),
            CalibrationCurveDates = new List<DateTimeOffset>(),
            ActivePoints = new List<int>()
        };

        return linearChartEmpty;
    }

    private static CurveControlChartDto Populate(CurveControlChartDto curveControlChart, List<CurveControlTestResultDto> results, List<DateTimeOffset>? calibrationCurveDates, DateTimeOffset now)
    {
        //Set dates
        var rep = results.Where(x => x.Replicates != null).SelectMany(x => x.Replicates!).ToList();

        var measurementTimes = rep.Select(y => y.MeasurementDateTime).ToList();

        var minMeasurement = now;
        var maxMeasurement = now;

        if (measurementTimes is { Count: > 0 })
        {
            minMeasurement = measurementTimes.Min();
            maxMeasurement = measurementTimes.Max();
        }

        var startDate = minMeasurement;
        var start = startDate.AddDays(-0.5);
        var startRegion = startDate.AddMonths(-1);

        var endDate = maxMeasurement;
        var end = endDate.AddDays(0.5);
        var endRegion = endDate.AddMonths(1);

        var activePoints = new List<int>();
        var firstResult = results.First();
        if (firstResult.Replicates != null)
        {
            activePoints.AddRange(firstResult.Replicates.Select(x => x.Id).ToList());
        }

        if (firstResult.JudgmentSummary is CurveControlJudgment.FlaggedLow or CurveControlJudgment.FlaggedHigh)
        {
            // If next result is also flagged, include those as active points
            var secondResult = results.Skip(1).FirstOrDefault();
            if (secondResult is { Replicates: not null })
            {
                if (secondResult.JudgmentSummary is CurveControlJudgment.FlaggedLow or CurveControlJudgment.FlaggedHigh)
                {
                    activePoints.AddRange(secondResult.Replicates.Select(x => x.Id).ToList());
                }
            }
        }

        curveControlChart.StartDate = start;
        curveControlChart.EndDate = end;
        curveControlChart.StartDateRegion = startRegion;
        curveControlChart.EndDateRegion = endRegion;
        curveControlChart.CurveControls = results;
        curveControlChart.CalibrationCurveDates = calibrationCurveDates;
        curveControlChart.ActivePoints = activePoints;

        return curveControlChart;
    }
}