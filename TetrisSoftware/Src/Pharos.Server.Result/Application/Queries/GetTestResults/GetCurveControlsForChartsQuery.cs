using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Queries.GetTestReplicateDetails;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetTestResults;

public record GetCurveControlsForChartsQuery(int AnalyticalRunId, string CurveControlName, int NrOfPoints) : IRequest<CurveControlChartDto>;

internal sealed class GetCurveControlsForChartsQueryHandler(IResultDbContext context, IMethodsService methodsService, IDateTime dateTime) : IRequestHandler<GetCurveControlsForChartsQuery, CurveControlChartDto>
{
    public async Task<CurveControlChartDto> Handle(GetCurveControlsForChartsQuery request, CancellationToken cancellationToken)
    {
        var analyticalRunInfo = await context.AnalyticalRuns
            .Where(ar => ar.Id == request.AnalyticalRunId)
            .Select(ar => new
            {
                ar.MethodName,
                ar.InstrumentNumber,
                ar.CurveControlTotalJudgment,
                ar.FrozenAt,
                AnyApproved = ar.TestResults
                    .OfType<SampleTestResult>()
                    .Any(x => x.ApprovalStatus == ResultApprovalStatus.Approved)
            })
            .SingleOrDefaultAsync(cancellationToken) ?? throw new NotFoundException();

        var methodName = analyticalRunInfo.MethodName;
        var instrumentNumber = analyticalRunInfo.InstrumentNumber;
        var totalJudgement = analyticalRunInfo.CurveControlTotalJudgment;

        // For committed runs, only consider CCs that arrived before commitment —
        // post-commitment CCs are archived but must not replace the pre-commitment anchor.
        var ccCutoff = analyticalRunInfo.FrozenAt;

        var latestTestResultQuery = context.CurveControlTestResults
            .Where(x => x.AnalyticalRunId == request.AnalyticalRunId && x.CurveControlPointName == request.CurveControlName);

        if (ccCutoff.HasValue)
        {
            latestTestResultQuery = latestTestResultQuery.Where(x => x.MeasurementDateTime <= ccCutoff.Value);
        }
        
        var latestTestResult = await latestTestResultQuery
            .Include(x => x.Replicates)
            .OrderByDescending(x => x.MeasurementDateTime)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? throw new NotFoundException();

        var testResults = GetEntriesAroundKnownEntry(latestTestResult.Id, request.NrOfPoints,
            methodName, instrumentNumber, request.CurveControlName, ccCutoff);

        var calibrationCurveDates = await context.Curves
            .Where(c => c.MethodName == methodName && c.InstrumentNumber == instrumentNumber)
            .Select(x => x.MeasurementDateTime)
            .ToListAsync(cancellationToken);

        var results = testResults.Select(x => new CurveControlTestResultDto
        {
            Id = x.Id,
            CurveControlPointName = x.CurveControlPointName,
            TestName = x.TestName,
            MethodName = x.MethodName,
            MeanResponseAsText = x.MeanResponseAsText,
            MeanConcentrationAsText = x.MeanConcentrationAsText,
            MeasurementDateTime = x.MeasurementDateTime,
            Comment = x.OperatorComment,
            Replicates = x.Replicates!.OfType<CurveControlTestReplicate>().Select(r => new CurveControlTestReplicateDto
            {
                Id = r.Id,
                Concentration = r.Concentration,
                MeasurementDateTime = r.MeasurementDateTime,
                CurveControlJudgment = r.Judgment,
                ReplicateNumber = r.ReplicateNumber,
                ConcentrationAsText = r.ConcentrationAsText,
                ResponseAsText = r.ResponseAsText,
                HasError = r.TestReplicateErrors != null && r.TestReplicateErrors.Any(),
                IsAborted = r.IsAborted
            }).ToList()
        }).ToList();

        foreach (var result in results)
        {
            //get concentration unit
            result.Unit = await methodsService.GetMethodTestConcentrationUnit(result.MethodName, result.TestName) ?? string.Empty;
        }

        var limits = await methodsService.GetCurveControl(methodName, request.CurveControlName,
            control => new MethodLimitsDto
            {
                CurveControlName = control.CurveControlName,
                ExpectedStdDev = control.ExpectedStdDev,
                InnerStdDevLimit = control.InnerStdDevLimit,
                OuterStdDevLimit = control.OuterStdDevLimit,
                ExpectedConcentration = control.ExpectedConcentration
            }) ?? throw new NotFoundException($"CurveControl limits not found for method: {methodName}");

        //Extract the replicates for this analytical run 
        var latestTestReplicatesIds = latestTestResult.Replicates!.Select(x => x.Id).ToList();
        var linearChart = Populate(limits, results, calibrationCurveDates, latestTestReplicatesIds, totalJudgement, analyticalRunInfo.AnyApproved, ccCutoff, dateTime.Now);

        return linearChart;
    }

