using Pharos.Server.Reflex.Domain;
using Pharos.Server.Reflex.Infrastructure.Hubs;

namespace Pharos.Server.Reflex.Application.Events;

internal class ReflexDefinitionCreatedEvent(ReflexDefinition newEntity) : EntityCreatedEvent<ReflexDefinition>(newEntity);

internal class ReflexDefinitionUpdatedEvent(ReflexDefinition updatedEntity) : EntityUpdatedEvent<ReflexDefinition>(updatedEntity);

internal class ReflexDefinitionDeletedEvent(ReflexDefinition deletedEntity) : EntityDeletedEvent<ReflexDefinition>(deletedEntity);

internal class ReflexDefinitionEventHandler(ReflexHub hub) : INotificationHandler<ReflexDefinitionCreatedEvent>, INotificationHandler<ReflexDefinitionUpdatedEvent>, INotificationHandler<ReflexDefinitionDeletedEvent>
{
    public async Task Handle(ReflexDefinitionCreatedEvent notification, CancellationToken cancellationToken)
    {
        await hub.Created(notification.NewEntity.Id);
    }

    public async Task Handle(ReflexDefinitionUpdatedEvent notification, CancellationToken cancellationToken)
    {
        await hub.Updated(notification.UpdatedEntity.Id);
    }

    public async Task Handle(ReflexDefinitionDeletedEvent notification, CancellationToken cancellationToken)
    {
        await hub.Deleted(notification.DeletedEntity.Id);
    }
}