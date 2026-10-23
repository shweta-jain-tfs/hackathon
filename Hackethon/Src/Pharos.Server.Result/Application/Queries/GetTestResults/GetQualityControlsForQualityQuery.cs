using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Queries.GetTestReplicateDetails;

namespace Pharos.Server.Result.Application.Queries.GetTestResults;

public record GetQualityControlsForQualityQuery(string InstrumentNumber, string MethodName, string QualityControlName, string TestName, int LastDays)
    : IRequest<QualityControlChartDto>;

internal sealed class GetQualityControlsForQualityQueryHandler(IResultDbContext context, IDateTime dateTime, IMethodsService methodsService) : IRequestHandler<GetQualityControlsForQualityQuery, QualityControlChartDto>
{
    public async Task<QualityControlChartDto> Handle(GetQualityControlsForQualityQuery request, CancellationToken cancellationToken)
    {
        var methodName = request.MethodName;
        var instrumentNumber = request.InstrumentNumber;
        var qualityControlName = request.QualityControlName;
        var testName = request.TestName;
        var lastDays = request.LastDays;
        var measurementDate = dateTime.Now.Date.AddDays(-lastDays);

        var chartData = InitEmptyChart(dateTime.Now);

        var testResults = await context.QualityControlTestResults
            .Include(x => x.Replicates)
            .Where(x => x.Replicates != null
                        && x.MethodName == methodName
                        && x.InstrumentNumber == instrumentNumber
                        && x.QualityControlName == qualityControlName
                        && x.TestName == testName
                        && x.MeasurementDateTime >= measurementDate)
            .OrderByDescending(x => x.Replicates!.FirstOrDefault()!.MeasurementDateTime)
            .Select(x => new QualityControlTestResultDto
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
                    HasError = r.TestReplicateErrors != null || r.TestReplicateErrors!.Any(),
                    ReplicateNumber = r.ReplicateNumber,
                    IsAborted = r.IsAborted
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        foreach (var result in testResults)
        {
            //get concentration unit
            result.Unit = await methodsService.GetMethodTestConcentrationUnit(result.MethodName, result.TestName) ?? string.Empty;
        }

        return testResults is { Count: 0 } ? chartData : Populate(chartData, testResults, dateTime.Now);
    }

    private static QualityControlChartDto InitEmptyChart(DateTimeOffset now)
    {
        var minMeasurement = now;
        var maxMeasurement = now;

        var startDate = minMeasurement;
        var start = startDate.AddDays(-0.5);

        var endDate = maxMeasurement;
        var end = endDate.AddDays(0.5);

        var chartData = new QualityControlChartDto
        {
            StartDate = start,
            EndDate = end,
            Results = new List<QualityControlTestResultDto>(),
            ActivePoints = new List<int>()
        };

        return chartData;
    }

    private static QualityControlChartDto Populate(QualityControlChartDto chartData, List<QualityControlTestResultDto> results, DateTimeOffset now)
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

        var activePoints = new List<int>();
        var firstResult = results.First();
        if (firstResult.Replicates != null)
        {
            activePoints.AddRange(firstResult.Replicates.Select(x => x.Id).ToList());
        }

        chartData.StartDate = start;
        chartData.EndDate = end;
        chartData.Results = results;
        chartData.ActivePoints = activePoints;

        return chartData;
    }
}