using Microsoft.Extensions.Options;
using Pharos.Contracts;
using Pharos.Server.Application;
using Pharos.Server.Settings;

namespace Pharos.Server.Services;

internal class ClientDeviceService(
    IHostEnvironment hostEnvironment,
    ICurrentInstrumentService currentInstrument,
    IOptions<ClientSettings> clientOptions) : IClientDeviceService
{
    public ClientDeviceType ClientDeviceType
    {
        get
        {

            // first, we want to check if the developer has overridden the client device type
            if (hostEnvironment.IsDevelopment() && clientOptions.Value.ForcedClientDeviceType.HasValue)
            {
                return clientOptions.Value.ForcedClientDeviceType.Value;
            }

            // second, we want to detect if this is a call from an instrument
            if (currentInstrument.InstrumentId != null)
            {
                return ClientDeviceType.Instrument;
            }

            return ClientDeviceType.Auto;
        }
    }
}
