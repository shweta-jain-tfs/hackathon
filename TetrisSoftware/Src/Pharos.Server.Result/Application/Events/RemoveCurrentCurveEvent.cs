using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Result.Application.Interfaces;

namespace Pharos.Server.Result.Application.Events;

internal record RemoveCurrentCurveEvent(int CurveId) : INotification;

internal sealed class RemoveCurrentCurveEventHandler(IResultDbContext dbContext) : INotificationHandler<RemoveCurrentCurveEvent>
{
    public async Task Handle(RemoveCurrentCurveEvent notification, CancellationToken cancellationToken)
    {
        var curveId = notification.CurveId;

        var curve = await dbContext.Curves.Where(c => c.Id == curveId).SingleOrDefaultAsync(cancellationToken);

        var currentCurve = await dbContext.CurrentCurves.Where(cc => curve != null
                                                                      && cc.MethodName == curve.MethodName
                                                                      && cc.ConjugateLotNumber == curve.ConjugateLotNumber
                                                                      && cc.Curve.InstrumentNumber == curve.InstrumentNumber).SingleOrDefaultAsync(cancellationToken);

        if (currentCurve != null)
        {
            dbContext.CurrentCurves.Remove(currentCurve);

            currentCurve.AddDomainEvent(new ValidCurvesUpdatedEvent(curve!.InstrumentNumber));
            curve.AddDomainEvent(new CalibrationCurveChartUpdatedEvent(curve.InstrumentNumber, curve.MethodName));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}