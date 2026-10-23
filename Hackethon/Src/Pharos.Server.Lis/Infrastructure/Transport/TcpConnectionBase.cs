using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Pharos.Server.Lis.Application.Interfaces;

namespace Pharos.Server.Lis.Infrastructure.Transport;

internal abstract class TcpConnectionBase : ILisTransportConnection, IDisposable
{
    protected readonly object _lock = new();

    protected TcpConnectionBase(ILogger<TcpConnectionBase> logger)
    {
        Logger = logger;
    }

    public int ReadTimeout { get; set; }

    public LisConnectionState State
    {
        get;
        protected set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnStateChanged(value);
        }
    } = LisConnectionState.Closed;

    public bool DataAvailable => State == LisConnectionState.Open && (Client?.GetStream().DataAvailable ?? false);

    public event EventHandler<LisConnectionStateChangedEventArgs>? StateChanged;

    protected TcpClient? Client { get; set; }

    protected ILogger<TcpConnectionBase> Logger { get; }

    public abstract void Connect();

    public abstract void Close();

    public virtual void Dispose()
    {
        Client?.Dispose();
        Client = null;
    }

    public int ReadData(byte[] buffer)
    {
        return ReadData(buffer, 0, buffer.Length);
    }

    public int ReadData(byte[] buffer, int offset, int count)
    {
        try
        {
            lock (_lock)
            {
                var stream = Client!.GetStream();
                stream.ReadTimeout = ReadTimeout;
                var read = stream.Read(buffer, offset, count);
                return read;
            }
        }
        catch (Exception e)
        {
            if (e.InnerException is SocketException { SocketErrorCode: SocketError.TimedOut })
            {
                return 0;
            }

            Logger.LogError(e, "Error reading from the socket.");
            State = LisConnectionState.Broken;
            throw;
        }
    }

    public void WriteData(byte[] data)
    {
        try
        {
            lock (_lock)
            {
                Client!.GetStream().Write(data);
            }
        }
        catch (Exception e)
        {
            if (e.InnerException is not SocketException { SocketErrorCode: SocketError.TimedOut })
            {
                Logger.LogError(e, "Error writing data to the socket.");
                State = LisConnectionState.Broken;
            }

            throw;
        }
    }

    protected virtual void OnStateChanged(LisConnectionState newState)
    {
        StateChanged?.Invoke(this, new LisConnectionStateChangedEventArgs(newState));
    }

}
