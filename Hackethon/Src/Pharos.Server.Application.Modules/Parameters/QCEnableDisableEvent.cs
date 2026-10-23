using MediatR;

namespace Pharos.Server.Application.Parameters;
/// <summary>
/// This event is published when a Phadia QC has been enabled or disabled.
/// </summary>
public record QcEnableDisableEvent : INotification;