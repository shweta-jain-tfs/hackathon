using MediatR;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Application.LoadList;

public record ProcessLoadReagentResultCommand(string InstrumentSerial, LoadReagentResult Request) : IRequest;