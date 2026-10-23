using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Services;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Events;

internal record NewCurve(Curve Curve, int AnalyticalRunId) : INotification;

internal sealed class NewCurveHandler(
    IResultDbContext dbContext,
    IMethodsService methodsService,
    IMediator mediator,
    IPublisher publisher,
    ILogger<NewCurveHandler> logger)
    : INotificationHandler<NewCurve>
{
    public async Task Handle(NewCurve notification, CancellationToken cancellationToken)
    {
        var curve = notification.Curve;
        var analyticalRunid = notification.AnalyticalRunId;

        // Only OK curves propagate to the next assay via CurrentCurve.
        // Flagged and NotOK curves stop propagation but still update the current run's state.
        if (curve.Judgment is CurveJudgment.OK)
        {
            await SetCurveAsCurrentCurve(curve);
        }
        else
        {
            await CleanupExistingCurrentCurves(curve);
        }

        var curveAssignedToRun = await SetCalculationCurveForAnalyticalRun(curve, analyticalRunid);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (curveAssignedToRun)
        {
            // Update the task card state for the instrument after the curve has been assigned to the analytical run.
            await ResultHelper.UpdateTaskCardState(mediator, dbContext, curve.InstrumentNumber, new[] { TaskCardType.CalibrationIssue }, cancellationToken);
        }

        if (curve.Judgment == CurveJudgment.OK)
        {
            await publisher.Publish(new ValidCurvesUpdatedEvent(curve.InstrumentNumber), cancellationToken);
        }
    }

    private async Task SetCurveAsCurrentCurve(Curve curve)
    {
        await CleanupExistingCurrentCurves(curve);

        //add the new curve as current curve
        var currentCurve = new CurrentCurve
        {
            ConjugateLotNumber = curve.ConjugateLotNumber,
            ImmunoCapLotNumber = curve.ImmunoCapLotNumber,
            MethodName = curve.MethodName,
            CurveId = curve.Id
        };
        await dbContext.CurrentCurves.AddAsync(currentCurve);
    }

    private async Task CleanupExistingCurrentCurves(Curve curve)
    {
        // Check if this method uses connection between ImmunoCap and conjugate lot numbers
        var useCarrierLotNumber = await ConnectImmunoCapAndConjugateLotNumbers(curve.MethodName);

        var existingCurrentCurves = dbContext.CurrentCurves.Where(cc =>
            cc.Curve.InstrumentNumber == curve.InstrumentNumber &&
            cc.MethodName == curve.MethodName &&
            cc.ConjugateLotNumber == curve.ConjugateLotNumber);

        if (useCarrierLotNumber)
        {
            existingCurrentCurves = existingCurrentCurves.Where(x => x.ImmunoCapLotNumber == curve.ImmunoCapLotNumber);
        }

        //remove older current curves that matches the key
        dbContext.CurrentCurves.RemoveRange(existingCurrentCurves);
    }

    /// <summary>
    /// Check if the method has connection with lot numbers on conjugate AND ImmunoCap
    /// </summary>
    /// <param name="methodName">Method name</param>
    /// <returns>True if conjugate and ImmunoCap lot numbers are connected</returns>
    private async Task<bool> ConnectImmunoCapAndConjugateLotNumbers(string methodName)
    {
        return await methodsService.GetMethod(methodName, m => m.AnalyticalRun.RunCalibrationOnCarrierLotChange);
    }

    /// <summary>
    /// Assign the new curve to the analytical run if it is not committed.
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="analyticalRunid"></param>
    /// <returns></returns>
    private async Task<bool> SetCalculationCurveForAnalyticalRun(Curve curve, int analyticalRunid)
    {
        var analyticalRun = dbContext.AnalyticalRuns.FirstOrDefault(ar => ar.Id == analyticalRunid);

        if (analyticalRun == null || analyticalRun.CurveState == CurveState.Frozen)
        {
            return false;
        }

        analyticalRun.CurveId = curve.Id;

        // This curve is always from the current instrument run at this point.
        // CurveFromPreviousAssay is only set at run-start time via StartAnalyticalRunCommand.
        analyticalRun.CurveSource = CurveSource.CurveFromCurrentAssay;

        // Flagged/NotOK curves block calculation — set Invalid.
        // OK curves always reset to Valid so CanCalculateConcentrations becomes true.
        // CalculationCommitted is already guarded at the top of this method.
        if (curve.Judgment != CurveJudgment.OK)
        {
            logger.LogInformation("Setting AnalyticalRun {AnalyticalRunId} CurveState to Invalid due to new curve with judgment {CurveJudgment}", analyticalRun.Id, curve.Judgment);
            analyticalRun.CurveState = CurveState.Invalid;
        }
        else
        {
            analyticalRun.CurveState = CurveState.Valid;
        }

        analyticalRun.AddDomainEvent(new AnalyticalRunCalculationCurveUpdated(analyticalRun.Id));

        var curveIssue = curve.Judgment switch
        {
            CurveJudgment.OK => AnalyticalRunIssue.NoIssue,
            CurveJudgment.Flagged => AnalyticalRunIssue.Warning,
            _ => AnalyticalRunIssue.Error
        };

        analyticalRun.AddDomainEvent(new UpdateAnalyticalRunQualityIssue(analyticalRun, curveIssue, CurveControlIssue: null, QualityControlIssue: null, TestIssue: null));

        return true;
    }

}
