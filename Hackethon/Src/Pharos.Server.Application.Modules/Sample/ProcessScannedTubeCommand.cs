using MediatR;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Application.Sample;

public sealed record ProcessScannedTubeCommand(string InstrumentNumber, ScannedSampleQcRequest Message) : IRequest<ScannedSampleQcResponse>;