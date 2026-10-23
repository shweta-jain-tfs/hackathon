using System.Net;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Infrastructure.Grpc;

namespace Pharos.Server.Instrument.Application.Services;

internal class InstrumentConnectionFactory(IServiceProvider serviceProvider) : IInstrumentConnectionFactory
{
    public IInstrumentConnection Create(IPAddress ipAddress, int port, Guid sessionId, string instrumentNumber)
    {
        return new InstrumentConnection(ipAddress, port, sessionId, instrumentNumber, serviceProvider);
    }
}