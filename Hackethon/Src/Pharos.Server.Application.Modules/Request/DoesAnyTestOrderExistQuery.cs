using MediatR;

namespace Pharos.Server.Application.Request;

public record DoesAnyTestOrderExistQuery(List<Guid> TestOrderIds) : IRequest<bool>;