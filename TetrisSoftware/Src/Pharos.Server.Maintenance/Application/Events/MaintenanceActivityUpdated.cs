using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Domain.Common;
using Pharos.Server.Maintenance.Domain;

namespace Pharos.Server.Maintenance.Application.Events;

internal class MaintenanceActivityUpdated(MaintenanceActivity entity) : EntityUpdatedEvent<MaintenanceActivity>(entity);

internal sealed class MaintenanceActivityUpdatedEventHandler(IMaintenanceActivityHub hub) : INotificationHandler<MaintenanceActivityUpdated>
{
    public async Task Handle(MaintenanceActivityUpdated notification, CancellationToken cancellationToken)
    {
        await hub.MaintenanceActivitiesUpdated();
    }
}