using MediatR;

namespace Pharos.Server.Application.Instrument;

public record SendMoveSampleQcCommand(string InstrumentNumber, string TubeId, Guid OperationId, bool ForcedMove, bool Abort) : IInstrumentCommand, IRequest;