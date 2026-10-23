using MediatR;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Application.Reagents;

public record GetReagentDrawerContentsCommand(string InstrumentNumber, ReagentDrawerContentsRequest Message) : IRequest<ReagentDrawerContentsResponse>;