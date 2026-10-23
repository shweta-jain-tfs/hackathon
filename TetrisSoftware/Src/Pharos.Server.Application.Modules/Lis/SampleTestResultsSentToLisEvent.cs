using MediatR;

namespace Pharos.Server.Application.Lis;

/// <summary>
/// This event is published after Pharos successfully sends sample test results to LIS.
/// </summary>
/// <remarks>
/// This event is published AFTER the message is successfully sent to LIS (not just queued).
/// Used to mark test orders as Reported.
/// </remarks>
public sealed record SampleTestResultsSentToLisEvent(
    IReadOnlyList<Guid> TestOrderIds,
    int LisChannelId) : INotification;
