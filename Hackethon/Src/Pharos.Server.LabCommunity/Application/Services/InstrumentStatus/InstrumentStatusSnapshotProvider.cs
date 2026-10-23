

using LabCommunity.API.Client;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Infrastructure;
using Pharos.Server.LabCommunity.Application.Interfaces.InstrumentStatus;
using Pharos.Server.LabCommunity.Application.Mapping;
using ApiInstrument = LabCommunity.API.Client.Instrument;

namespace Pharos.Server.LabCommunity.Application.Services.InstrumentStatus;

internal sealed class InstrumentStatusSnapshotProvider(
    IInstrumentManager instrumentManager,
    IDateTime dateTime,
    IInstrumentMaintenanceActivityCache maintenanceActivityCache) : IInstrumentStatusSnapshotProvider
{
    public InstrumentStatusRequest CreateSnapshot()
    {
        var instrumentInfos = instrumentManager.GetInstruments();

        var instruments = new List<ApiInstrument>();
        foreach (var instrument in instrumentInfos)
        {
            var mappedInstrument = MapInstrument(instrument);
            instruments.Add(mappedInstrument);
        }

        return new InstrumentStatusRequest
        {
            ReportedAt = dateTime.UtcNow,
            Instruments = instruments
        };
    }

    private ApiInstrument MapInstrument(IInstrument instrument)
    {
        var sensorReadings = instrument.EnvironmentSensors;

        return new ApiInstrument
        {
            Type = instrument.InstrumentType.ToLabCommunityInstrumentType(),
            SerialNumber = instrument.SerialNumber,
            SensorReadings = sensorReadings
                .Select(reading => new SensorReading
                {
                    SensorType = reading.Type.ToString(),
                    Value = reading.Value
                })
                .ToList(),
            Software = new InstrumentSoftwareStatus
            {
                State = instrument.State.ToString(),
                Version = instrument.InstrumentSoftwareVersion
            },
            MaintenanceActivity = maintenanceActivityCache.Get(instrument.SerialNumber),
            InstallationDate = instrument.InstallationDate,
            Modules = (instrument.Modules ?? Array.Empty<HardwareModule>()).Select(module => new InstrumentModule
            {
                ModuleType = module.ModuleType.ToString(),
                ArticleNumber = module.ArticleNumber,
                Model = module.Model,
                Version = module.Version,
                SerialNumber = module.SerialNumber,
                InstallationDate = module.InstallationDate
            }).ToList()
        };
    }
}