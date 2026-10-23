using MediatR;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Application.Sample;

public record GetUnloadableQualityControlTubesQuery(string QualityControlName) : IRequest<List<UnloadableQualityControlTubeDto>>;

public class UnloadableQualityControlTubeDto
{
    public string Id { get; set; } = string.Empty;
    public string Lot { get; set; } = string.Empty;
    public DateTimeOffset DetectedDateTime { get; set; }
    public string? InstrumentNumber { get; set; }
    public string? InstrumentName { get; set; }
    public SampleArea? Area { get; set; }
    public SampleTubeStatus TubeStatus { get; set; }
    public SampleTubeError Error { get; set; }
    public bool UnloadRequested { get; set; }
    public bool ManuallyEntered { get; set; }
    public string? TrayId { get; set; }

    /// <summary>
    /// A human-readable tube position in the tray (e.g. "A1").
    /// </summary>
    public string? TubePositionDisplayValue { get; set; }
    public int? TubePosition { get; set; }
    public int? RemainingVolume { get; set; }
    public bool IsInCurrentInstrument { get; set; }
    public bool DoesTrayExist { get; set; }
}
