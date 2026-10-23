using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Lis.Application.Events;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Application.Commands.LisStatisticsCommands;

internal sealed record ResetLisStatisticsCommand(int ChannelId) : IRequest;

internal sealed class ResetLisStatisticsCommandHandler(
    ILisDbContext lisDbContext,
    IPublisher publisher) : IRequestHandler<ResetLisStatisticsCommand>
{
    public async Task Handle(ResetLisStatisticsCommand request, CancellationToken cancellationToken)
    {
        var lisStatisticsQuery = lisDbContext.LisStatistics.Where(r => r.ChannelId == request.ChannelId);

        await lisStatisticsQuery
            .Where(r => r.LisStatisticsCategory != LisStatisticsCategory.Queries)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.LatestSampleId, (string?)null)
                .SetProperty(r => r.SamplesCount, 0)
                .SetProperty(r => r.TestsCount, 0)
                .SetProperty(r => r.LatestTime, (DateTimeOffset?)null),
                cancellationToken);

        await lisStatisticsQuery
            .Where(r => r.LisStatisticsCategory == LisStatisticsCategory.Queries)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.LatestSampleId, (string?)null)
                .SetProperty(r => r.SamplesCount, 0)
                .SetProperty(r => r.TestsCount, (int?)null)
                .SetProperty(r => r.LatestTime, (DateTimeOffset?)null),
                cancellationToken);

        await publisher.Publish(new LisStatisticsChangedEvent(request.ChannelId), cancellationToken);
    }
}
