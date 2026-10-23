using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Result.Application.Events;
using Pharos.Server.Result.Application.Events.TestResultReady;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.TestResult;

namespace Pharos.Server.Result.Application.Commands.ProcessAssayResult;

internal class ProcessQualityControlResultCommand : BaseProcessAssayResultCommand;

internal class ProcessQualityControlResultCommandHandler(
    IResultDbContext dbContext,
    IMethodsService methodsService,
    ILogger<ProcessQualityControlResultCommandHandler> logger,
    IResultCalculationService resultCalculationService,
    IResponseCalculationService responseCalculationService,
    IResultFormatter resultFormatter,
    IMediator mediator,
    IReagentBarcodeService barcodeService)
    : ProcessAssayResultCommandBaseHandler<ProcessQualityControlResultCommand, QualityControlTestResult, QualityControlTestReplicate>(dbContext, methodsService, logger, resultCalculationService, responseCalculationService, resultFormatter, mediator, barcodeService)
{
    private bool _lastReplicateInGroup;

    protected override QualityControlTestResult CreateTestResult(string instrumentNumber, AssayResult message)
    {
        var result = base.CreateTestResult(instrumentNumber, message);
        result.TubeId = message.Sample.TubeId;
        var task = InitializeNewTestResult(result, message);
        task.Wait();

        result.AddDomainEvent(new TestResultCreatedEvent(result));

        return result;
    }

    protected override void InitializeCurrentReplicate(QualityControlTestReplicate replicate, AssayResult message)
    {
        _lastReplicateInGroup = message.Replicate.TestOrdersInGroup == message.Replicate.TestOrderNumber
                                && message.Replicate.ReplicatesInTestOrder == message.Replicate.ReplicateNumber;

        var sampleReagent = message.Reagents.FirstOrDefault(x => x.ReagentType == EnumReagentType.QualityControlCartridge);
        if (sampleReagent is null)
        {
            return;
        }

        replicate.QualityControlBarcode = sampleReagent.ReagentBarcode;
    }

    protected override Task<QualityControlTestResult?> GetTestResultWithExistingReplicates(string instrumentNumber, AssayResult message, CancellationToken cancellationToken)
    {
        var groupId = Guid.Parse(message.Replicate.GroupId);

        return DbContext.QualityControlTestResults
            .Where(x => x.GroupId == groupId && x.QualityControlName == message.Sample.SampleId)
            .Include(x => x.Replicates)!
            .ThenInclude(y => y.TestReplicateReagents)
            .FirstOrDefaultAsync(cancellationToken);
    }

    protected override void OnAllReplicatesHaveResponse(QualityControlTestResult testResult)
    {
        base.OnAllReplicatesHaveResponse(testResult);

        testResult.AddDomainEvent(new TestResultUpdatedEvent(testResult));

        if (AnalyticalRun?.CanCalculateConcentrations != true)
        {
            // If calculations are not committed, this means that replicates have no concentration.
            return;
        }

        // Send ResultGroupComplete if this was the last replicate in last group
        if (_lastReplicateInGroup)
        {
            CurrentReplicate!.AddDomainEvent(new QualityControlResultGroupReadyEvent(testResult.InstrumentNumber, testResult.GroupId));
        }
    }

    private async Task InitializeNewTestResult(QualityControlTestResult testResult, AssayResult message)
    {
        testResult.QualityControlName = message.Sample.SampleId;
        testResult.GroupId = Guid.Parse(message.Replicate.GroupId);
        testResult.GroupSize = message.Replicate.TestOrdersInGroup;

        var sampleReagent = message.Reagents.FirstOrDefault(x => x.ReagentType == EnumReagentType.QualityControlCartridge);
        if (sampleReagent is null)
        {
            return;
        }

        var qcRange = await Mediator.Send(new GetQualityControlLotRangeQuery(message.MethodName, message.TestName, testResult.QualityControlName, sampleReagent.ReagentBarcode));

        if (qcRange != null)
        {
            testResult.QualityControlType = qcRange.QualityControlType;
            testResult.ExpectedConcentration = qcRange.ExpectedConcentration;
            testResult.ExpectedStdDev = qcRange.ExpectedStdDev;
            testResult.StdDevLimit = qcRange.StdDevLimit;

            testResult.MinValue = qcRange.RangeMin;
            testResult.MaxValue = qcRange.RangeMax;
            testResult.ExcludeMaxValue = qcRange.ExcludeRangeMax;
            testResult.ExcludeMinValue = qcRange.ExcludeRangeMin;
        }
        else
        {
            Logger.LogError("ProcessAssayResult: Received QC result for unknown QC. QC name: {QcName} Test name: {TestName}",
                testResult.QualityControlName, message.TestName);
        }
    }
}