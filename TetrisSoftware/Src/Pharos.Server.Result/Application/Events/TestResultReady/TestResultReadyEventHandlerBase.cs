using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Events.TestResultReady;

internal abstract class TestResultReadyEventHandlerBase<TTestResult>(IResultDbContext dbContext) where TTestResult : TestResult
{
    protected readonly IResultDbContext DbContext = dbContext;

    protected async Task SetTestResultStatus(TTestResult testResult, CancellationToken cancellationToken)
    {
        if (testResult.Replicates == null)
        {
            // this should not happen
            throw new InvalidOperationException("Test result has no replicates.");
        }

        //Only calculate the status on the active replicates
        var activeReplicates = testResult.Replicates.Where(x => x.Active).ToList();

        var isAborted = activeReplicates.Any(x => x.IsAborted) || activeReplicates.Count == 0;
        var isLowResponse = activeReplicates.Any(x => x.IsLowResponse);
        var replicateIds = activeReplicates.Select(x => x.Id).ToArray();
        var hasErrors = await DbContext.TestReplicateErrors.AnyAsync(x => replicateIds.Contains(x.TestReplicateId), cancellationToken);
        var isDoubtful = testResult.MeanConcentrationStatus == ConcentrationStatus.Doubtful;

        if (isAborted || isLowResponse)
        {
            testResult.Status = TestResultStatus.Aborted;
        }
        else if (hasErrors || isDoubtful)
        {
            testResult.Status = TestResultStatus.Flagged;
        }
        else
        {
            testResult.Status = testResult is SampleTestResult ? TestResultStatus.PendingApproval : TestResultStatus.Ready;
        }
    }
}
