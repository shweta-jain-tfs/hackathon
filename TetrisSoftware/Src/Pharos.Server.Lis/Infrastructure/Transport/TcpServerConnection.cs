using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Pharos.Server.Lis.Application.Interfaces;

namespace Pharos.Server.Lis.Infrastructure.Transport;

internal sealed class TcpServerConnection : TcpConnectionBase
{
    private readonly int _port;
    private TcpListener? _server;

    public TcpServerConnection(int port, ILogger<TcpServerConnection> logger) : base(logger)
    {
        _port = port;
    }

    public override void Connect()
    {
        lock (_lock)
        {
            if (State != LisConnectionState.Closed)
            {
                return;
            }
            try
            {
                Logger.LogInformation("Starting TCP listener at {IpAddress}:{Port}", IPAddress.Loopback, _port);
                _server = new TcpListener(IPAddress.Loopback, _port);
                _server.Start();
                State = LisConnectionState.Connecting;
                BeginAcceptingClients();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error starting TCP listener at {IpAddress}:{Port}", IPAddress.Loopback, _port);
            }
        }        
    }

    public override void Close()
    {
        try
        {
            lock (_lock)
            {
                if (State is LisConnectionState.Closed)
                {
                    return;
                }

                // Stop accepting new connections first
                _server?.Stop();

                Client?.Dispose();
                Client = null;
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error occurred during closing TCP listener at {IpAddress}:{Port}", IPAddress.Loopback, _port);
        }
        State = LisConnectionState.Closed;
    }

    public override void Dispose()
    {
        lock (_lock)
        {
            if (_server != null)
            {
                _server.Stop();
                _server.Dispose();
                _server = null;
            }
        }
        base.Dispose();
    }

    protected override void OnStateChanged(LisConnectionState newState)
    {
        base.OnStateChanged(newState);

        if (newState == LisConnectionState.Broken)
        {
            State = LisConnectionState.Connecting;
            BeginAcceptingClients();
        }
    }

    private void BeginAcceptingClients()
    {
        _server?.BeginAcceptTcpClient(OnNewClientConnected, null);
    }

    private void OnNewClientConnected(IAsyncResult ar)
    {
        try
        {
            lock (_lock)
            {
                if (State == LisConnectionState.Closed || _server == null)
                {
                    Logger.LogDebug("TCP listener is closed or disposed, ignoring client connection attempt");
                    return;
                }

                Client = _server.EndAcceptTcpClient(ar);
                Logger.LogInformation("TCP client connected at {IpAddress}:{Port}", IPAddress.Loopback, _port);
            }
            State = LisConnectionState.Open;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error accepting TCP client connection");
            BeginAcceptingClients();
        }
    }
}