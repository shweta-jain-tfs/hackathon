using MediatR;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.Application.Instrument;

public record ControlMaintenanceCommand(string InstrumentNumber, ControlMaintenanceRequest Message) : IRequest<ControlMaintenanceResponse>, IInstrumentCommand;