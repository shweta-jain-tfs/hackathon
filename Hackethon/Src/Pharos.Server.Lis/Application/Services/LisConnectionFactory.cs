using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Interfaces;

namespace Pharos.Server.Lis.Application.Services;

internal sealed class LisConnectionFactory(
    [FromKeyedServices(LisServiceKeys.AstmConnectionFactory)] ILisConnectionFactory astmConnectionFactory,
    [FromKeyedServices(LisServiceKeys.HL7ConnectionFactory)] ILisConnectionFactory hl7ConnectionFactory)
    : ILisConnectionFactory
{
    public ILisConnection CreateConnection(int channelId, LisChannel channelSettings, LisConnectionRole role)
    {
        return channelSettings.ProtocolType switch
        {
            LisProtocolType.Astm => astmConnectionFactory.CreateConnection(channelId, channelSettings, role),
            LisProtocolType.HL7 => hl7ConnectionFactory.CreateConnection(channelId, channelSettings, role),
            _ => throw new NotImplementedException()
        };
    }
}
