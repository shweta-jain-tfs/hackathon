using MediatR;

namespace Pharos.Server.Application.Instrument;

public record MeasureSampleVolumeCommand(string InstrumentNumber, string TubeId, int TubeTypeIndex) : IInstrumentCommand, IRequest;
