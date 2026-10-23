using MediatR;
using Pharos.Server.Application.Exceptions;
using Pharos.Server.Application.Security;
using Pharos.Server.Result.Application.Events;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Commands;

public record AcceptCurveFromFlaggedCurveControlCommand(int AnalyticalRunId) : IRequest;

internal sealed class AcceptCurveFromFlaggedCurveControlCommandHandler(
    IResultDbContext dbContext,
    IMediator mediator,
    IAuthorizationService authorizationService)
    : IRequestHandler<AcceptCurveFromFlaggedCurveControlCommand>
{
    public async Task Handle(AcceptCurveFromFlaggedCurveControlCommand request, CancellationToken cancellationToken)
    {
        var analyticalRun = await dbContext.AnalyticalRuns
            .Where(x => x.Id == request.AnalyticalRunId)
            .SingleOrDefaultAsync(cancellationToken);

        if (analyticalRun == null)
        {
            throw new KeyNotFoundException($"AnalyticalRun with Id {request.AnalyticalRunId} not found.");
        }

        if (analyticalRun.CurveId == null)
        {
            throw new InvalidOperationException($"AnalyticalRun with Id {request.AnalyticalRunId} has no CalculationCurve to approve.");
        }

        var curve = await dbContext.Curves
            .Where(c => c.Id == analyticalRun.CurveId)
            .SingleOrDefaultAsync(cancellationToken);

        var canAcceptFlaggedCurve = curve is { Judgment: CurveJudgment.Flagged };
        var canAcceptFlaggedCurveControl = analyticalRun.CurveControlTotalJudgment == CurveControlJudgmentResult.Flagged
            && curve is { Judgment: not CurveJudgment.NotOK };

        // Accept is allowed either for a flagged curve or for a run blocked by a flagged CC result.
        if (!canAcceptFlaggedCurve && !canAcceptFlaggedCurveControl)
        {
            throw new NotAllowedException("This curve cannot be accepted.");
        }

        if (analyticalRun.CurveState == CurveState.Frozen && !authorizationService.RndMode)
        {
            throw new NotAllowedException("This action is only allowed in R&D mode because the curve is already frozen.");
        }

        analyticalRun.CurveState = CurveState.Valid;

        analyticalRun.AddDomainEvent(new UpdateAnalyticalRunQualityIssue(analyticalRun, AnalyticalRunIssue.NoIssue, AnalyticalRunIssue.NoIssue, null, null));

        await dbContext.SaveChangesAsync(cancellationToken);
        await mediator.Publish(new AnalyticalRunCalculationCurveUpdated(analyticalRun.Id), cancellationToken);
    }
}
