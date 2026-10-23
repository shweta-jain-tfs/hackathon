using Pharos.Server.SystemUpdate.Application.Interfaces;

namespace Pharos.Server.SystemUpdate.Application.Services;

internal class RemoteUpdateService : IUpdateService
{
    public List<string> GetAvailableUpdates(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task InitiateUpdate(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}