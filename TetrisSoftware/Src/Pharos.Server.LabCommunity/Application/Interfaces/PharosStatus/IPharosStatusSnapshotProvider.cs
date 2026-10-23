using LabCommunity.API.Client;

namespace Pharos.Server.LabCommunity.Application.Interfaces.PharosStatus;

internal interface IPharosStatusSnapshotProvider
{
    OperatorSoftwareStatusRequest CreateSnapshot();
}
