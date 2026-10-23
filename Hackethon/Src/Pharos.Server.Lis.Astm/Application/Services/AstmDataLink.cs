using System.Text;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Settings.Lis;

namespace Pharos.Server.Lis.Astm.Application.Services;

internal sealed class AstmDataLink : IAstmDataLink
{
    /// <summary>
    /// Default ASTM data link read timeout
    /// </summary>
    private const int _readTimeoutMilliseconds = 15 * 1000;
    private const int _maxResendAttempts = 5;
    private readonly AstmSettings _settings;
    private readonly IAstmFrameBuilder _frameBuilder;
    private readonly ILisMessageParser<IAstmMessage> _messageParser;
    private readonly ILogger<AstmDataLink> _logger;
    private ILisTransportConnection? _connection;
    private readonly object _sendReceiveLock = new();
    private Task? _sendDelayTask;
    private CancellationTokenSource? _cts;
    private Task? _transmissionWaitTask;


    public AstmDataLink(AstmSettings settings, IAstmFrameBuilder frameBuilder, ILisMessageParser<IAstmMessage> messageParser, ILogger<AstmDataLink> logger)
    {
        _settings = settings;
        _frameBuilder = frameBuilder;
        _messageParser = messageParser;
        _logger = logger;
    }

    public event EventHandler<IAstmMessage>? MessageReceived;

    private Encoding MessageEncoding => _settings.Other.Encoding;

    public void Initialize(ILisTransportConnection connection)
    {
        _logger.LogInformation("Initializing ASTM data link.");

        _connection = connection;
        _connection.StateChanged += OnConnectionStateChanged;
        _connection.ReadTimeout = _readTimeoutMilliseconds;
        if (connection.State is LisConnectionState.Open)
        {
            StartWaitingForTransmission();
        }
    }

    public Task<bool> SendMessageAsync(string messageData)
    {
        if (_cts == null)
        {
            _logger.LogWarning("Cannot send message: data link not ready (connection may not be open)");
            return Task.FromResult(false);
        }
        // create frames from the message
        var messageBytes = MessageEncoding.GetBytes(messageData);

        if (AstmControlSymbols.ContainsControlSymbols(messageBytes))
        {
            // TODO : Handle this as a LIS invalid message error instead of transport-only logging.
            throw new AstmException("The message contains illegal characters.");
        }

        var cancellationToken = _cts.Token;

        return Task.Run(async () =>
        {
            if (_sendDelayTask is { IsCompleted: false })
            {
                await _sendDelayTask.ConfigureAwait(false);
            }
            _sendDelayTask = null;

            Monitor.Enter(_sendReceiveLock);
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                SendEnq();

                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                var response = ReadAck();
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                if (response != AstmControlSymbols.ACK)
                {
                    if (response == AstmControlSymbols.ENQ)
                    {
                        _logger.LogDebug("Systems are in contention.");

                        // According to ASTM if the systems are in contention, we must wait before
                        // we try to establish a new transmission
                        _logger.LogDebug("Waiting for {ContentionTimeout}s after contention", _settings.CommunicationSettings.EnqCompetitionTimeout);
                        _sendDelayTask = Task.Delay(_settings.CommunicationSettings.EnqCompetitionTimeout * 1000, cancellationToken);
                    }
                    else if (response == AstmControlSymbols.NAK)
                    {
                        _logger.LogDebug("LIS is not ready to accept information.");

                        // According to ASTM if the receiver is not ready, the sender must wait at least
                        // 10 seconds before it tries to establish a new transmission.
                        var notReadyTimeoutSeconds = 10;
                        _logger.LogDebug("Waiting for {NotReadyTimeout}s after, because LIS is not ready.", notReadyTimeoutSeconds);
                        _sendDelayTask = Task.Delay(notReadyTimeoutSeconds * 1000, cancellationToken);
                    }
                    else
                    {
                        _logger.LogDebug("Received an unexpected response from LIS.");
                    }

                    return false;
                }
                else
                {
                    _logger.LogDebug("LIS is ready to receive a message.");
                }

                // Transmission is established, we can start sending message
                var frames = _frameBuilder.CreateFramesFromMessage(messageBytes).ToArray();

                var resendAttempts = 0;
                for (var i = 0; i < frames.Length;)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return false;
                    }

                    _logger.LogDebug("Sending frame {FrameIndex} of {FrameCount}", i + 1, frames.Length);

                    var frameBytes = frames[i].GetBytes();

                    LogFrameDetails(frames[i], frameBytes);
                    _connection?.WriteData(frameBytes);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return false;
                    }

                    response = ReadAck();
                    switch (response)
                    {
                        case AstmControlSymbols.ACK:
                            _logger.LogDebug("ACK - The message has been acknowledged by LIS.");
                            i++;
                            resendAttempts = 0;
                            continue;

                        case AstmControlSymbols.NAK:
                            _logger.LogDebug("NAK - The message has not been acknowledged by LIS.");
                            if (resendAttempts > _maxResendAttempts)
                            {
                                _logger.LogDebug("Stopping the transmission after {ResendAttempts}.", resendAttempts);
                                return false;
                            }

                            resendAttempts++;
                            continue;

                        case AstmControlSymbols.EOT:
                            _logger.LogDebug("EOT - LIS requested to stop the transmission.");
                            return false;

                        default:
                            throw new AstmException("LIS has sent an unexpected response.");
                    }
                }

