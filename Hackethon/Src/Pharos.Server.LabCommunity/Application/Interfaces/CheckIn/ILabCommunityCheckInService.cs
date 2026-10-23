namespace Pharos.Server.LabCommunity.Application.Interfaces.CheckIn;

internal interface ILabCommunityCheckInService
{
    Task<bool> CheckIn(CancellationToken cancellationToken = default);
}
