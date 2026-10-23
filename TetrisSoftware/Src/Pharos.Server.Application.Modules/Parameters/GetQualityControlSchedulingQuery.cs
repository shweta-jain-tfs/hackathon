using MediatR;
using Tetris.Protocol.Instrument;

namespace Pharos.Server.Application.Parameters;

public record GetQualityControlSchedulingQuery : IRequest<QcSchedulingRequest>;