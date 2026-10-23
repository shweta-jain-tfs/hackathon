using MediatR;
using Tetris.Protocol.Pharos.Move;

namespace Pharos.Server.Application.Reagents;

public record ProcessUnloadReagentResultCommand(string InstrumentNumber, UnloadReagentResult UnloadResult) : IRequest;
