using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Events.TestResultReady;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.TestResult;

namespace Pharos.Server.Result.Application.Commands.ProcessAssayResult;

internal class ProcessCalibratorResultCommand : BaseProcessAssayResultCommand;

internal class ProcessCalibratorResultCommandHandler(
    IResultDbContext dbContext,
    IMethodsService methodsService,
    ILogger<ProcessCalibratorResultCommandHandler> logger,
    IResultCalculationService resultCalculationService,
    IResponseCalculationService responseCalculationService,
    IResultFormatter resultFormatter,
    IMediator mediator,
    IReagentBarcodeService barcodeService)
    : ProcessAssayResultCommandBaseHandler<ProcessCalibratorResultCommand, CalibratorTestResult, CalibratorTestReplicate>(dbContext, methodsService, logger, resultCalculationService, responseCalculationService, resultFormatter, mediator, barcodeService)
{
    private Curve? _curve;
    private bool _lastReplicateInGroup;

    protected override bool ReplicateConcentrationNeeded => false;

    protected override CalibratorTestResult CreateTestResult(string instrumentNumber, AssayResult message)
    {
        var result = base.CreateTestResult(instrumentNumber, message);
        result.CalibratorPointName = message.Sample.SampleId;
        result.GroupId = Guid.Parse(message.Replicate.GroupId);
        result.GroupSize = message.Replicate.TestOrdersInGroup;

        var task = MethodsService.GetMethod(
            message.MethodName,
            m => m.CalibrationCurve.CalibrationPoints
                .Where(p => p.CalibratorName == result.CalibratorPointName)
                .Select(p => p.ExpectedConcentration)
                .FirstOrDefault());

        task.WaitAsync(CancellationToken.None);
        result.ExpectedConcentration = task.Result;

        _curve!.TestResults!.Add(result);

        return result;
    }

    protected override void InitializeCurrentReplicate(CalibratorTestReplicate replicate, AssayResult message)
    {
        _lastReplicateInGroup = message.Replicate.TestOrdersInGroup == message.Replicate.TestOrderNumber
                                && message.Replicate.ReplicatesInTestOrder == message.Replicate.ReplicateNumber;

        var sampleReagent = message.Reagents.FirstOrDefault(x => x.ReagentType == EnumReagentType.CalibratorCartridge);
        if (sampleReagent is null)
        {
            return;
        }

        replicate.CalibratorBarcode = sampleReagent.ReagentBarcode;
    }

    protected override async Task<CalibratorTestResult?> GetTestResultWithExistingReplicates(string instrumentNumber, AssayResult message, CancellationToken cancellationToken)
    {
        var groupId = Guid.Parse(message.Replicate.GroupId);
        var runId = Guid.Parse(message.Run.AssayRunId);

        _curve = await CreateCurveIfNotExists(groupId, message.MethodName, message.TimeOfMeasurement.ToDateTimeOffset().ToLocalTime(), runId, cancellationToken);
        _curve.TestResults ??= new List<CalibratorTestResult>();

        return await DbContext.CalibratorTestResults
            .Where(x => x.GroupId == groupId && x.CalibratorPointName == message.Sample.SampleId)
            .Include(x => x.Replicates)!
            .ThenInclude(y => y.TestReplicateReagents)
            .FirstOrDefaultAsync(cancellationToken);
    }

    protected override void OnAllReplicatesHaveResponse(CalibratorTestResult testResult)
    {
        base.OnAllReplicatesHaveResponse(testResult);

        if (_lastReplicateInGroup)
        {
            CurrentReplicate!.AddDomainEvent(new CalibratorResultGroupReadyEvent(testResult.InstrumentNumber, testResult.GroupId, testResult.MethodName));
        }
    }

    /// <summary>
    /// Check if a calibration curve exist and creates it if it doesn't
    /// </summary>
    private async Task<Curve> CreateCurveIfNotExists(Guid groupId, string methodName, DateTimeOffset timeOfMeasurement, Guid runId, CancellationToken cancellationToken)
    {
        var curve = await DbContext.Curves.FirstOrDefaultAsync(c => c.InstrumentNumber == AnalyticalRun!.InstrumentNumber && c.GroupId == groupId, cancellationToken);

        if (curve == null)
        {
            var days = await MethodsService.GetCalibrationCurveDaysValidity(methodName);
            var firstDayToUse = DateOnly.FromDateTime(timeOfMeasurement.Subtract(TimeSpan.FromDays(28)).DateTime);
            var lastDayToUse = DateOnly.FromDateTime(timeOfMeasurement.AddDays(days - 1).DateTime);

            curve = new Curve
            {
                InstrumentNumber = AnalyticalRun!.InstrumentNumber,
                GroupId = groupId,
                InstrumentRunId = runId,

                MethodName = methodName,
                ConjugateLotNumber = ConjugateLotNumber,
                ImmunoCapLotNumber = CarrierLotNumber,

                MeasurementDateTime = timeOfMeasurement,
                FirstDayToUse = firstDayToUse,
                LastDayToUse = lastDayToUse,

                CurveControlDisabledFlag = false,
                LastCurveControlsAborted = false,

                CalculationType = await GetCalculationType(methodName)
            };
            await DbContext.Curves.AddAsync(curve, cancellationToken);

            // New curve arriving — update the run if it is still in a mutable waiting state.
            // Frozen runs are locked and must not be changed (rule 3).
            if (AnalyticalRun!.CurveState is not CurveState.Frozen)
            {
                AnalyticalRun.CurveSource = CurveSource.CurveFromCurrentAssay;
                // Keep WaitingForNewCurve until CalibratorResultGroupReadyEvent determines curve judgment.
                AnalyticalRun.CurveState = CurveState.WaitingForNewCurve;
            }
        }

        return curve;
    }

    /// <summary>
    /// Return calculation type for a method
    /// </summary>
    /// <param name="methodName">Method name</param>
    /// <returns>Calculation type</returns>
    private async Task<CalculationType> GetCalculationType(string methodName)
    {
        var calculationType = await MethodsService.GetMethod(methodName, m => m.Calculation.CalculationType);

        return calculationType;
    }
}
