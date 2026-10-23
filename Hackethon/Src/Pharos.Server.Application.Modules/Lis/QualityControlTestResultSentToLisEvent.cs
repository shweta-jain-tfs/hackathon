using MediatR;

namespace Pharos.Server.Application.Lis;

/// <summary>
/// This event is published after Pharos successfully sends quality control test result to LIS.
/// </summary>
/// <param name="TestResultId">The unique ID of the quality control test result in Pharos.</param>
public sealed record QualityControlTestResultSentToLisEvent(int TestResultId) : INotification;
