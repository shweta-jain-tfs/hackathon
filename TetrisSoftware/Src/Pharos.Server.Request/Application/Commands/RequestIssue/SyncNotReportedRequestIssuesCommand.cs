using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Commands.RequestIssue;

internal record SyncNotReportedRequestIssuesCommand : IRequest;

internal sealed class SyncNotReportedRequestIssuesCommandHandler(
    IRequestDbContext dbContext,
    NotReportedRequestIssueEvaluator notReportedRequestIssueEvaluator,
    ISender sender,
    IRequestHub hub) : IRequestHandler<SyncNotReportedRequestIssuesCommand>
{
    public async Task Handle(SyncNotReportedRequestIssuesCommand request, CancellationToken cancellationToken)
    {
        var existingIssues = await dbContext.Issues
            .Where(issue => issue.IssueType == RequestIssueType.NotReported)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var issuesByRequestId = existingIssues
            .GroupBy(issue => issue.RequestId)
            .ToDictionary(group => group.Key, group => group.ToList());

        // A "Report results" task is only needed for completed requests with approved or rejected results.
        var completedRequestsWithResults = await dbContext.Requests
            .Where(currentRequest =>
                currentRequest.RequestState == RequestState.Completed
                && currentRequest.TestOrders.Any(testOrder =>
                    testOrder.State == TestOrderState.Approved
                    || testOrder.State == TestOrderState.Rejected))
            .Include(x => x.TestOrders)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Load the default settings once because several requests may use the same method.
        var defaultLisChannelIdsByMethodName = await notReportedRequestIssueEvaluator
            .GetDefaultLisChannelIdsByMethodNameAsync(completedRequestsWithResults
                .SelectMany(currentRequest => currentRequest.TestOrders)
                .Where(testOrder => testOrder.State is TestOrderState.Approved or TestOrderState.Rejected));

        var issuesToAdd = new List<Domain.RequestIssue>();
        var issuesToRemove = new List<Domain.RequestIssue>();
        var changedRequestIds = new HashSet<int>();

        foreach (var currentRequest in completedRequestsWithResults)
        {
            var issueExists = issuesByRequestId.Remove(currentRequest.Id, out var currentIssues);
            var shouldCreate = notReportedRequestIssueEvaluator.ShouldCreate(
                currentRequest,
                defaultLisChannelIdsByMethodName);

            if (shouldCreate && !issueExists)
            {
                issuesToAdd.Add(new Domain.RequestIssue
                {
                    RequestId = currentRequest.Id,
                    IssueType = RequestIssueType.NotReported,
                    Urgency = RequestIssueUrgency.Information
                });
                changedRequestIds.Add(currentRequest.Id);
            }
            else if (!shouldCreate && issueExists)
            {
                issuesToRemove.AddRange(currentIssues!);
                changedRequestIds.Add(currentRequest.Id);
            }
        }

        // Delete issues for requests that no longer matched the checks above.
        foreach (var (requestId, staleIssues) in issuesByRequestId)
        {
            issuesToRemove.AddRange(staleIssues);
            changedRequestIds.Add(requestId);
        }

        if (changedRequestIds.Count == 0)
        {
            return;
        }

        if (issuesToAdd.Count > 0)
        {
            dbContext.Issues.AddRange(issuesToAdd);
        }

        if (issuesToRemove.Count > 0)
        {
            dbContext.Issues.RemoveRange(issuesToRemove);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var requestId in changedRequestIds)
        {
            await hub.Updated(requestId);
        }

        await sender.Send(new RegenerateRequestIssueTaskCardsCommand(), cancellationToken);
    }
}
