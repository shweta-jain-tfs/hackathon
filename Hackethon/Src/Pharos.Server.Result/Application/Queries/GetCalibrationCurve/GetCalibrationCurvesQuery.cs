using MediatR;
using Pharos.Server.Result.Application.Interfaces;

namespace Pharos.Server.Result.Application.Queries.GetCalibrationCurve;

internal record GetCalibrationCurvesQuery(string InstrumentNumber, string MethodName) : IRequest<List<int>>;

internal sealed class GetCalibrationCurvesQueryHandler(
    IResultDbContext context) : IRequestHandler<GetCalibrationCurvesQuery, List<int>>
{
    public async Task<List<int>> Handle(GetCalibrationCurvesQuery request, CancellationToken cancellationToken)
    {
        var list = await context.Curves
            .Where(x => x.InstrumentNumber == request.InstrumentNumber && x.MethodName == request.MethodName)
            .OrderByDescending(x => x.Id)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        return list;
    }
}