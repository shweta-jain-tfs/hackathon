using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Events;

internal class CleanupCurrentCurvesWhenMethodsUpdatedEventHandler(IResultDbContext dbContext, IMethodsService methodsService, IPublisher publisher) : INotificationHandler<MethodsUpdatedEvent>
{
    public async Task Handle(MethodsUpdatedEvent notification, CancellationToken cancellationToken)
    {
        //get all methods that now have RunCalibrationOnCarrierLotChange set to false
        var methodsOfInterest = await methodsService.GetMethods(x => x.AnalyticalRun.RunCalibrationOnCarrierLotChange == false, x => x.MethodName);

        //get the current curves for those methods, 
        var currentCurves = await dbContext.CurrentCurves
            .Include(x => x.Curve)
            .Where(x => methodsOfInterest.Contains(x.MethodName)).ToListAsync(cancellationToken);

        //group them by method, conjugate lot number, and instrument number
        var groupsOfInterest = currentCurves.GroupBy(x => (x.MethodName, x.ConjugateLotNumber, x.Curve.InstrumentNumber));

        var curvesToDelete = new List<CurrentCurve>();
        var instrumentsAffected = new HashSet<string>();

        // where there are more than 1 curve in the group (indicating that there are multiple curves for the same method and conjugate lot number)
        foreach (var group in groupsOfInterest.Where(x => x.Count() > 1))
        {
            //keep the curve with the highest LastDayToUse
            var curveToKeep = group.OrderByDescending(x => x.Curve.LastDayToUse).First();

            //add the others to the list of curves to delete
            curvesToDelete.AddRange(group.Except(new[] { curveToKeep }));

            //add instrument to be notified
            instrumentsAffected.Add(group.Key.InstrumentNumber);
        }

        //delete the curves that we have identified for deletion
        if (curvesToDelete.Count > 0)
        {
            dbContext.CurrentCurves.RemoveRange(curvesToDelete);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        //notify the instruments that their valid curves have been updated
        foreach (var instrumentNumber in instrumentsAffected)
        {
            await publisher.Publish(new ValidCurvesUpdatedEvent(instrumentNumber), cancellationToken);
        }
    }
}