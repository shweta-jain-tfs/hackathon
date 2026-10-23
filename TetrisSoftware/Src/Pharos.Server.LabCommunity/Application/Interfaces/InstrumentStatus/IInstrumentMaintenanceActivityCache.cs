using LabCommunity.API.Client;

namespace Pharos.Server.LabCommunity.Application.Interfaces.InstrumentStatus;

internal interface IInstrumentMaintenanceActivityCache
{
    MaintenanceActivity? Get(string instrumentNumber);

    void Set(string instrumentNumber, MaintenanceActivity information);

    void Remove(string instrumentNumber);
}
