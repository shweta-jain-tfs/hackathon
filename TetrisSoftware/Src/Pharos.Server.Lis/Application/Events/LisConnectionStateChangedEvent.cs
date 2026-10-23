using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Application.Queries;

namespace Pharos.Server.Lis.Application.Events;

internal record LisConnectionStateChangedEvent : INotification;

internal sealed class LisConnectionStateEventHandler(ILisConnectionHub hub, ISender sender) : INotificationHandler<LisConnectionStateChangedEvent>
{
    public async Task Handle(LisConnectionStateChangedEvent notification, CancellationToken cancellationToken)
    {
        await RaiseTaskCardForLisConnectionFaiure(cancellationToken);
        await hub.LisStatusChanged();
    }

    private async Task RaiseTaskCardForLisConnectionFaiure(CancellationToken cancellationToken)
    {
        var lisConnections = await sender.Send(new GetLisQuery(), cancellationToken);
        var hasDisconnectedLis = lisConnections.Any(x => x.State != LisConnectionState.Open);

        if (hasDisconnectedLis)
        {
            await sender.Send(new TaskCardCommand(TaskCardType.ConnectionFailure, TaskCardState.Error), cancellationToken);
        }
        else
        {
            await sender.Send(new RemoveTaskCardCommand(TaskCardType.ConnectionFailure), cancellationToken);
        }
    }
}