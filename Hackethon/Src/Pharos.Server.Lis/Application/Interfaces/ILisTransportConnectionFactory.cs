using System.Net;
using Pharos.Server.Application.Settings.Lis;

namespace Pharos.Server.Lis.Application.Interfaces;

/// <summary>
/// Defines a factory for creating LIS transport connections
/// </summary>
public interface ILisTransportConnectionFactory
{
    ILisTransportConnection CreateTcpServerConnection(int port);

    ILisTransportConnection CreateTcpClientConnection(IPAddress ipAddress, int port, int reconnectIntervalMilliseconds);

    ILisTransportConnection CreateRs232Connection(LisRs232Settings settings);
}
