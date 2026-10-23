using MediatR;
using Pharos.Server.LabCommunity.Application.Interfaces;

namespace Pharos.Server.LabCommunity.Application.Queries;

internal sealed record GetLabCommunityConnectionStatusQuery : IRequest<LabCommunityConnectionStatusDto>;

internal sealed class GetLabCommunityConnectionStatusQueryHandler(
    ILabCommunityConnectionStatusService connectionStatusService)
    : IRequestHandler<GetLabCommunityConnectionStatusQuery, LabCommunityConnectionStatusDto>
{
    public async Task<LabCommunityConnectionStatusDto> Handle(GetLabCommunityConnectionStatusQuery request, CancellationToken cancellationToken)
    {
        var isConnected = await connectionStatusService.IsConnected(cancellationToken);
        return new LabCommunityConnectionStatusDto(isConnected);
    }
}
