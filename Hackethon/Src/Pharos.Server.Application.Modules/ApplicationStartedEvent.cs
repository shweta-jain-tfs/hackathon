using MediatR;

namespace Pharos.Server.Application;

/// <summary>
/// This event is raised when the application has fully started (including all hosted services).
/// Use this event to for post-startup actions that need the fully-functional app.
/// </summary>
public class ApplicationStartedEvent : INotification;