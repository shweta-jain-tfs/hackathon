using MediatR;
using Tetris.Protocol.Pharos.Move;

namespace Pharos.Server.Application.Sample
{
    public sealed record MoveSampleQcResultCommand(string InstrumentNumber, MoveSampleQcResult Result) : IRequest;
    
}
