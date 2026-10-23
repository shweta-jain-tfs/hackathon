using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetMethodCalibrationCurves;

internal class GetMethodCalibrationCurvesQueryHandler(IParametersDbContext dbContext) : IRequestHandler<GetMethodCalibrationCurvesQuery, List<MethodCalibrationCurveDto>>
{
    public async Task<List<MethodCalibrationCurveDto>> Handle(GetMethodCalibrationCurvesQuery request, CancellationToken cancellationToken)
    {
        var methodCalibrationCurves = await dbContext.MethodCalibrationCurves
            .Include(x => x.Method)
            .ThenInclude(x => x.AnalyticalRun)
            .Select(x => new MethodCalibrationCurveDto
            {
                CurveId = x.Id,
                MethodId = x.MethodId,
                MethodTestId = x.MethodTestId,
                MaxNumberOfDays = x.UserMaxNumberOfDays ?? x.DefaultMaxNumberOfDays, // Use UserMaxNumberOfDays if available, otherwise DefaultMaxNumberOfDays
                MethodName = x.Method.MethodName,
                AllowMonthlyCalibration = x.Method.AnalyticalRun.AllowMonthlyCalibration
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        
        return methodCalibrationCurves;
    }
}