using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Result;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Commands;

internal class CleanupCurrentCurvesCommandHandler(IResultDbContext dbContext, IPublisher publisher, IDateTime dateTime) : IRequestHandler<CleanupCurrentCurvesCommand>
{
    public async Task Handle(CleanupCurrentCurvesCommand request, CancellationToken cancellationToken)
    {
        var noLongerCurrentCurves = new List<CurrentCurve>();

        var currentCurves = await dbContext.CurrentCurves.Include(x => x.Curve).ToListAsync(cancellationToken);

        foreach (var curve in currentCurves)
        {
            if (curve.Curve.LastDayToUse < DateOnly.FromDateTime(dateTime.Now.Date))
            {
                noLongerCurrentCurves.Add(curve);
            }
        }

        dbContext.CurrentCurves.RemoveRange(noLongerCurrentCurves);

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var instrumentGroup in noLongerCurrentCurves.GroupBy(x => x.Curve.InstrumentNumber))
        {
            await publisher.Publish(new ValidCurvesUpdatedEvent(instrumentGroup.Key), cancellationToken);
        }
    }
}