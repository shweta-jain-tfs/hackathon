using MediatR;
using Pharos.Server.Application.Security;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetCalibrationCurve;

internal record GetAvailableCalibrationCurvesQuery(int AnalyticalRunId) : IRequest<List<AvailableCurveDto>>;

internal class GetAvailableCalibrationCurvesQueryHandler(IResultDbContext context, IAuthorizationService authorizationService) : IRequestHandler<GetAvailableCalibrationCurvesQuery, List<AvailableCurveDto>>
{
    public async Task<List<AvailableCurveDto>> Handle(GetAvailableCalibrationCurvesQuery request, CancellationToken cancellationToken)
    {
        //Get analytical run
        var analyticalRun = await context.AnalyticalRuns
            .Where(x => x.Id == request.AnalyticalRunId)
            .SingleOrDefaultAsync(cancellationToken);

        if (analyticalRun == null)
        {
            throw new NotFoundException($"Analytical run with id {request.AnalyticalRunId} not found");
        }

        var conjugateLot = analyticalRun.ConjugateLotNumber ?? "";
        var serialNumber = analyticalRun.InstrumentNumber;
        var methodName = analyticalRun.MethodName;
        var calculationCurveId = analyticalRun.CurveId;
        var analyticalStart = DateOnly.FromDateTime(analyticalRun.StartDateTime.DateTime);

        var curvesQuery = context.Curves
            .Where(x => x.MethodName == methodName
                        && x.ConjugateLotNumber == conjugateLot
                        && x.InstrumentNumber == serialNumber
                        && x.Id != calculationCurveId);

        if (authorizationService.RndMode)
        {
            curvesQuery = curvesQuery.Where(x => x.Judgment == CurveJudgment.OK || x.Judgment == CurveJudgment.Flagged);
        }
        else
        {
            curvesQuery = curvesQuery.Where(x => x.Judgment == CurveJudgment.OK
                                                 && (x.CurveControlDisabledFlag == false || x.CurveControlDisabledFlag == null)
                                                 && x.FirstDayToUse <= analyticalStart
                                                 && x.LastDayToUse >= analyticalStart);
        }

        var curves = await curvesQuery
            .Select(c => new AvailableCurveDto
            {
                Id = c.Id,
                MeasurementDateTime = c.MeasurementDateTime,
                Judgment = c.Judgment,
                ConjugateLotNr = c.ConjugateLotNumber!
            })
            .ToListAsync(cancellationToken);

        return curves;
    }
}
