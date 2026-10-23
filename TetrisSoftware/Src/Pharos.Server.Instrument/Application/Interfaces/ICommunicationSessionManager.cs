using System.Net;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Application.Interfaces;

internal interface ICommunicationSessionManager
{
    CommunicationSession? GetSession(Guid sessionId);
    CommunicationSession? GetSession(string instrumentNumber);
    CommunicationSession? GetSession(IPAddress ipAddress, int port = 0);
    Task<CommunicationSession> StartSessionAsync(IPAddress ipAddress, int port, Guid sessionId, string instrumentNumber, EnumCompatibilityStatus compatibilityStatus = EnumCompatibilityStatus.FullyCompatible);
    Task StopSessionAsync(Guid sessionId);
    event EventHandler<(Guid SessionId, string InstrumentNumber)>? SessionClosed;
}