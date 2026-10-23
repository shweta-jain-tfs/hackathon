using MediatR;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Events;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Services;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Commands;

/// <summary>
/// Resets test results for the specified test orders.
/// </summary>
/// <param name="TestOrderIds">A list of unique test order IDs which test results to be reset.</param>
public record ResetTestResultsCommand(IReadOnlyList<Guid> TestOrderIds) : IRequest<int>;

internal sealed class ResetTestResultsCommandHandler(IResultDbContext dbContext, IMediator mediator, IAuthorizationService authorizationService) : IRequestHandler<ResetTestResultsCommand, int>
{
    public async Task<int> Handle(ResetTestResultsCommand request, CancellationToken cancellationToken)
    {
        var canReset = await CanReset();
        if (!canReset)
        {
            throw new UnauthorizedAccessException("You do not have permission to reset test results.");
        }

        var testResults = await dbContext.SampleTestResults
            .Where(r => request.TestOrderIds.Contains(r.TestOrderId) && r.AnalyticalRunId.HasValue)
            .Include(x => x.AnalyticalRun)
            .Include(x => x.Replicates!)
            .ThenInclude(x => x.TestReplicateErrors)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var modifiedTestResults = new List<ITestResult>();
        var instrumentsToUpdate = new HashSet<string>();

        foreach (var result in testResults)
        {
            if (result.Status is not (TestResultStatus.Rejected or TestResultStatus.Approved or TestResultStatus.ApprovedPendingReflex))
            {
                throw new ValidationException($"The result cannot be reset: Sample ID = {result.RequestUid.SampleId}, Test = {result.TestName}, Method = {result.MethodName}.");
            }

            result.ApprovalStatus = ResultApprovalStatus.NotApproved;

            result.Status = CalculatePreviousStatus(result);

            result.AddDomainEvent(new TestResultUpdatedEvent(result));

            instrumentsToUpdate.Add(result.AnalyticalRun!.InstrumentNumber);
            modifiedTestResults.Add(result);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await mediator.Publish(new TestResultStatusChangedEvent(modifiedTestResults), cancellationToken);

        // Update task card state for each affected instrument
        foreach (var instrument in instrumentsToUpdate)
        {
            await ResultHelper.UpdateTaskCardState(mediator, dbContext, instrument, new[] { TaskCardType.ResultsIssue }, cancellationToken);
        }

        // Return the number of modified test results
        return modifiedTestResults.Count;
    }

    private async Task<bool> CanReset()
    {
        var isService = await authorizationService.IsCurrentUserInRoleAsync(BuiltInRoles.ServiceEngineer);
        if (isService)
        {
            return true;
        }

        var hasApproveResultPermission = await authorizationService.AuthorizeCurrentUserAsync(Permissions.ApproveTestResults);
        if(hasApproveResultPermission && authorizationService.RndMode)
        {
            return true;
        }

        return false;
    }

    private static TestResultStatus CalculatePreviousStatus(SampleTestResult result)
    {
        var resultReplicates = result.Replicates;

        if (resultReplicates == null || resultReplicates.Count == 0)
        {
            //throw exception should never be possible as replicates are required for a test result, but handle just in case
            return TestResultStatus.Aborted;
        }

        var activeReplicates = resultReplicates.Where(x => x.Active).ToList();

        // Aborted: no active replicates, or at least one is aborted or has a low response value
        if (activeReplicates.Count == 0 || activeReplicates.Any(x => x.IsAborted || x.IsLowResponse))
        {
            return TestResultStatus.Aborted;
        }

        // Flagged: errors or doubtful concentration status
        var hasErrors = activeReplicates.Any(x => x.TestReplicateErrors?.Any() == true);
        var isDoubtful = result.MeanConcentrationStatus == ConcentrationStatus.Doubtful;
        if (hasErrors || isDoubtful)
        {
            return TestResultStatus.Flagged;
        }

        // PendingApproval: all replicates fully calculated with no issues
        if (activeReplicates.All(x => x.Concentration != null))
        {
            return TestResultStatus.PendingApproval;
        }

        // PendingCalibration: all replicates have a response, but concentration is not calculated
        if (activeReplicates.All(x => x.Response != null))
        {
            return TestResultStatus.PendingCalibration;
        }

        // PendingBlank: all replicates measured, but at least one has no response value (blank still missing)
        if (activeReplicates.All(x => x.FluorometerValue != null))
        {
            return TestResultStatus.PendingBlank;
        }

        // Processing: at least one active replicate has not been measured by the instrument yet
        return TestResultStatus.Processing;
    }
}