using MediatR;
using Pharos.Server.Application.Request;
using Pharos.Server.Lis.Application.Commands.LisStatisticsCommands;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Application.Events;

internal sealed class RequestUpdatedLisStatisticsEventHandler(ISender sender)
    : INotificationHandler<RequestUpdatedEvent>
{
    public async Task Handle(RequestUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var channelStats = notification.TestOrders
            .Where(x => x.LisChannelId.HasValue && x.EntityState == EntityState.Added)
            .GroupBy(x => x.LisChannelId!.Value)
            .Select(x => new { ChannelId = x.Key, TestsCount = x.Count() })
            .ToList();

        foreach (var channelStat in channelStats)
        {
            await sender.Send(new SaveLisStatisticsCommand(
                channelStat.ChannelId,
                LisStatisticsCategory.Imported,
                1,
                channelStat.TestsCount,
                notification.RequestUid.SampleId), cancellationToken);
        }
    }
}
