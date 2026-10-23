using Pharos.Contracts.Notifications;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Application.Events;

internal class SampleTrayDeletedEvent(SampleTray entity) : EntityDeletedEvent<SampleTray>(entity);

internal sealed class SampleTrayDeletedEventHandler(ISampleTrayHub hub) : INotificationHandler<SampleTrayDeletedEvent>
{
    public Task Handle(SampleTrayDeletedEvent notification, CancellationToken cancellationToken)
    {
        return hub.Deleted(notification.DeletedEntity.Id);
    }
}