namespace Pharos.Server.SystemUpdate.Application.Interfaces;

internal interface IUpdateService
{
    List<string> GetAvailableUpdates(CancellationToken cancellationToken = default);
    Task InitiateUpdate(CancellationToken cancellationToken = default);
}