using MediatR;
using Pharos.Server.Result.Domain;
using Pharos.Server.Result.Infrastructure.Hubs;

namespace Pharos.Server.Result.Application.Events;

internal class AnalyticalRunUpdatedEvent(AnalyticalRun entity) : EntityUpdatedEvent<AnalyticalRun>(entity);

internal sealed class AnalyticalRunUpdatedEventHandler(AnalyticalRunHub hub) : INotificationHandler<AnalyticalRunUpdatedEvent>
{
    public async Task Handle(AnalyticalRunUpdatedEvent notification, CancellationToken cancellationToken)
    {
        await hub.Updated(notification.UpdatedEntity.Id);
    }
}