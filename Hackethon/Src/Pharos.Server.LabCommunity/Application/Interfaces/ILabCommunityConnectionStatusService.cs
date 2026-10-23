namespace Pharos.Server.LabCommunity.Application.Interfaces;

internal interface ILabCommunityConnectionStatusService
{
    /// <summary>
    /// Returns whether Pharos is connected to LabCommunity.
    /// </summary>
    Task<bool> IsConnected(CancellationToken cancellationToken = default);
}
