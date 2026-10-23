using MediatR;

namespace Pharos.Server.Application.Sample;

public record SetSampleInSampleTrayPositionCommand : IRequest
{
    public string TrayId { get; set; } = default!;
    public int TubePosition { get; set; }
    public string SampleId { get; set; } = default!;
}