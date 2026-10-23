using MediatR;

namespace Pharos.Server.Application.Request;

public record RetestRejectedResultsCommand(List<Guid> TestOrderIds, int? InstrumentDilution = null) : IRequest;
