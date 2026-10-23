using MediatR;
using Tetris.Protocol.Instrument;
using Tetris.Protocol.Instrument.TestOrder;

namespace Pharos.Server.Application.Instrument;

public record GetNeededSampleVolumeQuery(string InstrumentNumber, IReadOnlyList<TestOrder> TestOrders) : IInstrumentCommand, IRequest<CalculateNeededSampleVolumeResponse?>;
