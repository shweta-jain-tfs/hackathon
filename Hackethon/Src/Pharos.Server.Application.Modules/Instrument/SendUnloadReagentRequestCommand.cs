using MediatR;
using Tetris.Protocol.Instrument.Move;

namespace Pharos.Server.Application.Instrument;

public record SendUnloadReagentRequestCommand(string InstrumentNumber, UnloadReagentRequest UnloadReagents) : IRequest, IInstrumentCommand;