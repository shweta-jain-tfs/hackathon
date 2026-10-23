using MediatR;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Application.Reagents;

public record SendScannedCarrierCommand(string InstrumentNumber, ScannedCarrierRequest Message) : IRequest<ScannedCarrierResponse>;