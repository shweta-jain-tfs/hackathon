using Pharos.Server.SystemUpdate.Domain;

namespace Pharos.Server.SystemUpdate.Application.Interfaces;

internal interface IUpdateMediumStateStore
{
    UpdateMediumState? Get();
    void Set(UpdateMediumState state);
    void Clear();
}