using MediatR;
using Tetris.Protocol.Pharos.ProcessStatus;

namespace Pharos.Server.Application.Instrument;
public record ProcessStatusUpdatedEvent(string InstrumentNumber, ProcessStatus ProcessStatus) : INotification;