using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Result;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Events;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Commands;

public record RerunCalibratorCurveCommand(int AnalyticalRunId) : IRequest;

internal class RerunCalibratorCurveCommandHandler(IMediator mediator, IResultDbContext context, IInstrumentManager instrumentManager) : IRequestHandler<RerunCalibratorCurveCommand>
{
    public async Task Handle(RerunCalibratorCurveCommand request, CancellationToken cancellationToken)
    {
        var analyticalRun = await context.AnalyticalRuns
            .Include(x => x.TestResults)
            .SingleOrDefaultAsync(x => x.Id == request.AnalyticalRunId, cancellationToken);
        if (analyticalRun == null)
        {
            return;
        }

        if (analyticalRun.TestResults.Any(x => x.Status == TestResultStatus.Approved))
        {
            return;
        }

        // No non-exception flow may change curve assignment after calculation is committed.
        if (analyticalRun.CurveState == CurveState.Frozen)
        {
            return;
        }

        //Do not allow if this assay has ended
        if (!instrumentManager.FindInstrument(analyticalRun.InstrumentNumber)?.IsInAssayMode ?? false)
        {
            return;
        }

        //Extract info
        var conjugateLot = analyticalRun.ConjugateLotNumber ?? "";
        var carrierLot = analyticalRun.CarrierLotNumber ?? "";
        var serialNumber = analyticalRun.InstrumentNumber;
        var methodName = analyticalRun.MethodName;


        //Remove the current curve, if it exists. 
        var currentCurves = await context.CurrentCurves
            .Where(cc => cc.MethodName == methodName
                         && cc.ConjugateLotNumber == conjugateLot
                         && cc.Curve.InstrumentNumber == serialNumber).SingleOrDefaultAsync(cancellationToken);

        if (currentCurves != null)
        {
            context.CurrentCurves.Remove(currentCurves);
            await context.SaveChangesAsync(cancellationToken);
        }

        //Send to instrument
        await mediator.Send(new OrderNewCalCcCommand(serialNumber, true, methodName,
            conjugateLot, carrierLot, string.Empty), cancellationToken);

        // Mark that a new curve has been ordered and calculations are blocked until it arrives
        analyticalRun.CurveState = CurveState.WaitingForNewCurve;
        analyticalRun.CurveControlTotalJudgment = CurveControlJudgmentResult.NotJudged;

        //Reset calibrator issue
        analyticalRun.AddDomainEvent(new UpdateAnalyticalRunQualityIssue(analyticalRun, AnalyticalRunIssue.NoIssue, AnalyticalRunIssue.NoIssue, null, null));

        //Save changes
        await context.SaveChangesAsync(cancellationToken);

        analyticalRun.AddDomainEvent(new CalibrationCurveChartUpdatedEvent(analyticalRun.InstrumentNumber, analyticalRun.MethodName));
        analyticalRun.AddDomainEvent(new CurveControlChartUpdatedEvent(analyticalRun.InstrumentNumber, analyticalRun.MethodName));

        //Reset results in this analytical run
        var sampleTestResults = await context.SampleTestResults
            .Include(x => x.Replicates)
            .Where(x => x.AnalyticalRunId == request.AnalyticalRunId && x.ApprovalStatus == ResultApprovalStatus.NotApproved)
            .ToListAsync(cancellationToken);

        foreach (var result in sampleTestResults)
        {
            var replicates = result.Replicates;
            if (replicates != null)
            {
                foreach (var rep in replicates)
                {
                    rep.Concentration = null;
                    rep.ConcentrationAsText = null;
                    rep.ConcentrationStatus = ConcentrationStatus.NotAvailable;
                }

                result.MeanConcentration = null;
                result.MeanConcentrationAsText = null;
                result.MeanConcentrationStatus = ConcentrationStatus.NotAvailable;
                result.PercentCvConcentration = null;
                result.Status = TestResultStatus.PendingCalibration;
                result.MeanClass = null;
                result.MeanCutOff = null;
                result.MeanQuotient = null;
                result.MeanQuotientAsText = null;
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        await mediator.Publish(new TestResultStatusChangedEvent(sampleTestResults), cancellationToken);
    }
}