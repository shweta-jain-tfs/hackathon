using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.LabCommunity;

namespace Pharos.Server.LabCommunity.Application.Queries;

internal sealed record IsLabCommunityEnabledQuery : IRequest<bool>;

internal sealed class IsLabCommunityEnabledQueryHandler(
    IOptionsSnapshot<LabCommunitySettings> settingsSnapshot)
    : IRequestHandler<IsLabCommunityEnabledQuery, bool>
{
    public Task<bool> Handle(IsLabCommunityEnabledQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(settingsSnapshot.Value.Enable);
    }
}
