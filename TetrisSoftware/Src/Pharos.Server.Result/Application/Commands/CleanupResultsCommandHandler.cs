using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Result;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Commands;

internal class CleanupResultsCommandHandler : IRequestHandler<CleanupResultsCommand>
{
    private readonly IResultDbContext _dbContext;
    private readonly IDateTime _dateTime;
    private readonly ISender _sender;
    private readonly ILogger<CleanupResultsCommandHandler> _logger;

    public CleanupResultsCommandHandler(IResultDbContext dbContext, IDateTime dateTime, ISender sender, ILogger<CleanupResultsCommandHandler> logger)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(CleanupResultsCommand request, CancellationToken cancellationToken)
    {
        var compareDate = _dateTime.Now.Subtract(TimeSpan.FromDays(request.OlderThanDays));

        _logger.LogInformation("Deleting analytical runs that has end date before {CompareDate} ", compareDate);

        var candidatesForDelete = await _dbContext.AnalyticalRuns
            .Include(x => x.TestResults)
            .Where(x => x.EndDateTime.HasValue && x.EndDateTime < compareDate && x.RunStatus != RunStatus.Processing)
            .Where(x => x.TestResults.Where(tr => tr.ResultType == TestResultType.Sample).All(tr => tr.Status == TestResultStatus.Reported))
            .ToListAsync(cancellationToken);

        var candidateIdsForDelete = candidatesForDelete.Select(x => x.Id).ToList();

        foreach (var analyticalRun in candidatesForDelete)
        {
            var exists = await _sender.Send(new DoesAnyTestOrderExistQuery(analyticalRun.TestResults.OfType<SampleTestResult>().Select(x => x.TestOrderId).ToList()), cancellationToken);
            if (exists)
            {
                candidateIdsForDelete.Remove(analyticalRun.Id);
            }
        }

        var numberOfAnalyticalRunsToDelete = candidatesForDelete.Count(x => candidateIdsForDelete.Contains(x.Id));

        foreach (var analyticalRun in candidatesForDelete.Where(x => candidateIdsForDelete.Contains(x.Id)))
        {
            _dbContext.AnalyticalRuns.Remove(analyticalRun);

            foreach (var result in analyticalRun.TestResults)
            {
                //always remove the link to the analytical run
                result.AnalyticalRun = null;

                //if sample result remove it completely
                if (result.ResultType == TestResultType.Sample)
                {
                    _dbContext.TestResults.Remove(result);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted {NumberOfAnalyticalRuns} analytical runs", numberOfAnalyticalRunsToDelete);
    }
}