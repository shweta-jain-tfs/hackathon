using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Infrastructure;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;
using Pharos.Server.Lis.HL7.Application.Interfaces;
using Pharos.Server.Lis.HL7.Application.Mappings;
using Pharos.Server.Lis.HL7.Application.Messages;

namespace Pharos.Server.Lis.HL7.Application.Services;

internal sealed class HL7Connection : ILisConnection
{
    private readonly ILisTransportConnectionFactory _transportConnectionFactory;
    private readonly IHL7DataLinkFactory _dataLinkFactory;
    private readonly ILogger<HL7Connection> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly HL7Settings _settings;
    private readonly IDateTime _dateTime;
    private DateTimeOffset _lastInfoRequestMessageTimestamp;
    private readonly LisImportSettings _lisImportSettings;
    private IHL7DataLink? _dataLink;
    private ILisTransportConnection? _transportConnection;

    public HL7Connection(
        int lisChannelId,
        LisChannel lisChannel,
        LisConnectionRole role,
        ILisTransportConnectionFactory transportConnectionFactory,
        IHL7DataLinkFactory dataLinkFactory,
        ILogger<HL7Connection> logger,
        IServiceScopeFactory serviceScopeFactory,
        IDateTime dateTime)
    {
        LisChannelId = lisChannelId;
        _settings = lisChannel.HL7Settings!;
        _lisImportSettings = lisChannel.ImportSettings;
        Role = role;
        _transportConnectionFactory = transportConnectionFactory;
        _dataLinkFactory = dataLinkFactory;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _dateTime = dateTime;
    }

    public LisConnectionState State { get; private set; } = LisConnectionState.Closed;

    public int LisChannelId { get; }

    public LisConnectionRole Role { get; }

    public event EventHandler<LisConnectionStateChangedEventArgs>? StateChanged;

    public event EventHandler<TestOrderReceivedEventArgs>? TestOrderReceived;

    public void Close()
    {
        if (_dataLink != null && Role.HasFlag(LisConnectionRole.Import))
        {
            _dataLink.MessageReceived -= OnDataLinkMessageReceived;
        }

        _dataLink = null;

        if (_transportConnection != null)
        {
            _transportConnection.Close();
            _transportConnection.StateChanged -= OnTransportConnectionStateChanged;
            _transportConnection = null;
        }
    }

    public Task<bool> OpenAsync()
    {
        if (State == LisConnectionState.Open)
        {
            return Task.FromResult(true);
        }

        CreateTransportConnection();

        if (_dataLink == null)
        {
            _dataLink = _dataLinkFactory.CreateHL7DataLink(_settings);
            _dataLink.Initialize(_transportConnection!);

            if (Role.HasFlag(LisConnectionRole.Import))
            {
                _dataLink.MessageReceived += OnDataLinkMessageReceived;
            }
        }

        return Task.Run(() =>
        {
            _transportConnection?.Connect();
            return _transportConnection?.State != LisConnectionState.Closed;
        });
    }

    public async Task<bool> SendMessageAsync(LisMessageType messageType, string messageData)
    {
        var canSend = Role.HasFlag(LisConnectionRole.Export)
            || (messageType == LisMessageType.Acknowledgement && Role.HasFlag(LisConnectionRole.Import));

        if (!canSend)
        {
            _logger.LogError("Connection role {Role} does not support sending HL7 messages of type {MessageType}.", Role, messageType);
            return false;
        }

        if (messageType == LisMessageType.Unknown)
        {
            _logger.LogError("Unsupported HL7 message type '{MessageType}'", messageType);
            return false;
        }

        if (_dataLink == null)
        {
            _logger.LogError("HL7 data link is not initialized for message type '{MessageType}'", messageType);
            return false;
        }

        if (messageType == LisMessageType.TestOrderQuery)
        {
            await AddDelayBetweenInfoRequestMessages().ConfigureAwait(false);
        }

        var result = await _dataLink.SendMessageAsync(messageData);

        return result;
    }

