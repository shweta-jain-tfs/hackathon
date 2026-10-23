using MediatR;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Events;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Services;

namespace Pharos.Server.Result.Application.Commands;

/// <summary>
/// Approves ready test results for the specified test orders.
/// </summary>
/// <param name="TestOrderIds">A list of unique test order IDs which test results to be approved.</param>
/// <param name="Reject">If true the test results a rejected.</param>
/// <param name="Retest">If true new tests are added for the rejected test results. Only applicable if Reject is true.</param>
/// <param name="InstrumentDilution">Optional instrument dilution factor for the retest. Only applicable if Retest is true and only one test result is being retested.</param>
public record ApproveTestResultsCommand(IReadOnlyList<Guid> TestOrderIds, bool Reject, bool Retest = false, int? InstrumentDilution = null) : IRequest<int>;

internal sealed class ApproveTestResultsCommandHandler(IResultDbContext dbContext, IMediator mediator) : IRequestHandler<ApproveTestResultsCommand, int>
{
    public async Task<int> Handle(ApproveTestResultsCommand request, CancellationToken cancellationToken)
    {
        var testResults = await dbContext.SampleTestResults
            .Where(r => request.TestOrderIds.Contains(r.TestOrderId) && r.AnalyticalRunId.HasValue)
            .Include(x => x.AnalyticalRun)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!request.Reject && testResults.Count > 1 && testResults.Any(r => r.Status == TestResultStatus.Flagged))
        {
            throw new ValidationException("A flagged result cannot be approved together with other selected results.");
        }

        var modifiedTestResults = new List<ITestResult>();
        var instrumentsToUpdate = testResults
            .Select(result => result.AnalyticalRun!.InstrumentNumber)
            .ToHashSet();

        var testOrdersToRetest = new List<Guid>();
        foreach (var result in testResults)
        {
            if ((result.Status is TestResultStatus.Aborted or TestResultStatus.Processing
                or TestResultStatus.PendingBlank or TestResultStatus.PendingCalibration) && !request.Reject)
            {
                throw new ValidationException($"The result cannot be approved: Sample ID = {result.RequestUid.SampleId}, Test = {result.TestName}, Method = {result.MethodName}.");
            }

            if (result.ApprovalStatus == ResultApprovalStatus.NotApproved)
            {
                if (request.Reject)
                {
                    result.ApprovalStatus = ResultApprovalStatus.Rejected;
                    result.Status = TestResultStatus.Rejected;
                    if (request.Retest)
                    {
                        testOrdersToRetest.Add(result.TestOrderId);
                    }
                }
                else
                {
                    result.ApprovalStatus = ResultApprovalStatus.Approved;
                    result.Status = TestResultStatus.Approved;
                }

                result.AddDomainEvent(new TestResultUpdatedEvent(result));

                modifiedTestResults.Add(result);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (testOrdersToRetest.Count > 0)
        {
            var dilution = testOrdersToRetest.Count == 1 ? request.InstrumentDilution : null; // Instrument dilution factor is only applicable if one test result is being retested
            await mediator.Send(new RetestRejectedResultsCommand(testOrdersToRetest, dilution), cancellationToken);
        }

        await mediator.Publish(new TestResultStatusChangedEvent(modifiedTestResults), cancellationToken);

        // Update task card state for each affected instrument
        foreach (var instrument in instrumentsToUpdate)
        {
            await ResultHelper.UpdateTaskCardState(mediator, dbContext, instrument, new[] { TaskCardType.ResultsIssue }, cancellationToken);
        }

        // Return the number of modified test results
        return modifiedTestResults.Count;
    }
}