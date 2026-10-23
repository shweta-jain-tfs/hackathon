using MediatR;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Application.Instrument;

/// <summary>
/// This event is published every time when Pharos receives the updated instrument storage information.
/// </summary>
public record InstrumentStorageUpdatedEvent(string InstrumentNumber, InstrumentStorage UpdatedStorage) : INotification;