    private static CurveControlChartDto Populate(MethodLimitsDto limits, List<CurveControlTestResultDto> results, List<DateTimeOffset>? calibrationCurveDates, List<int>? activeCurveControls, CurveControlJudgmentResult totalJudgement, bool anyApproved, DateTimeOffset? frozenAt, DateTimeOffset now)
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
        DateTimeOffset? endDateCalCurve = null;

        if (calibrationCurveDates is { Count: > 0 })
        {
            endDateCalCurve = calibrationCurveDates.Max();
        }

        //Set calibration curve as end date if it is run later than the last run CC
        if (endDateCalCurve.HasValue && endDateCalCurve.Value > endDate)
        {
            endDate = endDateCalCurve.Value;
        }

        var end = endDate.AddDays(0.5);
        var endRegion = endDate.AddMonths(1);

        var linearChart = new CurveControlChartDto
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
            CurveControls = results,
            CalibrationCurveDates = calibrationCurveDates,
            ActivePoints = activeCurveControls,
            CurveControlTotalJudgment = totalJudgement,
            AnyApprovedTestResultInRun = anyApproved,
            FrozenAt = frozenAt
        };

        return linearChart;
    }

    internal List<CurveControlTestResult> GetEntriesAroundKnownEntry(
        int knownId, int minCount, string methodName, string instrumentNumber, string curveControlPointName, DateTimeOffset? frozenAt)
    {
        var minTimespan = minCount - 1;

        // Get all
        var sortedEntries = context.CurveControlTestResults
            .Include(x => x.Replicates)
            .Where(x => x.Replicates != null
                        && x.MethodName == methodName
                        && x.InstrumentNumber == instrumentNumber
                        && x.CurveControlPointName == curveControlPointName
                        && (!frozenAt.HasValue || x.MeasurementDateTime <= frozenAt.Value))
            .OrderByDescending(x => x.Replicates!.FirstOrDefault()!.MeasurementDateTime)
            .Select(d => new
            {
                curveControl = d,
                listOfAnalyticalRun = d.AnalyticalRunId,
                replicateMeasurmentTime = d.Replicates!.Select(y => y.MeasurementDateTime).First()
            })
            .ToList();

        // Find the index of the known test result
        var knownIndex = sortedEntries.FindIndex(e => e.curveControl.Id == knownId);
        if (knownIndex == -1)
        {
            throw new ArgumentException("Known ID not found in the list.");
        }

        // Initialize the result list
        var entry = sortedEntries[knownIndex];
        var result = new List<(CurveControlTestResult, int?, DateTimeOffset?)>
        {
            (entry.curveControl, entry.listOfAnalyticalRun, entry.replicateMeasurmentTime)
        };
        var aboveIndex = knownIndex - 1;
        var belowIndex = knownIndex + 1;

        // Alternate between selecting the closest entry above and below
        while (result.Select(x => x.Item2).Distinct().Count() < minCount
               || (result.Max(e => e.Item3) - result.Min(e => e.Item3)).GetValueOrDefault().TotalDays < minTimespan)
        {
            var added = false;

            if (aboveIndex >= 0)
            {
                result.Add((sortedEntries[aboveIndex].curveControl, sortedEntries[aboveIndex].listOfAnalyticalRun, sortedEntries[aboveIndex].replicateMeasurmentTime));
                aboveIndex--;
                added = true;
            }

            // Check if we meet the timespan and count criteria
            if ((result.Max(e => e.Item3) - result.Min(e => e.Item3)).GetValueOrDefault().TotalDays >= minTimespan
                && result.Select(x => x.Item2).Distinct().Count() >= minCount)
            {
                break;
            }

            if (belowIndex < sortedEntries.Count)
            {
                result.Add((sortedEntries[belowIndex].curveControl, sortedEntries[belowIndex].listOfAnalyticalRun, sortedEntries[belowIndex].replicateMeasurmentTime));
                belowIndex++;
                added = true;
            }

            // Check if we meet the timespan and count criteria
            if ((result.Max(e => e.Item3) - result.Min(e => e.Item3)).GetValueOrDefault().TotalDays >= minTimespan
                && result.Select(x => x.Item2).Distinct().Count() >= minCount)
            {
                break;
            }

            // No more entries to add
            if (!added)
            {
                break;
            }
        }

        return result.Select(x => x.Item1).ToList();
    }
}