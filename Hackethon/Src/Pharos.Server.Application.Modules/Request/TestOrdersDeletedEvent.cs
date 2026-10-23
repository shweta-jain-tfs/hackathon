using MediatR;

namespace Pharos.Server.Application.Request;

public record TestOrdersDeletedEvent(List<Guid> TestOrderIds) : INotification;