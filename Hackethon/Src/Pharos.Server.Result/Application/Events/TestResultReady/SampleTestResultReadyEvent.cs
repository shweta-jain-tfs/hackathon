using MediatR;
using Pharos.Server.Application.Result;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Commands;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Events.TestResultReady;

/// <summary>
/// This event is published when all test replicates in a test order have been received
/// and response and concentration values for all if them have been calculated.
/// </summary>
internal record SampleTestResultReadyEvent(SampleTestResult TestResult) : INotification;

internal class SampleTestResultReadyEventHandler(
    IResultDbContext dbContext,
    IResultCalculationService resultCalculationService,
    IMediator mediator) : TestResultReadyEventHandlerBase<SampleTestResult>(dbContext), INotificationHandler<SampleTestResultReadyEvent>
{
    public async Task Handle(SampleTestResultReadyEvent notification, CancellationToken cancellationToken)
    {

        DbContext.TestResults.Attach(notification.TestResult);

        await resultCalculationService.CalculateSummaryResult(notification.TestResult);

        var testResult = notification.TestResult;

        await SetTestResultStatus(testResult, cancellationToken);

        if (testResult.Status is TestResultStatus.Flagged or TestResultStatus.Aborted
            && testResult.AnalyticalRunId is { } analyticalRunId)
        {
            var analyticalRun = await DbContext.AnalyticalRuns
                .FirstOrDefaultAsync(run => run.Id == analyticalRunId, cancellationToken);

            analyticalRun?.AddDomainEvent(new UpdateAnalyticalRunQualityIssue(
                analyticalRun,
                CurveIssue: null,
                CurveControlIssue: null,
                QualityControlIssue: null,
                TestIssue: AnalyticalRunIssue.Warning));
        }

        testResult.AddDomainEvent(new TestResultUpdatedEvent(testResult));
        testResult.AddDomainEvent(new TestResultStatusChangedEvent([testResult]));

        await DbContext.SaveChangesAsync(cancellationToken);

        if (AutoApprove() && QualityControlIsOk())
        {
            await mediator.Send(new ApproveTestResultsCommand(new[] { testResult.TestOrderId }, false), cancellationToken);
        }
    }

    /// <summary>
    /// TODO: Check settings if test should be AutoApproved
    /// </summary>
    /// <returns>true if test should be approved, otherwise false </returns>
    private static bool AutoApprove()
    {
        return false;
    }

    /// <summary>
    /// Checks QC for the results
    /// </summary>
    /// <returns>true if QC is ok, otherwise false </returns>
    private static bool QualityControlIsOk()
    {
        return true;
    }
}
