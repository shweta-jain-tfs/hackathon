using MediatR;

namespace Pharos.Server.Application.Parameters;

public record MethodTestActivatedEvent(string MethodName, string TestName, bool Active) : INotification;
