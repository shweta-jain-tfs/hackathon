using System.Net;

namespace Pharos.Server.Instrument.Application.Interfaces;

internal interface IInstrumentConnectionFactory
{
    IInstrumentConnection Create(IPAddress ipAddress, int port, Guid sessionId, string instrumentNumber);
}