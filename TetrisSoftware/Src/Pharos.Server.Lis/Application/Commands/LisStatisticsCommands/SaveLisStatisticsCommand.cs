using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Infrastructure;
using Pharos.Server.Lis.Application.Events;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Application.Commands.LisStatisticsCommands;

public sealed record SaveLisStatisticsCommand(
    int ChannelId,
    LisStatisticsCategory LisStatisticsCategory,
    int SamplesCount,
    int? TestsCount,
    string? LatestSampleId) : IRequest;


internal sealed class SaveLisStatisticsCommandHandler(
    ILisDbContext lisDbContext,
    IDateTime dateTime,
    IPublisher publisher) : IRequestHandler<SaveLisStatisticsCommand>
{
    public async Task Handle(SaveLisStatisticsCommand request, CancellationToken cancellationToken)
    {
        var statistic = await lisDbContext.LisStatistics.FirstOrDefaultAsync(s => s.ChannelId == request.ChannelId && s.LisStatisticsCategory == request.LisStatisticsCategory, cancellationToken);

        if (statistic is null)
        {
            statistic = new LisStatistics
            {
                ChannelId = request.ChannelId,
                LisStatisticsCategory = request.LisStatisticsCategory,
                CreatedDate = dateTime.UtcNow.DateTime,
            };
            await lisDbContext.LisStatistics.AddAsync(statistic, cancellationToken);
        }

        statistic.SamplesCount += request.SamplesCount;
        if (request.TestsCount.HasValue)
        {
            statistic.TestsCount = (statistic.TestsCount ?? 0) + request.TestsCount.Value;
        }
        statistic.LatestTime = dateTime.UtcNow;

        if (request.LatestSampleId is not null)
        {
            statistic.LatestSampleId = request.LatestSampleId;
        }

        await lisDbContext.SaveChangesAsync(cancellationToken);
        await publisher.Publish(new LisStatisticsChangedEvent(request.ChannelId), cancellationToken);
    }
}


