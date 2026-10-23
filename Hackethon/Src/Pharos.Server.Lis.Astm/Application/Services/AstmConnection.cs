using System.Net;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Application.SystemErrors;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Lis.Astm.Application.Mappings;
using Pharos.Server.Lis.Astm.Application.Messages;
using Pharos.Server.Lis.Domain;
using Pharos.Server.Localization;

namespace Pharos.Server.Lis.Astm.Application.Services;

internal sealed class AstmConnection : ILisConnection
{
    private readonly ILisTransportConnectionFactory _transportConnectionFactory;
    private readonly IAstmDataLinkFactory _dataLinkFactory;
    private readonly IDateTime _dateTime;
    private readonly ILogger<AstmConnection> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly AstmSettings _settings;
    private readonly LisImportSettings _lisImportSettings;
    private IAstmDataLink? _dataLink;
    private ILisTransportConnection? _transportConnection;
    private DateTimeOffset _lastInfoRequestMessageTimestamp;

    public AstmConnection(
        int lisChannelId,
        LisChannel lisChannel,
        LisConnectionRole role,
        ILisTransportConnectionFactory transportConnectionFactory,
        IAstmDataLinkFactory dataLinkFactory,
        IDateTime dateTime,
        ILogger<AstmConnection> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        LisChannelId = lisChannelId;
        _settings = lisChannel.AstmSettings!;
        _lisImportSettings = lisChannel.ImportSettings;
        Role = role;
        _transportConnectionFactory = transportConnectionFactory;
        _dataLinkFactory = dataLinkFactory;
        _dateTime = dateTime;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
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
            _dataLink = _dataLinkFactory.CreateAstmDataLink(_settings);
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
        if (!Role.HasFlag(LisConnectionRole.Export))
        {
            _logger.LogError("Connection role {Role} does not support sending messages.", Role);
            return false;
        }

        if (messageType is LisMessageType.Unknown || messageType is LisMessageType.Acknowledgement)
        {
            _logger.LogError("Unsupported message type '{MessageType}'", messageType);
            return false;
        }

        if (messageType == LisMessageType.TestOrderQuery)
        {
            // Ensure required delay before sending request information records
            await AddDelayBetweenInfoRequestMessages().ConfigureAwait(false);
        }

        if (_dataLink is null)
        {
            _logger.LogError("ASTM data link is not initialized for message type '{MessageType}'", messageType);
            return false;
        }

        var result = await _dataLink.SendMessageAsync(messageData).ConfigureAwait(false);

        return result;
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

    private void CreateTransportConnection()
    {
        if (_transportConnection != null)
        {
            return;
        }

        if (_settings.CommunicationSettings.CommunicationMode == AstmCommunicationMode.Tcp)
        {
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
                        IPAddress.Parse(tcpSettings.IpAddressForSeparateExportPort),
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
                        IPAddress.Parse(tcpSettings.IpAddress),
                        tcpSettings.TcpPort,
                        tcpSettings.TcpReconnectionInterval);
                }
            }
        }
        else if (_settings.CommunicationSettings.CommunicationMode == AstmCommunicationMode.Rs232)
        {
            _transportConnection = _transportConnectionFactory.CreateRs232Connection(_settings.CommunicationSettings.Rs232Settings);
        }
        else
        {
            throw new ArgumentException($"Invalid value for the ASTM communication protocol - {_settings.CommunicationSettings.CommunicationMode}");
        }

        _transportConnection.StateChanged += OnTransportConnectionStateChanged;
    }

    private void OnDataLinkMessageReceived(object? sender, IAstmMessage e)
    {
        var recordsToImport = HandleRepeatedRecords(e.Records);
        var eventArgs = new TestOrderReceivedEventArgs(e);
        LisPatient? curPatient = null;
        string? curRequestId = null;

        foreach (var record in recordsToImport)
        {
            if (record.ParsingErrors.Count > 0)
            {
                LogInvalidMessageAsync(ErrorClassification.Error, Translations.AstmConnection_InvalidOrderFieldsMessage);
            }

            switch (record)
            {
                case AstmHeaderRecord:
                    // Reset the patient context, so records after this header are not linked to the previous patient.
                    curPatient = null;
                    curRequestId = null;
                    break;

                case AstmPatientRecord patientRecord:

                    curPatient = patientRecord.MapToPatient(_settings, _lisImportSettings);
                    eventArgs.Patients.Add(curPatient);
                    curRequestId = patientRecord.LaboratoryPatientId;
                    break;

                case AstmTestOrderRecord testOrderRecord:

                    if (testOrderRecord.ReportType == AstmReportType.Undefined)
                    {
                        HandleIllegalReportType();
                    }

                    var curSample = curPatient?.Samples.FirstOrDefault(s => s.SampleId == testOrderRecord.SpecimenId);

                    if (curSample == null)
                    {
                        curSample = testOrderRecord.MapToSample();
                        curSample.RequestId = curRequestId;
                        curPatient?.Samples.Add(curSample);
                    }

                    testOrderRecord.MapToTestOrders(curSample.TestOrders, _settings.ImportSettings, _lisImportSettings);

                    var mapRequestorInfo = !string.IsNullOrEmpty(testOrderRecord.RequestorId) && !_settings.ImportSettings.SkipImportOfRequestorInformation;

                    if (mapRequestorInfo)
                    {
                        testOrderRecord.MapToRequestor(curSample, _settings);
                    }

                    break;

                case AstmUnsupportedRecord unsupportedRecord:
                    // TODO : Consider logging the unsupported record type and data to UI.
                    _logger.LogWarning("Unsupported ASTM record type '{RecordType}' encountered.", unsupportedRecord.RecordType);
                    break;
            }
        }

        TestOrderReceived?.Invoke(this, eventArgs);
    }

    private IList<IAstmRecord> HandleRepeatedRecords(IList<IAstmRecord> records)
    {
        var ignoreNonCriticalErrors = _settings.ImportSettings.IgnoreNonCriticalImportErrors;
        var repeatedHeaderCount = records.Count(record => record is AstmHeaderRecord);

        // No repeated header then return all records.
        if (repeatedHeaderCount == 0)
        {
            return records;
        }

        var lastHeaderIndex = records.Select((record, index) => new { Record = record, Index = index })
                                     .Last(item => item.Record is AstmHeaderRecord)
                                     .Index;

        var recordsFromLastHeader = records.Skip(lastHeaderIndex).ToList();

        // when last header doesn't have any record, discard all exiting record.
        if (recordsFromLastHeader.Count == 1)
        {
            LogRepeatedHeaders(repeatedHeaderCount, ignoreNonCriticalErrors: false);
            return [];
        }

        LogRepeatedHeaders(repeatedHeaderCount, ignoreNonCriticalErrors);

        // Setting OFF returns only the last header and its records; setting ON returns all records.
        return ignoreNonCriticalErrors ? records : recordsFromLastHeader;
    }

    private void LogRepeatedHeaders(int repeatedHeaderCount, bool ignoreNonCriticalErrors)
    {
        var (classification, message) = ignoreNonCriticalErrors
            ? (ErrorClassification.Warning, Translations.AstmConnection_RepeatedHeadersKeptMessage)
            : (ErrorClassification.Error, Translations.AstmConnection_RepeatedHeadersDiscardedMessage);

        for (var index = 0; index < repeatedHeaderCount; index++)
        {
            LogInvalidMessageAsync(classification, message);
        }
    }

    private void HandleIllegalReportType()
    {
        var classification = _settings.ImportSettings.IgnoreNonCriticalImportErrors
            ? ErrorClassification.Warning
            : ErrorClassification.Error;

        LogInvalidMessageAsync(classification, Translations.AstmConnection_IllegalReportTypeMessage);
    }


    private void LogInvalidMessageAsync(ErrorClassification classification, string message)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        if (classification == ErrorClassification.Error)
        {
            _logger.LogError("{Message}", message);
        }
        else
        {
            _logger.LogWarning("{Message}", message);
        }

        sender.Send(new CreatePharosErrorCommand(
            PharosErrorCodes.LisInvalidMessage,
            classification,
            Parameters: [message])).GetAwaiter().GetResult();
    }

    private void OnTransportConnectionStateChanged(object? sender, LisConnectionStateChangedEventArgs e)
    {
        State = e.NewState;
        StateChanged?.Invoke(this, e);
    }
}
