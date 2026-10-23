using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Result;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Commands;

internal class CleanupQualityCommandHandler : IRequestHandler<CleanupQualityCommand>
{
    private readonly IResultDbContext _dbContext;
    private readonly IDateTime _dateTime;
    private readonly ILogger<CleanupQualityCommandHandler> _logger;

    public CleanupQualityCommandHandler(IResultDbContext dbContext, IDateTime dateTime, ILogger<CleanupQualityCommandHandler> logger)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
        _logger = logger;
    }

    public async Task Handle(CleanupQualityCommand request, CancellationToken cancellationToken)
    {
        var compareDate = _dateTime.Now.Subtract(TimeSpan.FromDays(request.OlderThanDays));
        var dateOnlyCompareDate = DateOnly.FromDateTime(compareDate.DateTime);

        _logger.LogInformation("Deleting quality data that has measurement date before {CompareDate} ", compareDate);

        //delete curves (and the associated curve points TestResultType.Calibrator)
        var curvesUsedByAnalyticalRuns = await _dbContext.AnalyticalRuns.Select(x => x.CurveId).Distinct().ToListAsync(cancellationToken);
        var numberOfDeletedCurves = await _dbContext.Curves
            .Where(x => x.LastDayToUse < dateOnlyCompareDate && !curvesUsedByAnalyticalRuns.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation("Deleted {NumberOfDeletedCurves} curves", numberOfDeletedCurves);

        //delete quality control and curve control results
        var numberOfDeletedResults = await _dbContext.TestResults
            .Where(x => x.MeasurementDateTime < compareDate && x.AnalyticalRunId == null)
            .Where(x => x.ResultType == TestResultType.CurveControl || x.ResultType == TestResultType.QualityControl)
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation("Deleted {NumberOfDeletedResults} results", numberOfDeletedResults);

        //delete blanks
        var candidateBlankIds = await _dbContext.Blanks
            .Where(x => x.MeasurementTime < compareDate)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var usedBlankIds = await _dbContext.TestReplicates
            .Where(x => x.UsedBlankId.HasValue)
            .Select(x => x.UsedBlankId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var numberOfDeletedBlanks = 0;

        var blankIdsToDelete = candidateBlankIds.Except(usedBlankIds).ToList();
        if (blankIdsToDelete.Count > 0)
        {
            numberOfDeletedBlanks = await _dbContext.Blanks
                .Where(x => blankIdsToDelete.Contains(x.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        _logger.LogInformation("Deleted {NumberOfDeletedBlanks} blanks", numberOfDeletedBlanks);
    }
}