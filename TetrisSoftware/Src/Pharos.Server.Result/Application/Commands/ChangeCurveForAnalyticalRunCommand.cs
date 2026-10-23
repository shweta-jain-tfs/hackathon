using MediatR;
using Pharos.Server.Application.Security;
using Pharos.Server.Result.Application.Events;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Commands;

public record ChangeCurveForAnalyticalRunCommand(int SelectedCurveId, int AnalyticalRunId) : IRequest;

internal class ChangeCurveForAnalyticalRunCommandHandler(IResultDbContext context, IAuthorizationService authorizationService) : IRequestHandler<ChangeCurveForAnalyticalRunCommand>
{
    public async Task Handle(ChangeCurveForAnalyticalRunCommand request, CancellationToken cancellationToken)
    {
        var analyticalRun = await context.AnalyticalRuns
            .Include(x => x.TestResults)
            .Include(x => x.CalculationCurve)
            .SingleOrDefaultAsync(x => x.Id == request.AnalyticalRunId, cancellationToken);

        if (analyticalRun == null)
        {
            return;
        }

        if (analyticalRun.CurveState == CurveState.Frozen && !authorizationService.RndMode)
        {
            // Do not allow change curve when calculation is in progress
            return;
        }

        var curve = await context.Curves
            .Where(x => x.Id == request.SelectedCurveId).SingleAsync(cancellationToken);

        if (analyticalRun.CurveId == curve.Id)
        {
            //Curve already selected
            return;
        }

        //Reset any calibrator & curve control issue
        analyticalRun.AddDomainEvent(new UpdateAnalyticalRunQualityIssue(analyticalRun, AnalyticalRunIssue.NoIssue, AnalyticalRunIssue.NoIssue, null, null));

        // Manually selected curve — mark source and unlock calculations
        var oldState = analyticalRun.CurveState;
        analyticalRun.CurveSource = CurveSource.ManuallySelected;
        analyticalRun.CurveState = CurveState.Valid;

        //Save changes
        await context.SaveChangesAsync(cancellationToken);

        //Update calculation curve
        analyticalRun.CalculationCurve = curve;

        analyticalRun.AddDomainEvent(new AnalyticalRunCalculationCurveUpdated(analyticalRun.Id));

        await context.SaveChangesAsync(cancellationToken);
    }
}
