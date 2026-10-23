using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Interfaces;

namespace Pharos.Server.Lis.Application.Queries;

internal record GetLisQuery : IRequest<LisConnectionDto[]>;

internal class GetLisQueryHandler(ILisConnectionPool connectionPool, IOptionsSnapshot<LisSettings> lisSettings) : IRequestHandler<GetLisQuery, LisConnectionDto[]>
{
    public async Task<LisConnectionDto[]> Handle(GetLisQuery request, CancellationToken cancellationToken)
    {
        var lisChannelId = 0;
        var lisConnections = new List<LisConnectionDto>();
        foreach (var lisChannel in lisSettings.Value.LisChannels)
        {
            if (lisChannel.IsEnabled)
            {
                var lisConnection = new LisConnectionDto
                {
                    State = GetConnectionState(lisChannelId),
                    Id = lisChannelId,
                };

                lisConnections.Add(lisConnection);
            }

            lisChannelId++;
        }

        return lisConnections.ToArray();
    }

    private LisConnectionState GetConnectionState(int lisChannelId)
    {
        var statuses = connectionPool.GetConnectionStates(lisChannelId);

        if (statuses.All(x => x.State == LisConnectionState.Open))
        {
            return LisConnectionState.Open;
        }

        if (statuses.Any(x => x.State == LisConnectionState.Broken))
        {
            return LisConnectionState.Broken;
        }

        if (statuses.Any(x => x.State == LisConnectionState.Connecting))
        {
            return LisConnectionState.Connecting;
        }   
        return LisConnectionState.Closed;
    }
}