using MediatR;
using Tetris.Protocol.Common;

namespace Pharos.Server.Application.Instrument;

public record SendErrorResponseCommand(Guid ErrorId, EnumErrorResponseAction ResponseAction, string InstrumentNumber) : IInstrumentCommand, IRequest;