using MediatR;

namespace Pharos.Server.Application.Instrument;

/// <summary>
/// This event is published when an instrument is created.
/// </summary>
public record InstrumentCreatedEvent(string InstrumentNumber) : INotification;