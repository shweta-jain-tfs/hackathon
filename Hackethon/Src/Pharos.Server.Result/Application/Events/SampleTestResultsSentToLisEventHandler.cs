using MediatR;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Services;

namespace Pharos.Server.Result.Application.Events;
internal class SampleTestResultsSentToLisEventHandler(IResultDbContext dbContext, IMediator mediator) : INotificationHandler<SampleTestResultsSentToLisEvent>
{
    public async Task Handle(SampleTestResultsSentToLisEvent notification, CancellationToken cancellationToken)
    {
        var testResults = await dbContext.SampleTestResults
            .Where(x => notification.TestOrderIds.Contains(x.TestOrderId)
                && (x.Status == TestResultStatus.Rejected
                    || x.Status == TestResultStatus.Approved
                    || x.Status == TestResultStatus.ApprovedPendingReflex))
            .Include(x => x.AnalyticalRun)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var instrumentsToUpdate = new HashSet<string>();

        foreach (var testResult in testResults)
        {
            testResult.IsReportedToLis = true;
            if (testResult.Status != TestResultStatus.ApprovedPendingReflex)
            {
                testResult.Status = TestResultStatus.Reported;
            }

            testResult.AddDomainEvent(new TestResultUpdatedEvent(testResult));
            if (testResult.AnalyticalRun?.InstrumentNumber is { } instrumentNumber)
            {
                instrumentsToUpdate.Add(instrumentNumber);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var instrument in instrumentsToUpdate)
        {
            await ResultHelper.UpdateTaskCardState(mediator, dbContext, instrument, new[] { TaskCardType.ResultsIssue }, cancellationToken);
        }
    }
}
