using MediatR;

namespace Pharos.Server.Application.Parameters;

/// <summary>
/// This event is published every time after any method parameters are updated (e.g. after method update package import).
/// </summary>
public record MethodsUpdatedEvent : INotification;
