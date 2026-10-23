using System.Collections.Concurrent;

namespace Pharos.Server.Instrument.Infrastructure.Kestrel;

internal class ClientCertificateCache
{
    private readonly ConcurrentDictionary<string, string> _cache = new();

    public void Add(string thumbprint, string instrumentNumber)
    {
        _cache[thumbprint] = instrumentNumber;
    }

    public bool TryGetInstrumentNumber(string thumbprint, out string? instrumentNumber)
    {
        return _cache.TryGetValue(thumbprint, out instrumentNumber);
    }
}