    private void CreateTransportConnection()
    {
        if (_transportConnection != null)
        {
            return;
        }

        var tcpSettings = _settings.CommunicationSettings.TcpSettings;
        if (Role == LisConnectionRole.Export)
        {
            if (!tcpSettings.UseSeparateExportPort)
            {
                throw new InvalidOperationException("Export connection cannot be created when use separate export port setting is disabled.");
            }

            if (tcpSettings.SocketTypeForSeparateExportPort == SocketType.Server)
            {
                _transportConnection = _transportConnectionFactory.CreateTcpServerConnection(tcpSettings.TcpPortForSeparateExportPort);
            }
            else
            {
                _transportConnection = _transportConnectionFactory.CreateTcpClientConnection(
                    IPAddress.Parse(tcpSettings.RemoteNameOrIpAddressForSeparateExportPort),
                    tcpSettings.TcpPortForSeparateExportPort,
                    tcpSettings.TcpReconnectionIntervalForSeparateExportPort);
            }
        }
        else
        {
            if (tcpSettings.SocketType == SocketType.Server)
            {
                _transportConnection = _transportConnectionFactory.CreateTcpServerConnection(tcpSettings.TcpPort);
            }
            else
            {
                _transportConnection = _transportConnectionFactory.CreateTcpClientConnection(
                    IPAddress.Parse(tcpSettings.RemoteNameOrIpAddress),
                    tcpSettings.TcpPort,
                    tcpSettings.TcpReconnectionInterval);
            }
        }

        _transportConnection.StateChanged += OnTransportConnectionStateChanged;
    }

    private void OnDataLinkMessageReceived(object? sender, IHL7Message message)
    {
        if (message is not HL7Message hl7Message || hl7Message.Header == null)
        {
            _logger.LogWarning("Received HL7 message without a message header.");
            return;
        }

        var header = hl7Message.Header;
        if (HL7MessageType.IsAckMessage(header.MessageType1))
        {
            ProcessAcknowledgementMessage(hl7Message);
            return;
        }

        if (HL7MessageType.IsOrderMessage(header.MessageType1, header.MessageType2))
        {
            ProcessOrderMessage(hl7Message);
        }
        else
        {
            _logger.LogWarning("Received unknown HL7 message type {MessageCode}^{TriggerEvent}.", header.MessageType1, header.MessageType2);
        }
    }

    private void ProcessAcknowledgementMessage(HL7Message message)
    {
        var acknowledgement = message.Segments.OfType<HL7AcknowledgementSegment>().FirstOrDefault();
        if (acknowledgement == null)
        {
            _logger.LogWarning("Received HL7 ACK message without an MSA segment.");
            return;
        }

        _logger.LogInformation(
            "Received HL7 acknowledgement {AcknowledgementCode} for message control ID {MessageControlId}.",
            acknowledgement.AcknowledgementCode,
            acknowledgement.MessageControlId);
    }

