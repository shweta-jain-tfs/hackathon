using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Result;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Application.Interfaces;

namespace Pharos.Server.Result.Application.Queries.GetCalibrationCurve;

internal class GetCalibrationValidDaysQueryHandler(IResultDbContext dbContext, IMediator mediator, IDateTime dateTime) : IRequestHandler<GetCalibrationValidDaysQuery, List<CalibrationValidDaysDto>>
{
    public async Task<List<CalibrationValidDaysDto>> Handle(GetCalibrationValidDaysQuery request, CancellationToken cancellationToken)
    {
        // Get method curves and create lookup.
        var methodCurves = await mediator.Send(new GetMethodCalibrationCurvesQuery(), cancellationToken);
        var methodCurvesDict = methodCurves.ToDictionary(mc => mc.MethodName, mc => mc);
        var validMethodNames = methodCurvesDict.Keys.ToList();

        // Get current curves data for the specified instrument number.
        var currentCurvesData = await dbContext.CurrentCurves
            .Join(
                dbContext.Curves,
                currentCurve => currentCurve.CurveId,
                curve => curve.Id,
                (currentCurve, curve) => new
                {
                    currentCurve.MethodName,
                    currentCurve.CurveId,
                    curve.InstrumentNumber,
                    curve.MeasurementDateTime,
                    curve.LastDayToUse,
                    curve.ConjugateLotNumber,
                    curve.ImmunoCapLotNumber,
                    curve.Id
                })
            .Where(joined =>
                joined.InstrumentNumber == request.InstrumentNumber &&
                validMethodNames.Contains(joined.MethodName))
            .ToListAsync(cancellationToken);

        
        var utcNow = dateTime.UtcNow;

        var result = new List<CalibrationValidDaysDto>();

        foreach (var curveData in currentCurvesData)
        {
            var methodCurve = methodCurvesDict[curveData.MethodName];

            // Pretend that we don´t have any curves if we don´t allow monthly calibration for the method.
            if (!methodCurve.AllowMonthlyCalibration)
            {
                continue;
            }

            // Calculate the valid days for each current curve
            var daysDiff = curveData.LastDayToUse.DayNumber - DateOnly.FromDateTime(utcNow.Date).DayNumber;
            var validForDays = Math.Max(daysDiff, 0);
            
            result.Add(new CalibrationValidDaysDto
            {
                CalibrationValidForDays = validForDays,
                ConjugateLotNumber = curveData.ConjugateLotNumber,
                CurveId = curveData.Id,
                ImmunoCapLotNumber = curveData.ImmunoCapLotNumber,
                MaxNumberOfDays = methodCurve.MaxNumberOfDays,
                MethodId = methodCurve.MethodId,
                MethodName = methodCurve.MethodName,
                CurveCreated = curveData.MeasurementDateTime
            });

        }
        
        return result;
    }
}
