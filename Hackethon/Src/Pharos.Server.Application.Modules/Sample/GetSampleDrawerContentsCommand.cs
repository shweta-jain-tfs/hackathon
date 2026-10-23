using MediatR;
using Tetris.Protocol.Pharos.Tray;

namespace Pharos.Server.Application.Sample;

public sealed record GetSampleDrawerContentsCommand(string InstrumentNumber, SampleDrawerContentsRequest Message) : IRequest<SampleDrawerContentsResponse>;