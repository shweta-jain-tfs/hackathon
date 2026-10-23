using MediatR;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Application.Sample;

/// <summary>
/// Gets the tube processing history for a specified test request.
/// The response includes both the currently loaded tubes and the tubes that were processed in the past.
/// </summary>
/// <param name="SampleId">The sample identifier.</param>
/// <param name="RequestCreatedDate">The timestamp of the moment when the request was created in the database. This is needed to uniquely identify the request together with the sample ID.</param>
public record GetSampleTubeHistoryQuery(string SampleId, DateTimeOffset RequestCreatedDate) : IRequest<List<SampleTubeHistoryDto>>;

public class SampleTubeHistoryDto
{
    /// <summary>
    /// The human-readable tube ID extracted from the tube barcode (e.g. "001"). See remarks for more details.
    /// </summary>
    /// <remarks>
    /// This property is only used for display purposes. Do not confuse it with the internal TubeId property of the sample tube.
    /// This property is only set when the corresponding settings (for special sample handling) are enabled in Pharos.
    /// </remarks>
    public string? DisplayTubeId { get; set; }
    public DateTimeOffset? LoadTimestamp { get; set; }
    public string? InstrumentNumber { get; set; }
    public string? InstrumentName { get; set; }
    public SampleArea? Area { get; set; }
    public SampleTubeStatus TubeStatus { get; set; }
    public SampleTubeError Error { get; set; }
    public bool UnloadRequested { get; set; }
    public bool HighPriority { get; set; }
    public bool ManuallyEntered { get; set; }
    public string? TrayId { get; set; }

    /// <summary>
    /// A human-readable tube position in the tray (e.g. "A1").
    /// </summary>
    public string? TubePositionDisplayValue { get; set; }
    public int? TubePosition { get; set; }
    public int? SampleVolume { get; set; }
    public string? ImageIdentity { get; set; }
    public bool IsInCurrentInstrument { get; set; }
    public bool DoesTrayExist { get; set; }
}