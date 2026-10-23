using MediatR;

namespace Pharos.Server.Application.Sample;

/// <summary>
/// Gets the tube position information (TrayId and TubePosition) for export to LIS.
/// Returns the last known location from instrument storage (if currently loaded) or database (if unloaded).
/// Returns empty values only if the tube never had a tray location or it was cleared.
/// </summary>
/// <param name="TubeIds">The Tube IDs to query tube positions for.</param>
public record GetSampleTubePositionsForLisQuery(List<string> TubeIds) : IRequest<List<TubeLocationDto>>;

/// <summary>
/// Represents the tube location information for export to LIS.
/// </summary>
public class TubeLocationDto
{
    public TubeLocationDto(string tubeId, string? trayId, int? tubePosition)
    {
        TubeId = tubeId;
        TrayId = trayId;
        TubePosition = tubePosition;
    }

    /// <summary>
    /// The unique tube ID assigned by Pharos.
    /// </summary>
    public string TubeId { get; }

    /// <summary>
    /// The Tray ID where the tube is/was located.
    /// </summary>
    public string? TrayId { get; }

    /// <summary>
    /// The position where the tube is/was located in the tray.
    /// </summary>
    public int? TubePosition { get; }
}