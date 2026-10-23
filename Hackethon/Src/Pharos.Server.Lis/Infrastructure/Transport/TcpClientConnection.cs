using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Pharos.Server.Lis.Application.Interfaces;

namespace Pharos.Server.Lis.Infrastructure.Transport;

internal sealed class TcpClientConnection : TcpConnectionBase
{
    private readonly IPAddress _ipAddress;
    private readonly int _port;
    private readonly int _reconnectIntervalMilliseconds;
    private Timer? _reconnectTimer;

    // for limiting log output
    private bool _exceptionLogged;
    private bool _messageLogged;

    public TcpClientConnection(IPAddress ipAddress, int port, int reconnectIntervalMilliseconds, ILogger<TcpClientConnection> logger) : base(logger)
    {
        _ipAddress = ipAddress;
        _port = port;
        _reconnectIntervalMilliseconds = reconnectIntervalMilliseconds;
    }

    public override void Connect()
    {
        try
        {
            lock (_lock)
            {
                if (State is LisConnectionState.Open)
                {
                    return;
                }
                StopReconnectTimer();
                if (!_messageLogged)
                {
                    Logger.LogInformation("Opening TCP connection to a remote endpoint {IpAddress}:{Port}", _ipAddress, _port);
                    _messageLogged = true;
                }

                Client = new TcpClient();
                Client.Connect(new IPEndPoint(_ipAddress, _port));
            }

            _messageLogged = false;
            _exceptionLogged = false;
            State = LisConnectionState.Open;
        }
        catch (Exception e)
        {
            if (e.InnerException is SocketException)
            {
                Logger.LogError(e, "Socket error opening TCP connection to a remote endpoint {IpAddress}:{Port}", _ipAddress, _port);
                _exceptionLogged = true;
            }

            if (!_exceptionLogged)
            {
                Logger.LogError(e, "Error opening TCP connection to a remote endpoint {IpAddress}:{Port}", _ipAddress, _port);
                _exceptionLogged = true;
            }
            StartReconnectTimer();
        }
    }

    public override void Close()
    {
        try
        {
            lock (_lock)
            {
                if (State is LisConnectionState.Closed or LisConnectionState.Connecting)
                {
                    return;
                }

                Logger.LogInformation("Closing TCP client connection.");
                StopReconnectTimer();

                Client?.Close();
                Client = null;
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error occurred during closing closing TCP client at {IpAddress}:{Port}", _ipAddress, _port);
        }
        State = LisConnectionState.Closed;
    }

    public override void Dispose()
    {
        StopReconnectTimer();
        base.Dispose();
    }

    private void StartReconnectTimer()
    {
        if (_reconnectIntervalMilliseconds <= 0)
        {
            Logger.LogInformation("Reconnect interval is set to {ReconnectInterval}. Reconnect timer will not be started.", _reconnectIntervalMilliseconds);
            return;
        }

        _reconnectTimer ??= new Timer(_ =>
            {
                Connect();
            }, null, _reconnectIntervalMilliseconds, _reconnectIntervalMilliseconds);
    }

    private void StopReconnectTimer()
    {
        _reconnectTimer?.Dispose();
        _reconnectTimer = null;
    }
}
