using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Application.Queries;

public class SampleTrayDetailsDto
{
    public string Id { get; set; } = default!;
    public int? TrayPosition { get; set; }
    public string? TrayLocation { get; set; }
    public bool IsPositionEmpty { get; set; }
    public SampleTrayError TrayError { get; set; }
    public IList<SampleTrayPositionDto> SamplePositions { get; set; } = default!;
    public bool IsTrayEditable { get; set; }
}