using MediatR;
using Pharos.Server.Domain.Entities;

namespace Pharos.Server.Application.Request;

public record RequestCreatedEvent(int RequestId, RequestUid RequestUid, IReadOnlyList<ITestOrder> TestOrders) : INotification;
