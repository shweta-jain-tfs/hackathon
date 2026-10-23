namespace Pharos.Server.LabCommunity.Application.Interfaces.PharosStatus;

internal interface ILabCommunityPharosStatusService
{
    Task<bool> SendStatusAsync(CancellationToken cancellationToken = default);
}
