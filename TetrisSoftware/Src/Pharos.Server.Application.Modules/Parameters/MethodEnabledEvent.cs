using MediatR;

namespace Pharos.Server.Application.Parameters;

public record MethodEnabledEvent(string MethodName, bool Enabled) : INotification;
