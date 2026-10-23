using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Infrastructure.Transport;

namespace Pharos.Server.Lis.Application.Services;

internal sealed class LisTransportConnectionFactory : ILisTransportConnectionFactory
{
    private readonly IServiceProvider _provider;

    public LisTransportConnectionFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public ILisTransportConnection CreateRs232Connection(LisRs232Settings settings)
    {
        return new Rs232Connection(settings, _provider.GetRequiredService<ILogger<Rs232Connection>>());
    }

    public ILisTransportConnection CreateTcpClientConnection(IPAddress ipAddress, int port, int reconnectIntervalMilliseconds)
    {
        return new TcpClientConnection(ipAddress, port, reconnectIntervalMilliseconds, _provider.GetRequiredService<ILogger<TcpClientConnection>>());
    }

    public ILisTransportConnection CreateTcpServerConnection(int port)
    {
        return new TcpServerConnection(port, _provider.GetRequiredService<ILogger<TcpServerConnection>>());
    }
}
