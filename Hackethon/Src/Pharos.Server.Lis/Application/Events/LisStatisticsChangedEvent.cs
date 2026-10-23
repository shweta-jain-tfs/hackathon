using MediatR;
using Pharos.Contracts.Notifications;

namespace Pharos.Server.Lis.Application.Events;

internal sealed record LisStatisticsChangedEvent(int ChannelId) : INotification;

internal sealed class LisStatisticsChangedEventHandler(ILisConnectionHub hub) : INotificationHandler<LisStatisticsChangedEvent>
{
    public async Task Handle(LisStatisticsChangedEvent notification, CancellationToken cancellationToken)
    {
        await hub.LisStatisticsChanged(notification.ChannelId);
    }
}
