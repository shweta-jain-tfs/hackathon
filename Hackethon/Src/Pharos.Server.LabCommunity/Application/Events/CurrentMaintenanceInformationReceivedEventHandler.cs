using LabCommunity.API.Client;
using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.LabCommunity.Application.Interfaces.InstrumentStatus;

namespace Pharos.Server.LabCommunity.Application.Events;

internal sealed class CurrentMaintenanceInformationReceivedEventHandler(
    IInstrumentMaintenanceActivityCache cache) : INotificationHandler<CurrentMaintenanceInformationReceivedEvent>
{
    public Task Handle(CurrentMaintenanceInformationReceivedEvent notification, CancellationToken cancellationToken)
    {
        var message = notification.Message;
        cache.Set(
            notification.InstrumentNumber,
            new MaintenanceActivity
            {
                MaintenanceActivities = message.MaintenanceInformation
                    .Select(activity => new MaintenanceActivityInformation
                    {
                        Type = activity.Type.ToString(),
                        Status = activity.Status.ToString(),
                        LastStarted = activity.LastStarted?.ToDateTimeOffset(),
                        LastEnded = activity.LastEnded?.ToDateTimeOffset()
                    })
                    .ToList(),
                Rinsed = message.Rinsed,
                MaintenanceNeeded = message.MaintenanceNeeded
            });

        return Task.CompletedTask;
    }
}
