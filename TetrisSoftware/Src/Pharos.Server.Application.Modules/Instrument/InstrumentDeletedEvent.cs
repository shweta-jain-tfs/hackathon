using MediatR;

namespace Pharos.Server.Application.Instrument;

/// <summary>
/// This event is published when an instrument is deleted.
/// </summary>
public record InstrumentDeletedEvent(string InstrumentNumber, int EntityId) : INotification;