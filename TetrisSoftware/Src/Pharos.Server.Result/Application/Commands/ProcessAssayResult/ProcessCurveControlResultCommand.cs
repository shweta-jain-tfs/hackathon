using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Result.Application.Events.TestResultReady;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.TestResult;

namespace Pharos.Server.Result.Application.Commands.ProcessAssayResult;

internal class ProcessCurveControlResultCommand : BaseProcessAssayResultCommand;

internal class ProcessCurveControlResultCommandHandler(
    IResultDbContext dbContext,
    IMethodsService methodsService,
    ILogger<ProcessCurveControlResultCommandHandler> logger,
    IResultCalculationService resultCalculationService,
    IResponseCalculationService responseCalculationService,
    IResultFormatter resultFormatter,
    IMediator mediator,
    IReagentBarcodeService barcodeService)
    : ProcessAssayResultCommandBaseHandler<ProcessCurveControlResultCommand, CurveControlTestResult, CurveControlTestReplicate>(dbContext, methodsService, logger, resultCalculationService, responseCalculationService, resultFormatter, mediator, barcodeService)
{
    private bool _lastReplicateInGroup;

    protected override CurveControlTestResult CreateTestResult(string instrumentNumber, AssayResult message)
    {
        var result = base.CreateTestResult(instrumentNumber, message);
        result.CurveControlPointName = message.Sample.SampleId;
        result.GroupId = Guid.Parse(message.Replicate.GroupId);
        result.GroupSize = message.Replicate.TestOrdersInGroup;

        var task = MethodsService.GetMethod(
            message.MethodName,
            m => m.CurveControls
                .Where(p => p.CurveControlName == result.CurveControlPointName)
                .Select(p => p.ExpectedConcentration)
                .FirstOrDefault());

        task.WaitAsync(CancellationToken.None);
        result.ExpectedConcentration = task.Result;

        return result;
    }

    protected override void InitializeCurrentReplicate(CurveControlTestReplicate replicate, AssayResult message)
    {
        _lastReplicateInGroup = message.Replicate.TestOrdersInGroup == message.Replicate.TestOrderNumber
                                && message.Replicate.ReplicatesInTestOrder == message.Replicate.ReplicateNumber;

        var sampleReagent = message.Reagents.FirstOrDefault(x => x.ReagentType == EnumReagentType.CurveControlCartridge);
        if (sampleReagent is null)
        {
            return;
        }

        replicate.CurveControlBarcode = sampleReagent.ReagentBarcode;
    }

    protected override Task<CurveControlTestResult?> GetTestResultWithExistingReplicates(string instrumentNumber, AssayResult message, CancellationToken cancellationToken)
    {
        var groupId = Guid.Parse(message.Replicate.GroupId);

        return DbContext.CurveControlTestResults
            .Where(x => x.GroupId == groupId && x.CurveControlPointName == message.Sample.SampleId)
            .Include(x => x.Replicates)!
            .ThenInclude(y => y.TestReplicateReagents)
            .FirstOrDefaultAsync(cancellationToken);
    }

    protected override void OnAllReplicatesHaveResponse(CurveControlTestResult testResult)
    {
        base.OnAllReplicatesHaveResponse(testResult);

        if (_lastReplicateInGroup)
        {
            CurrentReplicate!.AddDomainEvent(new CurveControlResultGroupReadyEvent(testResult.InstrumentNumber, testResult.GroupId, testResult.MethodName));
        }
    }
}