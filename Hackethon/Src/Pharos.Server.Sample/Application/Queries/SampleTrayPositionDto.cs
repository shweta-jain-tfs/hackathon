using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Sample.Application.Queries;

public class SampleTrayPositionDto
{
    public SampleTubeStatus SampleTubeStatus { get; set; }
    public string? SampleId { get; set; }
    public string? SampleIdDisplay { get; set; }
    public string? TubeBarcode { get; set; }
    public int? Position { get; set; }
    public bool IsHighPrioritySet { get; set; }
    public bool IsUnloadRequested { get; set; }
    public SampleTubeIdentitySource SamplePositionSource { get; set; }
    public SampleTubeError Error { get; set; }
    public SampleType SampleType { get; set; }
}