using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Localization;
using Pharos.Server.Result.Application.Events;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Commands;

public record RerunCurveControlCommand : IRequest
{
    public int AnalyticalRunId { get; set; }
}

internal class RerunCurveControlCommandHandler(IMediator mediator, IResultDbContext context, IInstrumentManager instrumentManager) : IRequestHandler<RerunCurveControlCommand>
{
    public async Task Handle(RerunCurveControlCommand request, CancellationToken cancellationToken)
    {
        var analyticalRun = await context.AnalyticalRuns
            .Include(x => x.TestResults)
            .SingleOrDefaultAsync(x => x.Id == request.AnalyticalRunId, cancellationToken);

        if (analyticalRun == null)
        {
            throw new ObjectGoneException(Translations.ObjectGone);
        }

        if (analyticalRun.TestResults.Any(x => x.Status == TestResultStatus.Approved))
        {
            return;
        }

        // Committed runs are frozen — curve-control events must not modify them.
        if (analyticalRun.CurveState == CurveState.Frozen)
        {
            return;
        }

        //Do not allow if instrument not in assay
        if (!instrumentManager.FindInstrument(analyticalRun.InstrumentNumber)?.IsInAssayMode ?? false)
        {
            return;
        }

        var conjugateLot = analyticalRun.ConjugateLotNumber ?? "";
        var carrierLot = analyticalRun.CarrierLotNumber;
        var serialNumber = analyticalRun.InstrumentNumber;
        var methodName = analyticalRun.MethodName;

        // Guard: do not re-order if we are already waiting for a curve or a curve control.
        if (analyticalRun.CurveState is CurveState.WaitingForNewCurve
                                           or CurveState.WaitingForCurveControl)
        {
            return;
        }

        // Guard: current-assay sourced curves should not enter the CC rerun waiting state.
        if (analyticalRun.CurveSource == CurveSource.CurveFromCurrentAssay)
        {
            return;
        }

        // Guard: current-assay sourced curves should not enter the CC rerun waiting state.
        if (analyticalRun.CurveSource == CurveSource.CurveFromCurrentAssay)
        {
            return;
        }

        //Send to instrument
        await mediator.Send(new OrderNewCalCcCommand(serialNumber, false, methodName, conjugateLot,
            carrierLot ?? string.Empty, string.Empty), cancellationToken);

        // A new CC has been ordered — keep the existing CurveSource (we are not ordering a new calibration curve).
        // Block calculations until the incoming CC result is judged OK.
        analyticalRun.CurveState = CurveState.WaitingForCurveControl;

        //Reset curve control issue
        analyticalRun.AddDomainEvent(new UpdateAnalyticalRunQualityIssue(analyticalRun, null, AnalyticalRunIssue.NoIssue, null, null));

        //Update analyticalRun
        analyticalRun.CurveControlTotalJudgment = CurveControlJudgmentResult.NotJudged;
        analyticalRun.AddDomainEvent(new CurveControlChartUpdatedEvent(serialNumber, methodName));

        //Save changes
        await context.SaveChangesAsync(cancellationToken);
    }
}
