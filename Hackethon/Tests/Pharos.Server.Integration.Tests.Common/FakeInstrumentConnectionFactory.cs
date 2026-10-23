using System.Net;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Integration.Tests.Common;

public class FakeInstrumentConnectionFactory : IInstrumentConnectionFactory
{
    private readonly List<IInstrumentConnection> _instrumentConnections = new();

    public void AddInstrumentConnection(IInstrumentConnection instrumentConnection)
    {
        _instrumentConnections.Add(instrumentConnection);
    }

    public IInstrumentConnection Create(IPAddress ipAddress, int port, Guid sessionId, string instrumentNumber)
    {
        return _instrumentConnections.SingleOrDefault()!;
    }

    public async Task Clear()
    {
        foreach (var connection in _instrumentConnections)
        {
            await connection.DisconnectAsync();
        }

        _instrumentConnections.Clear();
    }
}