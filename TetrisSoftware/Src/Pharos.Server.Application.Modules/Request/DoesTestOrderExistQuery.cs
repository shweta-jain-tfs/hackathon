using MediatR;

namespace Pharos.Server.Application.Request;

public record DoesTestOrderExistQuery(Guid TestOrderId) : IRequest<bool>;