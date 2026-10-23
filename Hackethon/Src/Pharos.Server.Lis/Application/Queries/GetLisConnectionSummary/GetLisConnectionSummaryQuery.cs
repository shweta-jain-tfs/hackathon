using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Application.Queries.GetLisConnectionSummary;

internal record GetLisConnectionSummaryQuery(int ChannelId) : IRequest<LisConnectionSummaryDto>;

internal sealed class GetLisConnectionSummaryQueryHandler(
    ILisConnectionPool connectionPool,
    IOptionsSnapshot<LisSettings> lisSettings,
    ILisDbContext lisDbContext)
    : IRequestHandler<GetLisConnectionSummaryQuery, LisConnectionSummaryDto>
{
    public async Task<LisConnectionSummaryDto> Handle(GetLisConnectionSummaryQuery request, CancellationToken cancellationToken)
    {
        var lisConnectionSummaryDto = new LisConnectionSummaryDto();

        var channel = lisSettings.Value.LisChannels.ElementAtOrDefault(request.ChannelId);
        if (channel is null)
        {
            return lisConnectionSummaryDto;
        }

        lisConnectionSummaryDto.Id = request.ChannelId;
        lisConnectionSummaryDto.Name = channel.Name;

        MapConnectionStatesToDto(lisConnectionSummaryDto, request.ChannelId);

        await MapChannelStatisticsToDto(lisConnectionSummaryDto, request.ChannelId, cancellationToken);

        return lisConnectionSummaryDto;
    }

    private void MapConnectionStatesToDto(LisConnectionSummaryDto lisConnectionSummaryDto, int channelId)
    {
        var statuses = connectionPool.GetConnectionStates(channelId);

        lisConnectionSummaryDto.PrimaryConnectionState = statuses.FirstOrDefault(s => s.Role.HasFlag(LisConnectionRole.Import))?
            .State ?? LisConnectionState.Closed;

        lisConnectionSummaryDto.ExportConnectionState = statuses.FirstOrDefault(s => s.Role == LisConnectionRole.Export)?.State;
    }

    private async Task MapChannelStatisticsToDto(LisConnectionSummaryDto lisConnectionSummaryDto, int channelId, CancellationToken cancellationToken)
    {
        var statistics = await lisDbContext.LisStatistics
            .AsNoTracking()
            .Where(x => x.ChannelId == channelId)
            .ToListAsync(cancellationToken);

        foreach (var record in statistics)
        {
            switch (record.LisStatisticsCategory)
            {
                case LisStatisticsCategory.Queries:
                    lisConnectionSummaryDto.Queries = ToChannelStatisticsDto(record);
                    break;

                case LisStatisticsCategory.Exported:
                    lisConnectionSummaryDto.Exported = ToChannelStatisticsDto(record, includeTests: true);
                    break;

                case LisStatisticsCategory.Imported:
                    lisConnectionSummaryDto.Imported = ToChannelStatisticsDto(record, includeTests: true);
                    break;
            }
        }
    }

    private static LisConnectionStatisticsDto ToChannelStatisticsDto(LisStatistics record, bool includeTests = false)
    {
        return new()
        {
            LatestSample = record.LatestSampleId,
            LatestTime = record.LatestTime,
            Samples = record.SamplesCount,
            Tests = includeTests ? record.TestsCount : default
        };
    }
}
