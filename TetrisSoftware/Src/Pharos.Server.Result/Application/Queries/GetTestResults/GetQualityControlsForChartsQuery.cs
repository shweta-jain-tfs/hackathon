using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Queries.GetTestReplicateDetails;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetTestResults;

public record GetQualityControlsForChartsQuery(int AnalyticalRunId, string QualityControlName, string TestName, int NrOfPoints)
    : IRequest<QualityControlChartDto>;

internal sealed class GetQualityControlsForChartsQueryHandler(IResultDbContext context, IDateTime dateTime, IMethodsService methodsService)
    : IRequestHandler<GetQualityControlsForChartsQuery, QualityControlChartDto>
{
    public async Task<QualityControlChartDto> Handle(GetQualityControlsForChartsQuery request, CancellationToken cancellationToken)
    {
        var analyticalRunInfo = await context.AnalyticalRuns
            .Where(ar => ar.Id == request.AnalyticalRunId)
            .Select(ar => new
            {
                ar.MethodName,
                ar.InstrumentNumber
            })
            .SingleOrDefaultAsync(cancellationToken) ?? throw new NotFoundException();

        var methodName = analyticalRunInfo.MethodName;
        var instrumentNumber = analyticalRunInfo.InstrumentNumber;

        var latestTestResult = await context.QualityControlTestResults
            .Where(x => x.AnalyticalRunId == request.AnalyticalRunId
                && x.QualityControlName == request.QualityControlName && x.TestName == request.TestName)
            .Include(x => x.Replicates)
            .OrderByDescending(x => x.MeasurementDateTime)
            .AsNoTracking()
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);


        var latestTestReplicatesIds = latestTestResult.Replicates!.Select(x => x.Id).ToList();

        var testResults = GetEntriesAroundKnownEntry(latestTestResult.Id, request.NrOfPoints,
            methodName, instrumentNumber, request.QualityControlName, request.TestName);

        var results = testResults.Select(x => new QualityControlTestResultDto
        {
            Id = x.Id,
            Identity = x.QualityControlName,
            TestName = x.TestName,
            MethodName = x.MethodName,
            MeanResponseAsText = x.MeanResponseAsText,
            Concentration = x.MeanConcentration,
            MeanConcentrationAsText = x.MeanConcentrationAsText,
            MeasurementDateTime = x.MeasurementDateTime,
            Judgment = x.Judgment,
            MinValue = x.MinValue,
            MaxValue = x.MaxValue,
            ExpectedConcentration = x.ExpectedConcentration,
            ExpectedStdDev = x.ExpectedStdDev,
            StdDevLimit = x.StdDevLimit,
            InstrumentDilution = x.InstrumentDilutionFactor,
            PreDilution = x.PreDilutionFactor,
            TotalDilution = x.InstrumentDilutionFactor * x.PreDilutionFactor,
            Replicates = x.Replicates!.Select(r => new QualityControlTestReplicateDto
            {
                Id = r.Id,
                MeasurementDateTime = r.MeasurementDateTime,
                ResponseAsText = r.ResponseAsText,
                ConcentrationAsText = r.ConcentrationAsText,
                HasError = r.TestReplicateErrors?.Any() ?? false,
                ReplicateNumber = r.ReplicateNumber,
                IsAborted = r.IsAborted
            }).ToList()
        }).ToList();

        foreach (var result in results)
        {
            result.Unit = await methodsService.GetMethodTestConcentrationUnit(result.MethodName, result.TestName) ?? string.Empty;
        }

        return Populate(results, latestTestReplicatesIds, dateTime.Now);
    }

    private static QualityControlChartDto Populate(List<QualityControlTestResultDto> results, List<int>? activeQualityControls, DateTimeOffset now)
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

        var endDate = maxMeasurement;
        var end = endDate.AddDays(0.5);

        var chartData = new QualityControlChartDto
        {
            StartDate = start,
            EndDate = end,
            Results = results,
            ActivePoints = activeQualityControls
        };

        return chartData;
    }

    private List<QualityControlTestResult> GetEntriesAroundKnownEntry(int knownId, int minCount, string methodName,
        string instrumentNumber, string qualityControlName, string testName)
    {
        const int minTimespan = 4;

        // Get all
        var sortedEntries = context.QualityControlTestResults
            .Include(x => x.Replicates)
            .Where(x => x.Replicates != null
                        && x.MethodName == methodName
                        && x.InstrumentNumber == instrumentNumber
                        && x.QualityControlName == qualityControlName
                        && x.TestName == testName)
            .OrderByDescending(x => x.Replicates!.FirstOrDefault()!.MeasurementDateTime)
            .Select(d => new
            {
                qualityControl = d,
                listOfAnalyticalRun = d.AnalyticalRunId,
                replicateMeasurmentTime = d.Replicates!.Select(y => y.MeasurementDateTime).First()
            })
            .ToList();

        // Find the index of the known test result
        var knownIndex = sortedEntries.FindIndex(e => e.qualityControl.Id == knownId);
        if (knownIndex == -1)
        {
            throw new ArgumentException("Known ID not found in the list.");
        }

        // Initialize the result list
        var entry = sortedEntries[knownIndex];
        var result = new List<(QualityControlTestResult, int?, DateTimeOffset?)>
        {
            (entry.qualityControl, entry.listOfAnalyticalRun, entry.replicateMeasurmentTime)
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
                result.Add((sortedEntries[aboveIndex].qualityControl, sortedEntries[aboveIndex].listOfAnalyticalRun, sortedEntries[aboveIndex].replicateMeasurmentTime));
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
                result.Add((sortedEntries[belowIndex].qualityControl, sortedEntries[belowIndex].listOfAnalyticalRun, sortedEntries[belowIndex].replicateMeasurmentTime));
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