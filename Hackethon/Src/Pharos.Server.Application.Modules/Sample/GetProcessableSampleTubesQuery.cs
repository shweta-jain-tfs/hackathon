using MediatR;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Application.Sample;

/// <summary>
/// Gets for a sample the list of tubes that are loaded and can be processed.
/// </summary>
/// <param name="SampleId">The ID of the sample (not barcode).</param>
public record GetProcessableSampleTubesQuery(string SampleId) : IRequest<List<SampleTubeDto>>;

/// <summary>
/// Represents the sample tube information.
/// </summary>
public class SampleTubeDto
{
    public SampleTubeDto(string tubeId, string instrumentNumber, double sampleVolume, SampleTubeIdentitySource tubeIdentitySource)
    {
        TubeId = tubeId;
        InstrumentNumber = instrumentNumber;
        SampleVolume = sampleVolume;
        TubeIdentitySource = tubeIdentitySource;
    }

    /// <summary>
    /// The unique tube ID assigned by Pharos.
    /// </summary>
    public string TubeId { get; }

    /// <summary>
    /// The instrument number where the tube is loaded, null if the tube is manually identified
    /// in a tray but not loaded yet.
    /// </summary>
    public string InstrumentNumber { get; }

    /// <summary>
    /// The current sample volume in the tube.
    /// </summary>
    public double SampleVolume { get; }

    /// <summary>
    /// The source of the tube identity.
    /// </summary>
    public SampleTubeIdentitySource TubeIdentitySource { get; }
}