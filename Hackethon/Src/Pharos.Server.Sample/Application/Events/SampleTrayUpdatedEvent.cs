using Pharos.Contracts.Notifications;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Application.Events;

/// <summary>
/// This event will be triggered only when tray entity is updated (currently only instrument, drawer are part of the entity)
/// </summary>
internal class SampleTrayUpdatedEvent(SampleTray entity) : EntityUpdatedEvent<SampleTray>(entity);

internal sealed class SampleTrayUpdatedEventHandler(ISampleTrayHub hub) : INotificationHandler<SampleTrayUpdatedEvent>
{
    public Task Handle(SampleTrayUpdatedEvent notification, CancellationToken cancellationToken)
    {
        return hub.Updated(notification.UpdatedEntity.Id);
    }
}