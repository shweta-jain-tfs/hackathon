using MediatR;

namespace Pharos.Server.Application.Instrument;

public record GetSampleTubeImageQuery(string InstrumentNumber, string ImageId) : IInstrumentCommand, IRequest<SampleTubeImageDto>;

public class SampleTubeImageDto
{
    public byte[] ImageData { get; set; } = [];
    public string TubeId { get; set; } = string.Empty;
    public DateTimeOffset Time { get; set; } = DateTimeOffset.MinValue;
    public bool ImageFound { get; set; }
}