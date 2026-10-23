using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Application.Services;

internal class CommunicationSessionManager : BackgroundService, ICommunicationSessionManager
{
    private readonly IInstrumentConnectionFactory _connectionFactory;
    private readonly ILogger<CommunicationSessionManager> _logger;
    private readonly ConcurrentDictionary<Guid, CommunicationSession> _sessions = new();

    public CommunicationSessionManager(IInstrumentConnectionFactory connectionFactory, ILogger<CommunicationSessionManager> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public CommunicationSession? GetSession(IPAddress ipAddress, int port = 0)
    {
        foreach (var session in _sessions.Values)
        {
            var connection = session.Connection;
            if (connection.IpAddress.Equals(ipAddress) && (port == 0 || connection.Port == port))
            {
                return session;
            }
        }
        return null;
    }

    public CommunicationSession? GetSession(Guid sessionId)
    {
        return _sessions.GetValueOrDefault(sessionId);
    }

    public CommunicationSession? GetSession(string instrumentNumber)
    {
        foreach (var session in _sessions.Values)
        {
            if (session.InstrumentNumber == instrumentNumber)
            {
                return session;
            }
        }
        return null;
    }

    public async Task<CommunicationSession> StartSessionAsync(IPAddress ipAddress, int port, Guid sessionId, string instrumentNumber, EnumCompatibilityStatus compatibilityStatus = EnumCompatibilityStatus.FullyCompatible)
    {
        CommunicationSession? session;
        IInstrumentConnection connection;
        lock (_sessions)
        {
            foreach (var item in _sessions.Values)
            {
                if (item.InstrumentNumber == instrumentNumber)
                {
                    throw new InvalidOperationException($"There is already an open session connection for this instrument. SessionId: {sessionId}, InstrumentNumber: {instrumentNumber}");
                }
            }

            _logger.LogInformation("Opening an instrument connection, Instrument = {Instrument}, IpAddress={IpAddress}, Port={Port}.", instrumentNumber, ipAddress, port);

            connection = _connectionFactory.Create(ipAddress, port, sessionId, instrumentNumber);

            session = new CommunicationSession
            {
                SessionId = sessionId,
                InstrumentNumber = instrumentNumber,
                Connection = connection,
                CompatibilityStatus = compatibilityStatus,
            };
            _sessions[sessionId] = session;
        }

        try
        {
            await connection.ConnectAsync().ConfigureAwait(false);
            connection.RemoteDisconnect += OnConnectionRemoteDisconnect;
            return session;
        }
        catch
        {
            // failed to open connection, need to remove session
            _sessions.TryRemove(sessionId, out session);
            throw;
        }
    }

    private void OnConnectionRemoteDisconnect(object? sender, Guid sessionId)
    {
        try
        {
            var task = StopSessionAsync(sessionId);
            task.Wait();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error happened when cleaning up the instrument communication session");
        }
    }

    public async Task StopSessionAsync(Guid sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            session.Connection.RemoteDisconnect -= OnConnectionRemoteDisconnect;
            await session.Connection.DisconnectAsync().ConfigureAwait(false);

            SessionClosed?.Invoke(this, (session.SessionId, session.InstrumentNumber));
        }
    }

    public event EventHandler<(Guid SessionId, string InstrumentNumber)>? SessionClosed;

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Closing all instrument connections.");
        foreach (var sessionId in _sessions.Keys)
        {
            try
            {
                await StopSessionAsync(sessionId).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error occurred while closing an instrument connection");
            }
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
