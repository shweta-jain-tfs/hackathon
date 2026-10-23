using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Entities;

namespace Pharos.Server.Request.Application.Events;

internal class RequestCreatedEvent(Domain.Request newEntity) : EntityCreatedEvent<Domain.Request>(newEntity);

internal sealed class RequestCreatedEventHandler(IMediator mediator, IRequestHub hub) : INotificationHandler<RequestCreatedEvent>
{
    public async Task Handle(RequestCreatedEvent notification, CancellationToken cancellationToken)
    {
        var newRequest = notification.NewEntity;

        var requestUid = new RequestUid
        {
            SampleId = newRequest.SampleId,
            RequestCreatedDate = newRequest.Created
        };

        await mediator.Publish(
            new Server.Application.Request.RequestCreatedEvent(
                newRequest.Id,
                requestUid,
                newRequest.TestOrders.ToList()),
            cancellationToken);
        //If the request is generated from request generator UI notification is ignored.
        if (newRequest.Source != Domain.RequestSource.Generator)
        {
            await hub.Created(newRequest.Id);
        }
    }
}