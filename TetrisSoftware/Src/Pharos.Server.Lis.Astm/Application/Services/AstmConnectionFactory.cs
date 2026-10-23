using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Infrastructure;

namespace Pharos.Server.Lis.Astm.Application.Services;

internal sealed class AstmConnectionFactory(
    ILisTransportConnectionFactory transportConnectionFactory,
    IAstmDataLinkFactory dataLinkFactory,
    IDateTime dateTime,
    ILoggerFactory loggerFactory,
    IServiceScopeFactory serviceScopeFactory) : ILisConnectionFactory
{
    public ILisConnection CreateConnection(int channelId, LisChannel channelSettings, LisConnectionRole role)
    {
        var connectionRole = GetConnectionRole(channelSettings, role);

        return new AstmConnection(
            channelId,
            channelSettings,
            connectionRole,
            transportConnectionFactory,
            dataLinkFactory,
            dateTime,
            loggerFactory.CreateLogger<AstmConnection>(),
            serviceScopeFactory);
    }

    private static LisConnectionRole GetConnectionRole(LisChannel channelSettings, LisConnectionRole role)
    {
        if (role != LisConnectionRole.Import)
        {
            return role;
        }

        var hasSeparateExportPort = channelSettings.AstmSettings?.CommunicationSettings?.CommunicationMode == AstmCommunicationMode.Tcp
            && (channelSettings.AstmSettings.CommunicationSettings.TcpSettings?.UseSeparateExportPort ?? false);

        return hasSeparateExportPort ? LisConnectionRole.Import : LisConnectionRole.Both;
    }
}
