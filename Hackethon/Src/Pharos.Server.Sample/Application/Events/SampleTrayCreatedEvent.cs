using Pharos.Contracts.Notifications;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Application.Events;

internal class SampleTrayCreatedEvent(SampleTray newEntity) : EntityCreatedEvent<SampleTray>(newEntity);

internal sealed class SampleTrayCreatedEventHandler(ISampleTrayHub hub) : INotificationHandler<SampleTrayCreatedEvent>
{
    public Task Handle(SampleTrayCreatedEvent notification, CancellationToken cancellationToken)
    {
        return hub.Created(notification.NewEntity.Id);
    }
}