                // terminate the transmission
                SendEot();

                return true;
            }
            catch (Exception e)
            {
                // TODO : Handle this as a LIS communication error instead of transport-only logging.
                _logger.LogError(e, "Error transmitting a message.");
                return false;
            }
            finally
            {
                Monitor.Exit(_sendReceiveLock);
            }
        }, cancellationToken);
    }

    private void OnConnectionStateChanged(object? sender, LisConnectionStateChangedEventArgs e)
    {
        if (e.NewState == LisConnectionState.Open)
        {
            StartWaitingForTransmission();
        }
        else if (e.NewState == LisConnectionState.Closed)
        {
            StopWaitingForTransmission();
        }
    }

    private void StartWaitingForTransmission()
    {
        if (_transmissionWaitTask is { IsCompleted: false })
        {
            return;
        }

        _cts = new CancellationTokenSource();

        var cancellationToken = _cts.Token;

        _transmissionWaitTask = Task.Run(() =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                Monitor.Enter(_sendReceiveLock);
                try
                {
                    // After we have received the "read lock" we want to take a pause and wait
                    // for data to arrive. We don't want to do this with a blocking read, because
                    // we may get unnecessary exceptions.
                    // The reason to put delay before checking the DataAvailable property is this:
                    // after the data link sends a message and releases the lock, we want to give
                    // LIS some time to react and respond without getting into contention too often
                    // (if we have queued messages we want to send).
                    Thread.Sleep(10);

                    if (!(_connection?.DataAvailable ?? false))
                    {
                        continue;
                    }

                    var data = new byte[1];
                    var bytesRead = _connection.ReadData(data);

                    if (!cancellationToken.IsCancellationRequested && bytesRead > 0 && data[0] == AstmControlSymbols.ENQ)
                    {
                        // incoming transmission request
                        _logger.LogDebug("Received ENQ - LIS wants to initiate transmission.");
                        SendAck();

                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        ReceiveMessage(cancellationToken);

                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                    }
                }
                catch (Exception e)
                {
                    // todo : Handle this as a LIS communication error instead of transport-only logging.
                    _logger.LogError(e, "LIS communication error.");
                }
                finally
                {
                    Monitor.Exit(_sendReceiveLock);
                }
            }
        }, cancellationToken);
    }

    private void StopWaitingForTransmission()
    {
        if (_cts is { IsCancellationRequested: false })
        {
            _cts.Cancel();
            _transmissionWaitTask?.Wait();
            _cts.Dispose();
            _cts = null;
            _transmissionWaitTask = null;
        }
    }

    private void ReceiveMessage(CancellationToken cancellationToken)
    {
        IAstmMessage? message = null;

        try
        {
            var frames = new List<IAstmFrame>();
            var buffer = new byte[IAstmFrame.MaxFrameLength];

            _connection!.ReadTimeout = 30 * 1000; // 30-second wait timeout for messages from LIS
            var readBytes = _connection.ReadData(buffer);
            var expectedFrameNumber = 1;

            while (readBytes > 0)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                if (buffer[0] == AstmControlSymbols.STX)
                {
                    _logger.LogDebug("STX - Receiving a frame.");
                    try
                    {
                        // make sure that the frame is fully read
                        var frameBytes = readBytes;
                        while (readBytes > 0 && frameBytes < IAstmFrame.MaxFrameLength && buffer[frameBytes - 1] != AstmControlSymbols.LF)
                        {
                            readBytes = _connection.ReadData(buffer, frameBytes, IAstmFrame.MaxFrameLength - frameBytes);
                            frameBytes += readBytes;
                        }

                        var frame = _frameBuilder.CreateFrameFromBytes(buffer, expectedFrameNumber);
                        LogFrameDetails(frame, frame.GetBytes());

                        expectedFrameNumber = (expectedFrameNumber + 1) % 8;
                        frames.Add(frame);

                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        SendAck();
                    }
                    catch (AstmException e)
                    {
                        // TODO: Handle this as a LIS invalid message error instead of transport-only logging.
                        _logger.LogError(e, "Error building an ASTM frame.");
                        SendNak();
                    }
                }
                else if (buffer[0] == AstmControlSymbols.EOT)
                {
                    _logger.LogDebug("EOT - LIS finished the transmission.");
                    if (frames.Count > 0)
                    {
                        message = ParseMessage(frames);
                    }

                    return;
                }
                else
                {
                    // we have received something unexpected and we want to quit the transmission
                    SendNak();
                    return;
                }
                readBytes = _connection.ReadData(buffer);
            }
        }
        catch (Exception e)
        {
            // TODO: Handle parsing/receive failures as LIS invalid message errors.
            _logger.LogError(e, "Error receiving an ASTM message.");
        }
        finally
        {
            if (message != null && !cancellationToken.IsCancellationRequested)
            {
                MessageReceived?.Invoke(this, message);
            }
        }
    }

    private IAstmMessage ParseMessage(IEnumerable<IAstmFrame> frames)
    {
        _logger.LogDebug("Parsing an ASTM message from the frames.");
        var frameList = frames.ToList();
        // combine data from all frames into one message
        var messageData = new byte[frameList.Sum(x => x.MessageData.Length)];
        var index = 0;
        foreach (var frame in frameList)
        {
            frame.MessageData.CopyTo(messageData, index);
            index += frame.MessageData.Length;
        }

        // convert binary message data to a string
        var messageString = MessageEncoding.GetString(messageData);

        return _messageParser.ParseMessage(messageString);
    }

    private void SendAck()
    {
        _logger.LogDebug("Sending ACK");
        _connection!.WriteData(new[] { AstmControlSymbols.ACK });
    }

    private void SendEnq()
    {
        _logger.LogDebug("Sending ENQ - Initiating transmission");
        _connection!.WriteData(new[] { AstmControlSymbols.ENQ });
    }

    private void SendEot()
    {
        _logger.LogDebug("Sending EOT");
        _connection!.WriteData(new[] { AstmControlSymbols.EOT });
    }

    private void SendNak()
    {
        _logger.LogDebug("Sending NAK");
        _connection!.WriteData(new[] { AstmControlSymbols.NAK });
    }

    private byte ReadAck()
    {
        _logger.LogDebug("Waiting for acknowledgement.");
        var buffer = new byte[1];
        var read = _connection!.ReadData(buffer);
        if (read == 0)
        {
            throw new TimeoutException($"LIS didn't respond within {_readTimeoutMilliseconds} ms");
        }

        return buffer[0];
    }

    private void LogFrameDetails(IAstmFrame frame, byte[] frameBytes)
    {
        var dataLength = frame.MessageData.Length;
        var trailerIndex = dataLength + 2;
        var terminator = frameBytes[trailerIndex] == AstmControlSymbols.ETX ? "ETX" : "ETB";
        var checksum0 = frameBytes[trailerIndex + 1];
        var checksum1 = frameBytes[trailerIndex + 2];
        var cr = frameBytes[trailerIndex + 3] == AstmControlSymbols.CR ? "CR" : frameBytes[trailerIndex + 3].ToString();
        var lf = frameBytes[trailerIndex + 4] == AstmControlSymbols.LF ? "LF" : frameBytes[trailerIndex + 4].ToString();

        _logger.LogDebug("Frame Details {FrameNumber}: Length={Length} bytes, FrameTerminator={FrameTerminator}, CheckSum={CheckSum0:X2}{CheckSum1:X2}, CR={CR}, LF={LF}",
            frame.FrameNumber,
            dataLength,
            terminator,
            checksum0,
            checksum1,
            cr,
            lf);
    }
}
