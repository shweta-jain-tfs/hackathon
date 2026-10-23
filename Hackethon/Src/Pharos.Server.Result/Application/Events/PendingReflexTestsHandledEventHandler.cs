using MediatR;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Services;

namespace Pharos.Server.Result.Application.Events;

internal class PendingReflexTestsHandledEventHandler(IResultDbContext dbContext, IMediator mediator) : INotificationHandler<PendingReflexTestsHandledEvent>
{
    public async Task Handle(PendingReflexTestsHandledEvent notification, CancellationToken cancellationToken)
    {
        var testResults = await dbContext.SampleTestResults
            .Where(r => notification.TestOrderIds.Contains(r.TestOrderId) && r.Status == TestResultStatus.ApprovedPendingReflex)
            .Include(r => r.AnalyticalRun)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var instrumentsToUpdate = testResults
            .Where(r => r.AnalyticalRun != null)
            .Select(r => r.AnalyticalRun!.InstrumentNumber)
            .Distinct()
            .ToList();

        foreach (var result in testResults)
        {
            result.ApprovalStatus = ResultApprovalStatus.Approved;
            result.Status = result.IsReportedToLis
                ? TestResultStatus.Reported
                : TestResultStatus.Approved;
            result.AddDomainEvent(new TestResultUpdatedEvent(result));
        }

        if (!dbContext.HasChanges())
        {
            return;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await mediator.Publish(new TestResultStatusChangedEvent(testResults), cancellationToken);

        foreach (var instrument in instrumentsToUpdate)
        {
            // TODO: Change when we include pendingresultissues in analytical run issues.
            await ResultHelper.UpdateTaskCardState(mediator, dbContext, instrument, new[] { TaskCardType.ResultsIssue }, cancellationToken);
        }
    }
}
