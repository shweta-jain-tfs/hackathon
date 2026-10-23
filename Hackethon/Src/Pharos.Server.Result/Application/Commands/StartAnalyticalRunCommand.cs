using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Application.Events;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Services;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Commands;

internal record StartAnalyticalRunCommand : IRequest<AnalyticalRun>
{
    public Guid AnalyticalRunId { get; init; }
    public Guid RunId { get; init; }
    public string InstrumentNumber { get; init; } = default!;
    public DateTimeOffset StartDateTime { get; init; }
    public string MethodName { get; init; } = default!;
    public string ConjugateLotNumber { get; init; } = default!;
    public string CarrierLotNumber { get; init; } = default!;
    public string? CustomRunId { get; init; }
}

internal class StartNewAnalyticalRunCommandHandler(
    IResultDbContext dbContext,
    ILogger<StartNewAnalyticalRunCommandHandler> logger,
    IMethodsService methodsService,
    IMediator mediator,
    IDateTime dateTime)
    : IRequestHandler<StartAnalyticalRunCommand, AnalyticalRun>
{
    public async Task<AnalyticalRun> Handle(StartAnalyticalRunCommand request, CancellationToken cancellationToken)
    {
        AnalyticalRun analyticalRun;
        try
        {
            var otherAnalyticalRuns = await SetOtherProcessingAnalyticalRunsAsReady(request.InstrumentNumber, request.MethodName, cancellationToken);
            var item = await GetCurveIdForNewAnalyticalRun(request, cancellationToken);

            analyticalRun = new AnalyticalRun
            {
                InstrumentNumber = request.InstrumentNumber,
                InstrumentAnalyticalRunId = request.AnalyticalRunId,
                StartDateTime = request.StartDateTime,
                MethodName = request.MethodName,
                ConjugateLotNumber = request.ConjugateLotNumber,
                CarrierLotNumber = request.CarrierLotNumber,
                CmrRunId = request.CustomRunId,
                CurveId = item.CurveId,
                InstrumentRunId = request.RunId
            };

            await SetCurveSourceAndCalculationState(analyticalRun, cancellationToken);

            await dbContext.AnalyticalRuns.AddAsync(analyticalRun, cancellationToken);
            analyticalRun.AddDomainEvent(new AnalyticalRunCreatedEvent(analyticalRun));

            if (item.CurrentCurvesUpdated)
            {
                analyticalRun.AddDomainEvent(new ValidCurvesUpdatedEvent(request.InstrumentNumber));
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var analyticalRunId in otherAnalyticalRuns)
            {
                await ResultHelper.UpdateApprovalStatusForAnalyticalRun(dbContext, analyticalRunId, cancellationToken);
            }
            await ResultHelper.UpdateTaskCardState(mediator, dbContext, analyticalRun.InstrumentNumber, new[] { TaskCardType.CalibrationIssue, TaskCardType.QCIssue, TaskCardType.ResultsIssue }, cancellationToken);
        }
        catch (DbUpdateException)
        {
            logger.LogWarning("A concurrent database transaction already created the analytical run AnalyticalRunId={AnalyticalRunId}, fetching it from the database...", request.AnalyticalRunId);

            analyticalRun = await dbContext.AnalyticalRuns.FirstAsync(a => a.InstrumentNumber == request.InstrumentNumber && a.InstrumentAnalyticalRunId == request.AnalyticalRunId, cancellationToken);
        }
        return analyticalRun;
    }

    private async Task SetCurveSourceAndCalculationState(AnalyticalRun analyticalRun, CancellationToken cancellationToken)
    {
        var instrumentRun = analyticalRun.InstrumentRunId;
        var curveId = analyticalRun.CurveId;
        var methodName = analyticalRun.MethodName;

        if (curveId == null)
        {
            // No curve found in CurrentCurves or in the current run — waiting for one to arrive.
            // Invalid is reserved for runs blocked after two failed CCs; Missing at run-start
            // is a natural "not yet calibrated" state, so use WaitingForNewCurve.
            analyticalRun.CurveSource = CurveSource.Missing;
            analyticalRun.CurveState = CurveState.WaitingForNewCurve;
            return;
        }

        var assignedCurve = await dbContext.Curves
            .Where(c => c.Id == curveId.Value)
            .Select(c => new { c.InstrumentRunId, c.Judgment })
            .FirstOrDefaultAsync(cancellationToken);

        if (assignedCurve?.InstrumentRunId == instrumentRun)
        {
            analyticalRun.CurveSource = CurveSource.CurveFromCurrentAssay;
            analyticalRun.CurveState = assignedCurve.Judgment == CurveJudgment.OK
                ? CurveState.Valid
                : CurveState.Invalid;
            return;
        }

        analyticalRun.CurveSource = CurveSource.CurveFromPreviousAssay;

        // Block calculation until an OK CC arrives in this run.
        logger.LogInformation("Curve from previous assay — waiting for curve control");
        analyticalRun.CurveState = CurveState.WaitingForCurveControl;

        // Transition stale waiting runs from other assay runs to Invalid.
        await TransitionStaleWaitingRunsToInvalid(analyticalRun.InstrumentNumber, methodName, instrumentRun, cancellationToken);
    }

    /// <summary>
    /// When a new assay run starts, any older analytical runs still in a waiting state
    /// (WaitingForCurveControl / WaitingForNewCurve) are transitioned to Invalid so the user
    /// can resolve them manually by selecting or accepting a curve.
    /// </summary>
    private async Task TransitionStaleWaitingRunsToInvalid(string instrumentNumber, string methodName, Guid currentRunId, CancellationToken cancellationToken)
    {
        var staleRuns = await dbContext.AnalyticalRuns
            .Where(ar => ar.InstrumentNumber == instrumentNumber
                         && ar.MethodName == methodName
                         && ar.InstrumentRunId != currentRunId
                         && (ar.CurveState == CurveState.WaitingForCurveControl
                             || ar.CurveState == CurveState.WaitingForNewCurve))
            .ToListAsync(cancellationToken);

        foreach (var staleRun in staleRuns)
        {
            logger.LogInformation("Transitioning stale analytical run {RunId} from {OldState} to Invalid", staleRun.Id, staleRun.CurveState);
            staleRun.CurveState = CurveState.Invalid;
        }
    }

    /// <summary>
    /// Set other analytical runs as Ready (only one analytical run per instrument and method can be processing...)
    /// </summary>
    private async Task<List<int>> SetOtherProcessingAnalyticalRunsAsReady(string instrumentNumber, string methodName, CancellationToken cancellationToken)
    {
        var processingAnalyticalRunIds = await (from ar in dbContext.AnalyticalRuns
                                                where ar.InstrumentNumber == instrumentNumber
                                                      && ar.MethodName == methodName
                                                      && ar.RunStatus == RunStatus.Processing
                                                select ar.Id).ToListAsync(cancellationToken);
        foreach (var arId in processingAnalyticalRunIds)
        {
            await SetAnalyticalRunAsReady(arId, cancellationToken);
        }

        return processingAnalyticalRunIds;
    }

    /// <summary>
    /// Set one analytical runs as Ready 
    /// </summary>
    /// <param name="arId">analytical run ID</param>
    /// <param name="cancellationToken"></param>
    private async Task SetAnalyticalRunAsReady(int arId, CancellationToken cancellationToken)
    {
        var analyticalRun = await dbContext.AnalyticalRuns.FirstOrDefaultAsync(ar => ar.Id == arId, cancellationToken);

        if (analyticalRun != null)
        {
            await ResultHelper.CleanupAnalyticalRun(analyticalRun, dbContext, dateTime, cancellationToken);
        }

        // TODO: Here Prime calls HandleAutomaticApprovalRun() and AnalyticalRun.UpdateApprovalStatus()...
    }

    /// <summary>
    /// Retrieve id of curve to use for calculations
    /// </summary>
    private async Task<(int? CurveId, bool CurrentCurvesUpdated)> GetCurveIdForNewAnalyticalRun(StartAnalyticalRunCommand request, CancellationToken cancellationToken)
    {
        var allowMonthlyCalibration = true;
        var connectImmunoCapAndConjugate = false;

        var analyticalRunSettings = await methodsService.GetMethod(request.MethodName, m =>
            new
            {
                m.AnalyticalRun.AllowMonthlyCalibration,
                ConnectImmunoCapAndConjugate = m.AnalyticalRun.RunCalibrationOnCarrierLotChange
            });

        if (analyticalRunSettings != null)
        {
            allowMonthlyCalibration = analyticalRunSettings.AllowMonthlyCalibration;
            connectImmunoCapAndConjugate = analyticalRunSettings.ConnectImmunoCapAndConjugate;
        }

        if (!allowMonthlyCalibration)
        {
            logger.LogInformation("Method using not simplified calibration, force new curve by setting curve id to null");
            return (null, false);
        }


        var item = await GetCurveIdFromCurrentCurves(request.InstrumentNumber, request.MethodName, request.ConjugateLotNumber, request.CarrierLotNumber, connectImmunoCapAndConjugate, cancellationToken);
        if (item.CurveId == null)
        {
            logger.LogInformation("No curve stored");
            return item;
        }

        // Curve flagged and new assay run
        if (!await CheckCurveJudgementAndAssayRun(request.RunId, item.CurveId, cancellationToken))
        {
            logger.LogInformation("The curve is flagged and new assay run used: Curve not usable for this analytical run");
            return (null, item.CurrentCurvesUpdated);
        }

        return item;
    }

    /// <summary>
    /// Check if curve is flagged and a new assay run is used
    /// </summary>
    /// <param name="runId">Assay run id</param>
    /// <param name="curveId">Curve id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if curve can be used</returns>
    private async Task<bool> CheckCurveJudgementAndAssayRun(Guid runId, int? curveId, CancellationToken cancellationToken)
    {
        var curveInfo =
            await dbContext
                .Curves
                .Where(c => c.Id == curveId)
                .Select(c => new
                {
                    c.Judgment,
                    c.InstrumentRunId
                })
                .FirstOrDefaultAsync(cancellationToken);

        if (curveInfo == null)
        {
            return false;
        }

        return (curveInfo.Judgment != CurveJudgment.Flagged && curveInfo.Judgment != CurveJudgment.NotOK) || curveInfo.InstrumentRunId == runId;
    }

    /// <summary>
    /// Get the curve ID for curve to be used for calculations
    /// </summary>
    /// <param name="instrumentNumber"></param>
    /// <param name="methodName">Method name</param>
    /// <param name="conjugateLotNumber"></param>
    /// <param name="connectImmunoCapAndConjugate">Connect ImmunoCap and conjugate lot number</param>
    /// <param name="cancellationToken"></param>
    /// <param name="immunoCapLotNumber"></param>
    /// <returns>Curve ID</returns>
    private async Task<(int? CurveId, bool CurrentCurvesUpdated)> GetCurveIdFromCurrentCurves(string instrumentNumber, string methodName, string conjugateLotNumber, string immunoCapLotNumber, bool connectImmunoCapAndConjugate, CancellationToken cancellationToken)
    {
        int? curveId = null;
        CurrentCurve? currentCurve = null;
        var currentCurvesUpdated = false;

        if (connectImmunoCapAndConjugate)
        {
            currentCurve = await dbContext.CurrentCurves.Where(cc =>
                cc.MethodName == methodName &&
                cc.Curve.InstrumentNumber == instrumentNumber &&
                cc.ConjugateLotNumber == conjugateLotNumber &&
                cc.ImmunoCapLotNumber == immunoCapLotNumber).FirstOrDefaultAsync(cancellationToken);
        }
        else
        {
            var curveList = await dbContext.CurrentCurves.Where(cc =>
                cc.MethodName == methodName &&
                cc.Curve.InstrumentNumber == instrumentNumber &&
                cc.ConjugateLotNumber == conjugateLotNumber).ToListAsync(cancellationToken);

            if (curveList.Count == 0)
            {
                // OK
            }
            else if (curveList.Count == 1)
            {
                currentCurve = curveList[0];
            }
            else
            {
                // If there were more than 1 curve stored (i.e. previously used "Connect ImmunoCap and conjugate lot number", and now has disabled it)
                // we need to treat it as no stored one, since we do not know which one is "best"
                logger.LogInformation("Get CurrentCurve, found {CurveListCount} matches for Method = {MethodName}, Instrument = {Instrument}, Conjugate = {ConjugateLotNumber}",
                    curveList.Count, methodName, instrumentNumber, conjugateLotNumber);

                currentCurvesUpdated = await ClearCurrentCurves(instrumentNumber, methodName, conjugateLotNumber, cancellationToken);
            }
        }

        if (currentCurve != null)
        {
            curveId = currentCurve.CurveId;
        }

        return (curveId, currentCurvesUpdated);
    }

    /// <summary>
    /// Clear some current curves
    /// </summary>
    /// <param name="instrumentNumber"></param>
    /// <param name="methodName">Method name</param>
    /// <param name="conjugateLotNumber">Conjugate lot number</param>
    /// <param name="cancellationToken"></param>
    private async Task<bool> ClearCurrentCurves(string instrumentNumber, string methodName, string? conjugateLotNumber, CancellationToken cancellationToken)
    {
        var currentCurveList = await dbContext.CurrentCurves.Where(cc =>
            cc.MethodName == methodName &&
            cc.Curve.InstrumentNumber == instrumentNumber &&
            cc.ConjugateLotNumber == conjugateLotNumber).ToListAsync(cancellationToken);
        if (currentCurveList.Count > 0)
        {
            dbContext.CurrentCurves.RemoveRange(currentCurveList);
            return true;
        }

        return false;
    }
}
