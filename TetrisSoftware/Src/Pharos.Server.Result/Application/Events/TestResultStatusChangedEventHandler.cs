using MediatR;
using Pharos.Server.Application.Result;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Services;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Events;

internal class TestResultStatusChangedEventHandler(
    IResultDbContext dbContext) : INotificationHandler<TestResultStatusChangedEvent>
{
    public async Task Handle(TestResultStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.TestResults.All(x => x.ApprovalStatus is ResultApprovalStatus.NotApproved))
        {
            // nothing to do, no Approved or Rejected results
            return;
        }

        var resultIdList = notification.TestResults
            .Where(result => result.ApprovalStatus is not ResultApprovalStatus.NotApproved)
            .Select(result => result.Id)
            .ToList();

        var analyticalRunIdList = await GetListOfAnalyticalRunIdsHavingTestIssues(resultIdList, cancellationToken);

        if (await CheckAnalyticalRunsForTestIssueUpdates(analyticalRunIdList, cancellationToken))
        {
            // Some Test issues updated...
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await UpdateAnalyticalRunApprovalStatus(resultIdList, cancellationToken);
    }

    private async Task<List<int>> GetListOfAnalyticalRunIdsHavingTestIssues(IReadOnlyList<int> resultIdList, CancellationToken cancellationToken)
    {
        return await
            dbContext.SampleTestResults
                .Where(result => result.AnalyticalRunId.HasValue && resultIdList.Contains(result.Id) && result.AnalyticalRun!.TestIssue != AnalyticalRunIssue.NoIssue)
                .GroupBy(result => result.AnalyticalRunId!.Value)
                .Select(arGroup => arGroup.Key)
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> CheckAnalyticalRunsForTestIssueUpdates(List<int> analyticalRunIdList, CancellationToken cancellationToken)
    {
        var anyUpdates = false;

        foreach (var analyticalRunId in analyticalRunIdList)
        {
            // Still having Test issues...
            if (await dbContext.SampleTestResults.AnyAsync(r =>
                    r.AnalyticalRunId == analyticalRunId && r.ApprovalStatus == ResultApprovalStatus.NotApproved &&
                    (r.MeanConcentrationStatus == ConcentrationStatus.Doubtful ||
                     r.Replicates!.Any(repl => repl.IsLowResponse || repl.IsAborted || repl.TestReplicateErrors!.Any())), cancellationToken))
            {
            }

            // No Test issues anymore...
            else
            {
                await ResetAnalyticalRunTestIssue(analyticalRunId);
                anyUpdates = true;
            }
        }

        return anyUpdates;
    }

    private async Task ResetAnalyticalRunTestIssue(int analyticalRunId)
    {
        var analyticalRun = await dbContext.AnalyticalRuns.FirstOrDefaultAsync(ar => ar.Id == analyticalRunId);

        analyticalRun!.AddDomainEvent(new UpdateAnalyticalRunQualityIssue(analyticalRun, null, null, null, AnalyticalRunIssue.NoIssue));
    }

    private async Task UpdateAnalyticalRunApprovalStatus(IReadOnlyList<int> resultIdList, CancellationToken cancellationToken)
    {
        var analyticalRunIdList = await
            dbContext.SampleTestResults
                .Where(result => result.AnalyticalRunId.HasValue && resultIdList.Contains(result.Id))
                .GroupBy(result => result.AnalyticalRunId!.Value)
                .Select(arGroup => arGroup.Key)
                .ToListAsync(cancellationToken);
        foreach (var analyticalRunId in analyticalRunIdList)
        {
            await ResultHelper.UpdateApprovalStatusForAnalyticalRun(dbContext, analyticalRunId, cancellationToken);
        }
    }

}
