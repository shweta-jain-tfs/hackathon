using MediatR;
using Pharos.Server.Result.Domain;
using Pharos.Server.Result.Infrastructure.Hubs;

namespace Pharos.Server.Result.Application.Events;

internal class AnalyticalRunCreatedEvent(AnalyticalRun entity) : EntityCreatedEvent<AnalyticalRun>(entity);

internal sealed class AnalyticalRunCreatedEventHandler(AnalyticalRunHub hub) : INotificationHandler<AnalyticalRunCreatedEvent>
{
    public async Task Handle(AnalyticalRunCreatedEvent notification, CancellationToken cancellationToken)
    {
        await hub.Created(notification.NewEntity.Id);
    }
}