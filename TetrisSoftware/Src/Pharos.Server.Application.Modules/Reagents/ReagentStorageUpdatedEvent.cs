using MediatR;

namespace Pharos.Server.Application.Reagents;

public record ReagentStorageUpdatedEvent(string InstrumentNumber, bool IsFullUpdate, Dictionary<string, int> Carriers, Dictionary<string, int> Cartridges, Dictionary<string, int> Bottles) : INotification;