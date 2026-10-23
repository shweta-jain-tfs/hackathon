using LabCommunity.API.Client;

namespace Pharos.Server.LabCommunity.Application.Interfaces.InstrumentStatus;

internal interface IInstrumentStatusSnapshotProvider
{
    InstrumentStatusRequest CreateSnapshot();
}
