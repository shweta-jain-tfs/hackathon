using MediatR;
using Pharos.Server.Result.Application.Interfaces;

namespace Pharos.Server.Result.Application.Queries.GetCalibrationCurve;

internal record GetCurrentCalibrationCurvesQuery(string InstrumentNumber, string MethodName) : IRequest<List<(int CurveId, string ConjugateLotNumber)>>;

internal sealed class GetCurrentCalibrationCurvesQueryHandler(
    IResultDbContext context) : IRequestHandler<GetCurrentCalibrationCurvesQuery, List<(int, string)>>
{
    public async Task<List<(int, string)>> Handle(GetCurrentCalibrationCurvesQuery request, CancellationToken cancellationToken)
    {
        var list = await context.CurrentCurves
            .Where(x => x.Curve.InstrumentNumber == request.InstrumentNumber && x.MethodName == request.MethodName && x.ConjugateLotNumber != null)
            .Select(x => new
            {
                x.CurveId,
                x.ConjugateLotNumber
            })
            .ToListAsync(cancellationToken);

        return list.Select(x => (x.CurveId, x.ConjugateLotNumber!)).ToList();
    }
}