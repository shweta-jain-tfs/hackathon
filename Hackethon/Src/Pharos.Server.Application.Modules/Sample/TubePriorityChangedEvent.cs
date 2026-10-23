using MediatR;

namespace Pharos.Server.Application.Sample;

/// <summary>
/// This event is published every time when the priority of a tube has been changed after tube identification.
/// </summary>
public record TubePriorityChangedEvent(string? InstrumentNumber, SampleTubeId SampleTubeId, bool HighPriority) : INotification;