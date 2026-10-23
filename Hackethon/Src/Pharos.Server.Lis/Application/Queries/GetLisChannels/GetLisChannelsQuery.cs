using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Lis;

namespace Pharos.Server.Lis.Application.Queries.GetLisChannels;

internal record GetLisChannelsQuery : IRequest<IList<LisChannelDto>>;

internal sealed class GetLisChannelsQueryHandler(
    IOptionsSnapshot<LisSettings> lisSettings)
    : IRequestHandler<GetLisChannelsQuery, IList<LisChannelDto>>
{
    public Task<IList<LisChannelDto>> Handle(GetLisChannelsQuery request, CancellationToken cancellationToken)
    {
        var channels = lisSettings.Value.LisChannels
            .Select((channel, index) => new LisChannelDto
            {
                Id = index,
                Name = channel.Name
            })
            .ToList();

        return Task.FromResult<IList<LisChannelDto>>(channels);
    }
}
