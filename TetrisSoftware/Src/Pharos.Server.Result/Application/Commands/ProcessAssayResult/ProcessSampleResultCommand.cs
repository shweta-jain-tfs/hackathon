using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Application.Result;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Events;
using Pharos.Server.Result.Application.Events.TestResultReady;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;
using Tetris.Protocol.Pharos.TestResult;

namespace Pharos.Server.Result.Application.Commands.ProcessAssayResult;

internal class ProcessSampleResultCommand : BaseProcessAssayResultCommand;

internal class ProcessSampleResultCommandHandler(
    IResultDbContext dbContext,
    IMethodsService methodsService,
    ILogger<ProcessSampleResultCommandHandler> logger,
    IResultCalculationService resultCalculationService,
    IResponseCalculationService responseCalculationService,
    IResultFormatter resultFormatter,
    IMediator mediator,
    IReagentBarcodeService barcodeService)
    : ProcessAssayResultCommandBaseHandler<ProcessSampleResultCommand, SampleTestResult, SampleTestReplicate>(dbContext, methodsService, logger, resultCalculationService, responseCalculationService, resultFormatter, mediator, barcodeService)
{

    protected override async Task CalculateReplicateResponse(string instrumentNumber, AssayResult requestMessage, SampleTestReplicate replicate)
    {
        await base.CalculateReplicateResponse(instrumentNumber, requestMessage, replicate);

        if (CurrentTestResult!.AllReplicatesAvailable && !CurrentTestResult.AllReplicatesHaveResponse)
        {
            // When there is no response (or calculations are not committed yet) the test result will stay
            // in the PendingBlank state for a while, so we need to propagate this status to the test order.
            replicate.AddDomainEvent(new TestResultStatusChangedEvent([CurrentTestResult]));
        }

        if (replicate.IsLowResponse)
        {
            replicate.TestReplicateErrors!.Add(new TestReplicateError
            {
                ErrorCode = 99, //temporary error code for low RU. 
            });

            AddQualityIssue(AnalyticalRunIssue.Warning);
        }
    }

    protected override SampleTestResult CreateTestResult(string instrumentNumber, AssayResult message)
    {
        var result = base.CreateTestResult(instrumentNumber, message);

        result.TestOrderId = Guid.Parse(message.Replicate.GroupId);
        result.RequestUid.SampleId = message.Sample.SampleId;

        var sampleDate = message.Sample.SampleDate?.ToDateTimeOffset().ToLocalTime() ?? DateTimeOffset.MinValue;

        result.RequestUid.RequestCreatedDate = sampleDate;

        result.AddDomainEvent(new TestResultCreatedEvent(result));

        return result;
    }

    protected override async Task<SampleTestResult?> GetTestResultWithExistingReplicates(string instrumentNumber, AssayResult message, CancellationToken cancellationToken)
    {
        var testResult = await DbContext.SampleTestResults.Where(x => x.TestOrderId == Guid.Parse(message.Replicate.GroupId))
            .Include(x => x.Replicates)!
            .ThenInclude(y => y.TestReplicateReagents)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (testResult != null && testResult.ResultType != TestResultType.Sample)
        {
            Logger.LogError("The test order type and the result type mismatch for the instrument replicate ID '{InstrumentReplicateId}'.", message.Replicate.ReplicateId);
            throw new InvalidOperationException();
        }

        return testResult;
    }

    protected override void InitializeCurrentReplicate(SampleTestReplicate replicate, AssayResult message)
    {
        CheckAbortedOrErrors(replicate);
        base.InitializeCurrentReplicate(replicate, message);
    }

    protected override void OnAllReplicatesHaveResponse(SampleTestResult testResult)
    {
        base.OnAllReplicatesHaveResponse(testResult);

        testResult.Status = TestResultStatus.PendingCalibration;

        if (AnalyticalRun?.CanCalculateConcentrations != true)
        {
            //Set status to aborted if any replicate is aborted regardless of the curve status.
            if (testResult.Replicates != null && testResult.Replicates.Any(x => x.IsAborted))
            {
                testResult.Status = TestResultStatus.Aborted;
            }

            // When there is no response (or calculations are not committed yet) the test result will stay in the
            // PendingCalibration state for a while, so we need to propagate this status to the test order.
            testResult.AddDomainEvent(new TestResultStatusChangedEvent([CurrentTestResult!]));

            // If calculations are not committed, this means that replicates have no concentration.
            return;
        }

        testResult.AddDomainEvent(new SampleTestResultReadyEvent(testResult));
    }

    private void AddQualityIssue(AnalyticalRunIssue testIssue)
    {
        AnalyticalRun?.AddDomainEvent(new UpdateAnalyticalRunQualityIssue(AnalyticalRun, null, null, null, testIssue));
    }

    private void CheckAbortedOrErrors(SampleTestReplicate replicate)
    {
        if (replicate.IsAborted || replicate.TestReplicateErrors is { Count: > 0 })
        {
            AddQualityIssue(AnalyticalRunIssue.Warning);
        }
    }
}