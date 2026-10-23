using System.Text;
using Microsoft.Extensions.Logging;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.HL7.Application.Common;
using Pharos.Server.Lis.HL7.Application.Interfaces;

namespace Pharos.Server.Lis.HL7.Application.Services;

internal sealed class HL7DataLink(
    IHL7FrameBuilder frameBuilder,
    ILisMessageParser<IHL7Message> messageParser,
    ILogger<HL7DataLink> logger) : IHL7DataLink
{
    private const int _readTimeoutMilliseconds = 15 * 1000;
    private const int _bufferSize = 2048;

    private readonly Encoding _encoding = Encoding.GetEncoding(1252);
    private readonly object _sendReceiveLock = new();

    private ILisTransportConnection? _connection;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;

    public event EventHandler<IHL7Message>? MessageReceived;

    public void Initialize(ILisTransportConnection connection)
    {
        logger.LogInformation("Initializing HL7 data link.");

        _connection = connection;
        _connection.ReadTimeout = _readTimeoutMilliseconds;
        _connection.StateChanged += OnConnectionStateChanged;

        if (_connection.State == LisConnectionState.Open)
        {
            StartReceiving();
        }
    }

    public Task<bool> SendMessageAsync(string messageData)
    {
        if (_connection?.State != LisConnectionState.Open)
        {
            logger.LogWarning("Cannot send HL7 message: transport connection is not open.");
            return Task.FromResult(false);
        }

        try
        {
            var messageBytes = _encoding.GetBytes(messageData);

            if (HL7ControlSymbols.ContainsControlSymbols(messageBytes))
            {
                throw new HL7Exception("HL7 message contains illegal frame control characters.");
            }

            var frame = frameBuilder.CreateFrameFromMessage(messageBytes);
            lock (_sendReceiveLock)
            {
                _connection.WriteData(frame);
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error transmitting HL7 message.");
            return Task.FromResult(false);
        }
    }

    private void OnConnectionStateChanged(object? sender, LisConnectionStateChangedEventArgs e)
    {
        if (e.NewState == LisConnectionState.Open)
        {
            StartReceiving();
        }
        else if (e.NewState == LisConnectionState.Closed)
        {
            StopReceiving();
        }
    }

    private void StartReceiving()
    {
        if (_receiveTask is { IsCompleted: false })
        {
            return;
        }

        _cts = new CancellationTokenSource();
        var cancellationToken = _cts.Token;

        _receiveTask = Task.Run(() =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Thread.Sleep(10);
                    if (!(_connection?.DataAvailable ?? false))
                    {
                        continue;
                    }

                    ReceiveMessages(cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error receiving HL7 messages.");
                }
            }
        }, cancellationToken);
    }

    private void StopReceiving()
    {
        if (_cts is not { IsCancellationRequested: false })
        {
            return;
        }

        _cts.Cancel();
        _receiveTask?.Wait();
        _cts.Dispose();
        _cts = null;
        _receiveTask = null;
    }

    private void ReceiveMessages(CancellationToken cancellationToken)
    {
        var frames = ReadAvailableFrames(cancellationToken);
        if (frames.Count == 0 || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        foreach (var frame in frames)
        {
            try
            {
                var messageData = frameBuilder.CreateMessageFromFrame(frame);
                var messageString = _encoding.GetString(messageData);
                var message = messageParser.ParseMessage(messageString);

                if (message != null && !cancellationToken.IsCancellationRequested)
                {
                    MessageReceived?.Invoke(this, message);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error parsing HL7 message.");
            }
        }
    }

    private List<List<byte>> ReadAvailableFrames(CancellationToken cancellationToken)
    {
        var buffer = new byte[_bufferSize];
        var frames = new List<List<byte>>();
        var currentFrame = new List<byte>(_bufferSize);
        var isReceivingFrame = false;
        var hasReceivedEndBlock = false;

        while (!cancellationToken.IsCancellationRequested)
        {
            var bytesRead = 0;
            lock (_sendReceiveLock)
            {
                bytesRead = _connection!.ReadData(buffer);
            }

            if (bytesRead == 0)
            {
                return frames;
            }

            for (var i = 0; i < bytesRead; i++)
            {
                var currentByte = buffer[i];

                if (currentByte == HL7ControlSymbols.StartBlock)
                {
                    logger.LogDebug("SB - Received HL7 start block.");

                    currentFrame.Clear();
                    currentFrame.Add(currentByte);
                    isReceivingFrame = true;
                    hasReceivedEndBlock = false;
                }
                else if (currentByte == HL7ControlSymbols.EndBlock && isReceivingFrame)
                {
                    logger.LogDebug("EB - Received HL7 end block.");

                    currentFrame.Add(currentByte);
                    hasReceivedEndBlock = true;
                }
                else if (currentByte == HL7ControlSymbols.CarriageReturn && hasReceivedEndBlock)
                {
                    logger.LogDebug("CR - Received carriage return.");

                    currentFrame.Add(currentByte);
                    frames.Add([.. currentFrame]);

                    currentFrame.Clear();
                    isReceivingFrame = false;
                    hasReceivedEndBlock = false;
                }
                else if (isReceivingFrame)
                {
                    currentFrame.Add(currentByte);
                }
            }

            if (frames.Count > 0)
            {
                return frames;
            }
        }

        return frames;
    }
}
