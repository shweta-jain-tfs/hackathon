using Pharos.Server.Application;

namespace Pharos.Server.Integration.Tests.Common;

public class FakeCurrentInstrumentService : ICurrentInstrumentService
{
    public int? InstrumentId { get; } = 1;

    public string? InstrumentNumber { get; } = "SERIAL";
}