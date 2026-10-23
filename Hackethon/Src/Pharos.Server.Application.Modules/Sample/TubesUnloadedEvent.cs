using MediatR;

namespace Pharos.Server.Application.Sample;

/// <summary>
/// This event is published when one or more previously identified tubes have been unloaded from an instrument.
/// "Identified" means that the tube is detected by the instrument and the sample ID is known.
/// </summary>
public record TubesUnloadedEvent(string InstrumentNumber, IReadOnlyList<UnloadedTube> Tubes) : INotification;

public record UnloadedTube(string TubeId, string SampleId, bool HasErrors);
