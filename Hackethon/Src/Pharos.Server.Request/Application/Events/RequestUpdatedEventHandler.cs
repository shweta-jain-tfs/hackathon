using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Request;

namespace Pharos.Server.Request.Application.Events;

internal sealed class RequestUpdatedEventHandler(IRequestHub hub) : INotificationHandler<RequestUpdatedEvent>
{
    public async Task Handle(RequestUpdatedEvent notification, CancellationToken cancellationToken)
    {
        //Send notification to clients
        await hub.Updated(notification.RequestId);
    }
}