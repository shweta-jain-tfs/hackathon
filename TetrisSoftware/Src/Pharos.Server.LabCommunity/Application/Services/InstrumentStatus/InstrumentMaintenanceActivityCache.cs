using System.Collections.Concurrent;
using LabCommunity.API.Client;
using Pharos.Server.LabCommunity.Application.Interfaces.InstrumentStatus;

namespace Pharos.Server.LabCommunity.Application.Services.InstrumentStatus;

internal sealed class InstrumentMaintenanceActivityCache : IInstrumentMaintenanceActivityCache
{
    private readonly ConcurrentDictionary<string, MaintenanceActivity> _informationByInstrumentNumber =
        new(StringComparer.Ordinal);

    public MaintenanceActivity? Get(string instrumentNumber)
    {
        return _informationByInstrumentNumber.GetValueOrDefault(Normalize(instrumentNumber));
    }

    public void Set(string instrumentNumber, MaintenanceActivity information)
    {
        _informationByInstrumentNumber[Normalize(instrumentNumber)] = information;
    }

    public void Remove(string instrumentNumber)
    {
        _informationByInstrumentNumber.TryRemove(Normalize(instrumentNumber), out _);
    }

    private static string Normalize(string instrumentNumber)
    {
        return instrumentNumber.Trim();
    }
}
