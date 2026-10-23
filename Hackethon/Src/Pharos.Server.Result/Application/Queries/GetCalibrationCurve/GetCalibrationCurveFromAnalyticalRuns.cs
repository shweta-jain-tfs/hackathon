using MediatR;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Queries.GetAnalyticalRuns;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetCalibrationCurve;

internal record GetCalibrationCurveFromAnalyticalRuns(int AnalyticalRunId) : IRequest<CurvePresentationDto?>;

internal sealed class GetCalibrationCurveFromAnalyticalRunsHandler(ISender sender, IResultDbContext context) : IRequestHandler<GetCalibrationCurveFromAnalyticalRuns, CurvePresentationDto?>
{
    public async Task<CurvePresentationDto?> Handle(GetCalibrationCurveFromAnalyticalRuns request, CancellationToken cancellationToken)
    {
        var analyticalRun = await context.AnalyticalRuns
            .Include(x => x.TestResults)
            .Where(x => x.Id == request.AnalyticalRunId)
            .SingleOrDefaultAsync(cancellationToken);

        if (analyticalRun == null)
        {
            return null;
        }

        if (!analyticalRun.CurveId.HasValue)
        {
            return new CurvePresentationDto
            {
                Id = analyticalRun.CurveId ?? 0,
                CurveState = (CalculationStateStatus)analyticalRun.CurveState,
                CurveControlDisabledFlag = true
            };
        }

        var curve = await sender.Send(new GetCalibrationCurvePointsQuery(analyticalRun.CurveId.Value), cancellationToken);
        curve.CurveState = (CalculationStateStatus)analyticalRun.CurveState;
        curve.IsManuallySelected = analyticalRun.CurveSource == CurveSource.ManuallySelected;
        curve.IsAccepted = curve.Judgment == CurveJudgment.Flagged
                           && analyticalRun.CurveState == CurveState.Frozen;

        curve.CurveControlDisabledFlag = analyticalRun.CurveSource == CurveSource.Missing && analyticalRun.CurveState == CurveState.Invalid;
        return curve;
    }
}