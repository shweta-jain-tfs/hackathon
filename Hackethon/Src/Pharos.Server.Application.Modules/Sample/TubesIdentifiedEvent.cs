using MediatR;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Application.Sample;

/// <summary>
/// This event is published every time when one or several tubes has been identified inside an instrument.
/// "Identified" means that the tube is detected by the instrument and the sample ID is known.
/// </summary>
public record TubesIdentifiedEvent(string InstrumentNumber, IReadOnlyList<SampleTubeInformation> Tubes) : INotification;

public record SampleTubeInformation(SampleTubeId Tube, SampleTubeIdentitySource IdentitySource);

public record SampleTubeId(string TubeId, string SampleId);