    private void ProcessOrderMessage(HL7Message message)
    {
        var eventArgs = new TestOrderReceivedEventArgs(message);

        var orderMessage = HL7OrderMessage.Parse(message);
        using var scope = _serviceScopeFactory.CreateScope();
        var methodsService = scope.ServiceProvider.GetRequiredService<IMethodsService>();

        foreach (var patient in orderMessage.Patients)
        {
            var lisPatient = patient.Patient.MapToPatient(_lisImportSettings.CalculatePatientAge);

            foreach (var order in patient.Orders)
            {
                var sampleId = GetSampleId(order);
                if (string.IsNullOrEmpty(sampleId))
                {
                    continue;
                }

                var lisSample = lisPatient.Samples.FirstOrDefault(s => s.SampleId == sampleId);
                if (lisSample == null)
                {
                    lisSample = new LisSample { SampleId = sampleId };
                    lisPatient.Samples.Add(lisSample);
                }

                lisSample.RequestId = GetRequestId(order);
                order.CommonOrder.MapToSample(lisSample, _settings.ImportSettings.SkipImportOfRequestorInformation);

                if (order.Specimen != null)
                {
                    lisSample.TubeType = order.Specimen.Specimen.ContainerType;
                }

                foreach (var observation in order.Observations)
                {
                    var lisTestOrder = observation.ObservationRequest.MapToTestOrder(_lisImportSettings, methodsService);
                    lisSample.TestOrders.Add(lisTestOrder);

                    foreach (var testInformation in observation.AdditionalTestInformations)
                    {
                        testInformation.MapToSymptoms(lisPatient);
                    }
                }
            }

            if (lisPatient.Samples.Count > 0)
            {
                eventArgs.Patients.Add(lisPatient);
            }
        }

        if (eventArgs.Patients.Count == 0)
        {
            _logger.LogWarning("Rejected HL7 order message because no valid patient/sample data was received.");
            return;
        }

        if(message.Header != null)
        {
            eventArgs.Metadata.Add(LisMessageMetadataKeys.MessageControlId, message.Header?.MessageControlId ?? string.Empty);
            eventArgs.Metadata.Add(LisMessageMetadataKeys.TriggerEvent, message.Header?.MessageType2 ?? string.Empty);
            eventArgs.Metadata.Add(LisMessageMetadataKeys.ProcessingId, message.Header?.ProcessingId.ToString() ?? string.Empty);
        }
        TestOrderReceived?.Invoke(this, eventArgs);
    }


    private string GetSampleId(HL7Order order)
    {
        var sampleId = order.Specimen?.SpecimenContainers
            .Select(c => c.SampleId)
            .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? string.Empty;

        if (string.IsNullOrEmpty(sampleId) && _settings.ImportSettings.SampleIdInOBX)
        {
            var sampleIdFromObx = order.Observations
                .SelectMany(o => o.AdditionalTestInformations)
                .Select(i => i.SampleId)
                .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));

            if (!string.IsNullOrWhiteSpace(sampleIdFromObx))
            {
                return sampleIdFromObx;
            }
        }

        if (string.IsNullOrEmpty(sampleId) && _settings.ImportSettings.UseRequestIdAsSampleId)
        {
            var sampleIdFromObr = order.Observations
               .Select(o => o.ObservationRequest.PlacerOrderNumber)
               .FirstOrDefault(o => !string.IsNullOrWhiteSpace(o));

            if (!string.IsNullOrWhiteSpace(sampleIdFromObr))
            {
                return sampleIdFromObr;
            }
        }

        return sampleId;
    }

    private static string GetRequestId(HL7Order order)
    {
        return order.Observations
            .Select(o => o.ObservationRequest.PlacerOrderNumber)
            .FirstOrDefault(o => !string.IsNullOrWhiteSpace(o)) ?? string.Empty;
    }

    private void OnTransportConnectionStateChanged(object? sender, LisConnectionStateChangedEventArgs e)
    {
        State = e.NewState;
        StateChanged?.Invoke(this, e);
    }

    private Task AddDelayBetweenInfoRequestMessages()
    {
        if (_settings.CommunicationSettings.DelayBetweenRequestInformationRecords > 0)
        {
            var now = _dateTime.Now;
            var timeSinceLastMessage = now - _lastInfoRequestMessageTimestamp;
            var minDelay = TimeSpan.FromSeconds(_settings.CommunicationSettings.DelayBetweenRequestInformationRecords);
            if (timeSinceLastMessage < minDelay)
            {
                var delayNeeded = minDelay - timeSinceLastMessage;
                _lastInfoRequestMessageTimestamp = now + delayNeeded;
                return Task.Delay(delayNeeded);
            }
            _lastInfoRequestMessageTimestamp = now;
        }
        return Task.CompletedTask;
    }
}
