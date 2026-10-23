using System.IO.Ports;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Interfaces;

namespace Pharos.Server.Lis.Infrastructure.Transport;

/// <summary>
/// Handles low level RS232 communication
/// </summary>
internal sealed class Rs232Connection : ILisTransportConnection, IDisposable
{
    private SerialPort? _port;
    private readonly LisRs232Settings _settings;
    private readonly ILogger<Rs232Connection> _logger;

    public int ReadTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public LisConnectionState State
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                StateChanged?.Invoke(this, new LisConnectionStateChangedEventArgs(value));
            }
        }
    } = LisConnectionState.Closed;

    public Rs232Connection(LisRs232Settings settings, ILogger<Rs232Connection> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public bool DataAvailable => State == LisConnectionState.Open && _port is { BytesToRead: > 0 };

    public event EventHandler<LisConnectionStateChangedEventArgs>? StateChanged;

    public void Connect()
    {
        try
        {
            _logger.LogInformation("Opening RS232 connection on the COM port {ComPort}", _settings.Port);

            if (_settings.Port > 0)
            {
                _port = new SerialPort($"COM{_settings.Port}", _settings.BaudRate, (Parity)_settings.Parity, (int)_settings.DataBits)
                {
                    Handshake = (Handshake)_settings.Handshake,
                    DtrEnable = _settings.EnableDtr,
                    RtsEnable = _settings.EnableRts
                };

                _port.PinChanged += new SerialPinChangedEventHandler(OnComPortPinChanged);
                _port.ErrorReceived += new SerialErrorReceivedEventHandler(OnComPortErrorReceived);
                _port.Open();
                State = LisConnectionState.Open;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error opening RS232 connection on the COM port {ComPort}", _settings.Port);
        }
    }

    public void Close()
    {
        try
        {
            _logger.LogInformation("Closing RS232 connection on the COM port {ComPort}", _settings.Port);
            if (_port != null)
            {
                _port.PinChanged -= new SerialPinChangedEventHandler(OnComPortPinChanged);
                _port.ErrorReceived -= new SerialErrorReceivedEventHandler(OnComPortErrorReceived);
                _port.Close();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error closing RS232 connection on the COM port {ComPort}", _settings.Port);
        }
        finally
        {
            State = LisConnectionState.Closed;
        }
    }

    public int ReadData(byte[] buffer)
    {
        return ReadData(buffer, 0, buffer.Length);
    }

    public int ReadData(byte[] buffer, int offset, int count)
    {
        try
        {
            if (_port == null || _port.IsOpen)
            {
                throw new InvalidOperationException("Connection is closed.");
            }

            return _port!.Read(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing data to the COM port {ComPort}", _settings.Port);
            State = LisConnectionState.Broken;
            throw;
        }
    }

    public void WriteData(byte[] data)
    {
        try
        {
            if (_port == null || _port.IsOpen)
            {
                throw new InvalidOperationException("Connection is closed.");
            }
            _port!.Write(data, 0, data.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing data to the COM port {ComPort}", _settings.Port);
            State = LisConnectionState.Broken;
            throw;
        }
    }

    private void OnComPortErrorReceived(object sender, SerialErrorReceivedEventArgs e)
    {
        switch (e.EventType)
        {
            case SerialError.Frame:
                _logger.LogError("COM port error: The hardware detected a framing error. COM port {ComPort}", _settings.Port);
                break;
            case SerialError.Overrun:
                _logger.LogError("COM port error: A character-buffer overrun has occurred. COM port {ComPort}", _settings.Port);
                break;
            case SerialError.RXOver:
                _logger.LogError("COM port error: An input buffer overflow has occurred. COM port {ComPort}", _settings.Port);
                break;
            case SerialError.RXParity:
                _logger.LogError("COM port error: The hardware detected a parity error. COM port {ComPort}", _settings.Port);
                break;
            case SerialError.TXFull:
                _logger.LogError("COM port error: The application tried to transmit a character, but the output buffer was full. COM port {ComPort}", _settings.Port);
                break;
        }
    }

    private void OnComPortPinChanged(object sender, SerialPinChangedEventArgs e)
    {
        switch (e.EventType)
        {
            case SerialPinChange.Break:
                _logger.LogDebug("COM pin change: A Break was received.");
                break;
            case SerialPinChange.CDChanged:
                _logger.LogDebug("COM pin change: Change in the CD line.");
                break;
            case SerialPinChange.CtsChanged:
                _logger.LogDebug("COM pin change: Change in the CTS line.");
                break;
            case SerialPinChange.DsrChanged:
                _logger.LogDebug("COM pin change: Change in the DSR line.");
                break;
            case SerialPinChange.Ring:
                _logger.LogDebug("COM pin change: Change in the Ring indicator.");
                break;
        }
    }

    public void Dispose()
    {
        _port?.Dispose();
    }
}
