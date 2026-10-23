using Pharos.Server.Application.Sample;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Interfaces;

namespace Pharos.Server.Sample.Application.Queries.GetSampleTubePositionsForLis;

internal sealed class GetSampleTubePositionsForLisQueryHandler(
    IInstrumentSampleStorage sampleStorage,
    ISampleDbContext dbContext) : IRequestHandler<GetSampleTubePositionsForLisQuery, List<TubeLocationDto>>
{
    public async Task<List<TubeLocationDto>> Handle(GetSampleTubePositionsForLisQuery request, CancellationToken cancellationToken)
    {
        var result = new List<TubeLocationDto>();

        foreach (var tubeId in request.TubeIds)
        {
            string? trayId = null;
            int? tubePosition = null;

            var tubeInInstrument = sampleStorage.GetTube(tubeId);
            if (tubeInInstrument != null && tubeInInstrument.Area is not (SampleArea.Shuttle1 or SampleArea.Shuttle2 or SampleArea.InternalStorage))
            {
                trayId = tubeInInstrument.TrayId;
                tubePosition = tubeInInstrument.TubePosition;
            }
            else if (tubeInInstrument == null)
            {
                var tubeFromDb = await dbContext.SampleTubes
                    .Where(t => t.TubeId == tubeId)
                    .OrderByDescending(t => t.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (tubeFromDb != null)
                {
                    trayId = tubeFromDb.TrayId;
                    tubePosition = tubeFromDb.TubePosition;
                }
            }

            result.Add(new TubeLocationDto(tubeId, trayId, tubePosition));
        }

        return result;
    }
}