using MediatR;

namespace Pharos.Server.Application.Instrument;

/// <summary>
/// This event is published when an instrument is enabled/disabled.
/// </summary>
/// 
public record InstrumentEnabledChangedEvent(string InstrumentNumber, bool Enabled) : INotification;