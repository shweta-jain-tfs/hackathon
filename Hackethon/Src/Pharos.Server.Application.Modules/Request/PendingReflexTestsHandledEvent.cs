using MediatR;

namespace Pharos.Server.Application.Request;

/// <summary>
/// This event is raised when manual reflex tests have been handled by the user.
/// </summary>
public record PendingReflexTestsHandledEvent(IReadOnlyList<Guid> TestOrderIds) : INotification;
