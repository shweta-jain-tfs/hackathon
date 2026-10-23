using MediatR;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Application.Sample;

public record GetSampleQcDestinationQuery(string InstrumentNumber, SampleQcDestinationRequest Message) : IRequest<SampleQcDestinationResponse>;