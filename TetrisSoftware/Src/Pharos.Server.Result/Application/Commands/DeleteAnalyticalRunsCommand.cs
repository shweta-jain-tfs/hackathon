using MediatR;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Services;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Commands;

public record DeleteAnalyticalRunsCommand(List<int> AnalyticalRunIds) : IRequest;

internal class DeleteAnalyticalRunsCommandHandler(IResultDbContext dbContext, IMediator sender) : IRequestHandler<DeleteAnalyticalRunsCommand>
{
    public async Task Handle(DeleteAnalyticalRunsCommand command, CancellationToken cancellationToken)
    {
        var analyticalRuns = await dbContext.AnalyticalRuns
            .Where(x => command.AnalyticalRunIds.Contains(x.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var affectedInstruments = analyticalRuns
            .Select(x => x.InstrumentNumber)
            .Distinct()
            .ToList();

        dbContext.AnalyticalRuns.RemoveRange(analyticalRuns);

        var analyticalRunsToBeDeleted = analyticalRuns.Select(x => x.Id).ToList();

        var results = await dbContext.TestResults
            .Include(x => x.AnalyticalRun)
            .Where(x => x.AnalyticalRunId.HasValue && analyticalRunsToBeDeleted.Contains(x.AnalyticalRunId.Value))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var result in results)
        {
            //always remove the link to the analytical run
            result.AnalyticalRun = null;

            //if sample result, check if test order exists, if not delete the sample result
            if (result.ResultType == TestResultType.Sample)
            {
                var sampleResult = (SampleTestResult)result;
                var testOrderExist = await sender.Send(new DoesTestOrderExistQuery(sampleResult.TestOrderId), cancellationToken).ConfigureAwait(false);
                if (!testOrderExist)
                {
                    dbContext.TestResults.Remove(result);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var instrumentNumber in affectedInstruments)
        {
            await ResultHelper.UpdateTaskCardState(sender, dbContext, instrumentNumber, new[] { TaskCardType.QCIssue ,TaskCardType.CalibrationIssue, TaskCardType.ResultsIssue }, cancellationToken);
        }
    }
}