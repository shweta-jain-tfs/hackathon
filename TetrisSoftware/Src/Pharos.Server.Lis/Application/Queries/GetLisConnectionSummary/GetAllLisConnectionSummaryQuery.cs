using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Lis;

namespace Pharos.Server.Lis.Application.Queries.GetLisConnectionSummary;

internal record GetAllLisConnectionSummaryQuery() : IRequest<List<LisConnectionSummaryDto>>;

internal sealed class GetAllLisConnectionSummaryQueryHandler(
    IOptionsSnapshot<LisSettings> lisSettings, ISender sender)
    : IRequestHandler<GetAllLisConnectionSummaryQuery, List<LisConnectionSummaryDto>>
{
    public async Task<List<LisConnectionSummaryDto>> Handle(GetAllLisConnectionSummaryQuery request, CancellationToken cancellationToken)
    {
        var lisStatisticsDtos = new List<LisConnectionSummaryDto>();
        var lisChannelId = 0;
        foreach (var lisChannel in lisSettings.Value.LisChannels)
        {
            if (lisChannel.IsEnabled)
            {
                var lisStatisticsDto = await sender.Send(new GetLisConnectionSummaryQuery(lisChannelId), cancellationToken);
                lisStatisticsDtos.Add(lisStatisticsDto);
            }
            lisChannelId++;
        }
        return lisStatisticsDtos;
    }
}
