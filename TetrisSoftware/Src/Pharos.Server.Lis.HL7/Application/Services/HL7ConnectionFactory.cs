using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Infrastructure;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.HL7.Application.Interfaces;

namespace Pharos.Server.Lis.HL7.Application.Services;

internal sealed class HL7ConnectionFactory(
    ILisTransportConnectionFactory transportConnectionFactory,
    IHL7DataLinkFactory dataLinkFactory,
    IServiceScopeFactory serviceScopeFactory,
    ILoggerFactory loggerFactory, IDateTime dateTime) : ILisConnectionFactory
{
    public ILisConnection CreateConnection(int channelId, LisChannel channelSettings, LisConnectionRole role)
    {
        return new HL7Connection(
            channelId,
            channelSettings,
            GetConnectionRole(channelSettings, role),
            transportConnectionFactory,
            dataLinkFactory,
            loggerFactory.CreateLogger<HL7Connection>(),
            serviceScopeFactory,
            dateTime
            );
    }

    private static LisConnectionRole GetConnectionRole(LisChannel channelSettings, LisConnectionRole role)
    {
        if (role != LisConnectionRole.Import)
        {
            return role;
        }

        return channelSettings.HL7Settings?.CommunicationSettings.TcpSettings.UseSeparateExportPort == true
            ? LisConnectionRole.Import
            : LisConnectionRole.Both;
    }
}
