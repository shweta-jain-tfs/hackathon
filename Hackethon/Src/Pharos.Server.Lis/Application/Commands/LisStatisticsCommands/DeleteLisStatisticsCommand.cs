using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Lis.Application.Events;
using Pharos.Server.Lis.Application.Interfaces;

namespace Pharos.Server.Lis.Application.Commands.LisStatisticsCommands;

internal sealed record DeleteLisStatisticsCommand(int ChannelId) : IRequest;

internal sealed class DeleteLisStatisticsCommandHandler(
    ILisDbContext lisDbContext,
    IPublisher publisher) : IRequestHandler<DeleteLisStatisticsCommand>
{
    public async Task Handle(DeleteLisStatisticsCommand request, CancellationToken cancellationToken)
    {
        var lisStatistics = await lisDbContext.LisStatistics
            .Where(x => x.ChannelId == request.ChannelId)
            .ToListAsync(cancellationToken);

        if (lisStatistics.Count == 0)
        {
            return;
        }

        lisDbContext.LisStatistics.RemoveRange(lisStatistics);
        await lisDbContext.SaveChangesAsync(cancellationToken);
        await publisher.Publish(new LisStatisticsChangedEvent(request.ChannelId), cancellationToken);
    }
}
