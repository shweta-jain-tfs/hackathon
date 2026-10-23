using MediatR;
using Tetris.Protocol.Pharos.Errors;

namespace Pharos.Server.Application.Instrument;

/// <summary>
/// This event is published every time Pharos receives the current error list from an instrument.
/// </summary>
public record InstrumentErrorListUpdatedEvent(string InstrumentNumber, CurrentErrorList Message) : INotification;