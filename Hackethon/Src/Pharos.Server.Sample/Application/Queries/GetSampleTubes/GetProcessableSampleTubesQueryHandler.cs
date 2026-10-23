using Pharos.Server.Application.Sample;
using Pharos.Server.Sample.Application.Interfaces;

namespace Pharos.Server.Sample.Application.Queries.GetSampleTubes;

internal sealed class GetProcessableSampleTubesQueryHandler(
    IInstrumentSampleStorage sampleStorage) : IRequestHandler<GetProcessableSampleTubesQuery, List<SampleTubeDto>>
{
    public Task<List<SampleTubeDto>> Handle(GetProcessableSampleTubesQuery request, CancellationToken cancellationToken)
    {
        var tubes = sampleStorage.GetTubesBySampleId(request.SampleId, includeTubesWithError: false);

        var result = new List<SampleTubeDto>();

        foreach (var tube in tubes.Where(t => !t.UnloadRequested))
        {
            result.Add(new SampleTubeDto(tube.TubeId!, tube.InstrumentNumber!, tube.SampleVolume, tube.IdentitySource));
        }

        return Task.FromResult(result);
    }
}