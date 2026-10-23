using Pharos.Server.SystemUpdate.Application.Interfaces;
using Pharos.Server.SystemUpdate.Domain;

namespace Pharos.Server.SystemUpdate.Application.Services;

internal sealed class InMemoryUpdateMediumStateStore : IUpdateMediumStateStore
{
    private UpdateMediumState? _state;

    public UpdateMediumState? Get()
    {
        return _state;
    }

    public void Set(UpdateMediumState state)
    {
        _state = state;
    }

    public void Clear()
    {
        _state = null;
    }
}