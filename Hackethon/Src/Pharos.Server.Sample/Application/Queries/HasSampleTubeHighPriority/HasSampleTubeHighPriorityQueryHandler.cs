using Pharos.Server.Application.Sample;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Sample.Application.Interfaces;

namespace Pharos.Server.Sample.Application.Queries.HasSampleTubeHighPriority;

internal sealed class HasSampleTubeHighPriorityQueryHandler(
    ISampleDbContext dbContext, IInstrumentSampleStorage sampleStorage) : IRequestHandler<HasSampleTubeHighPriorityQuery, bool>
{
    public async Task<bool> Handle(HasSampleTubeHighPriorityQuery query, CancellationToken cancellationToken)
    {
        var tubesInStorage = sampleStorage.GetTubesBySampleId(query.RequestUid.SampleId, true);

        if (tubesInStorage is { Count: > 0 })
        {
            return HasHighPriorityInSampleStorage(tubesInStorage, query.RequestUid.RequestCreatedDate);
        }

        return await HasHighPriorityInDatabase(query.RequestUid, cancellationToken);
    }

    private static bool HasHighPriorityInSampleStorage(List<Domain.SampleTube> tubes, DateTimeOffset requestCreatedDate)
    {
        var filteredTubes = requestCreatedDate != default
            ? tubes.Where(x => x.RequestCreatedDate == requestCreatedDate)
            : tubes.Where(x => x.RequestCreatedDate == null);

        return filteredTubes.Any(x => x.HighPriority);
    }

    private async Task<bool> HasHighPriorityInDatabase(RequestUid requestUid, CancellationToken cancellationToken)
    {
        if (requestUid.RequestCreatedDate != default)
        {
            return await dbContext.SampleTubes
                .AnyAsync(x => x.SampleId == requestUid.SampleId
                            && x.RequestCreatedDate == requestUid.RequestCreatedDate
                            && x.HighPriority, cancellationToken);
        }

        return await dbContext.SampleTubes
            .AnyAsync(x => x.SampleId == requestUid.SampleId
                        && x.RequestCreatedDate == null
                        && x.HighPriority, cancellationToken);
    }
}
