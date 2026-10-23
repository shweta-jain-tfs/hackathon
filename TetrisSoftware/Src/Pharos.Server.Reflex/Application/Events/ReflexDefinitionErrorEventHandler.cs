using Pharos.Server.Application.SystemErrors;

namespace Pharos.Server.Reflex.Application.Events;

internal class ReflexDefinitionErrorEventHandler(ISender sender) :
    INotificationHandler<ReflexDefinitionCreatedEvent>,
    INotificationHandler<ReflexDefinitionDeletedEvent>
{
    public Task Handle(ReflexDefinitionCreatedEvent notification, CancellationToken cancellationToken)
    {
        return sender.Send(
            ReflexPharosErrors.ReflexDefinitionCreated(notification.NewEntity.Name),
            cancellationToken);
    }

    public Task Handle(ReflexDefinitionDeletedEvent notification, CancellationToken cancellationToken)
    {
        return sender.Send(
            ReflexPharosErrors.ReflexDefinitionDeleted(notification.DeletedEntity.Name),
            cancellationToken);
    }
}
