using MediatR;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Services;

namespace Pharos.Server.Result.Application.Events;

internal sealed class RequestUpdatedEventHandler(IResultDbContext dbContext, IMediator mediator) : INotificationHandler<RequestUpdatedEvent>
{
    public async Task Handle(RequestUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var testOrderIdsFromNotification = notification.TestOrders
            .Select(x => x.Uid)
            .ToList();

        var instrumentsToUpdate = await dbContext.SampleTestResults
            .Where(r => testOrderIdsFromNotification.Contains(r.TestOrderId) && r.AnalyticalRunId.HasValue)
            .Include(r => r.AnalyticalRun)
            .Select(r => r.AnalyticalRun!.InstrumentNumber)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (notification.TestOrders.Any(x => x.State == TestOrderState.ApprovedPendingReflex))
        {
            // When ITestOrder is ApprovedPendingReflex, sync TestResults to the same status.
            var testOrderIds = notification.TestOrders
                .Where(x => x.State == TestOrderState.ApprovedPendingReflex)
                .Select(x => x.Uid)
                .ToList();

            var testResults = await dbContext.SampleTestResults
                .Where(r => testOrderIds.Contains(r.TestOrderId) && r.Status == TestResultStatus.Approved)
                .Include(r => r.AnalyticalRun)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var result in testResults)
            {
                result.Status = TestResultStatus.ApprovedPendingReflex;
                result.AddDomainEvent(new TestResultUpdatedEvent(result));
            }
        }

        if (notification.TestOrders.Any(x => x.State == TestOrderState.Approved))
        {
            // When ITestOrder returns to Approved (e.g. manual reflex handled),
            // sync any lingering ApprovedPendingReflex test results back to Approved.
            var testOrderIds = notification.TestOrders
                .Where(x => x.State == TestOrderState.Approved)
                .Select(x => x.Uid)
                .ToList();

            var testResults = await dbContext.SampleTestResults
                .Where(r => testOrderIds.Contains(r.TestOrderId) && r.Status == TestResultStatus.ApprovedPendingReflex)
                .Include(r => r.AnalyticalRun)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var result in testResults)
            {
                result.Status = TestResultStatus.Approved;
                result.AddDomainEvent(new TestResultUpdatedEvent(result));
            }
        }

        if (dbContext.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var instrument in instrumentsToUpdate)
        {
            await ResultHelper.UpdateTaskCardState(mediator, dbContext, instrument, new[] { TaskCardType.ResultsIssue }, cancellationToken);
        }
    }
}
