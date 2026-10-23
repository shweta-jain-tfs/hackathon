using MediatR;
using Tetris.Protocol.Instrument.UpdateBottledReagent;

namespace Pharos.Server.Application.Instrument;

public record UpdateBottledReagentCommand(string InstrumentNumber, UpdateBottledReagentInformation UpdateBottledReagent) : IInstrumentCommand, IRequest;