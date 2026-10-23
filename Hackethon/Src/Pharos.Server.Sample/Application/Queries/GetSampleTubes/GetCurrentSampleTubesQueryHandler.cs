using Pharos.Server.Application.Sample;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Mapping;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Application.Queries.GetSampleTubes;

internal sealed class GetCurrentSampleTubesQueryHandler(
    IInstrumentSampleStorage sampleStorage,
    ISampleDbContext dbContext) : IRequestHandler<GetCurrentSampleTubesQuery, List<CurrentSampleTubesDto>>
{
    /// <summary>
    /// Identifies one request. SampleId can be reused, so the creation date is part of the key.
    /// </summary>
    private readonly record struct RequestKey(string SampleId, DateTimeOffset RequestCreatedDate);

    /// <summary>
    /// Returns current sample tubes by combining live instrument storage with persisted tubes not loaded in an instrument.
    /// </summary>
    public async Task<List<CurrentSampleTubesDto>> Handle(GetCurrentSampleTubesQuery request, CancellationToken cancellationToken)
    {
        // Request identity is SampleId + RequestCreatedDate. SampleId alone is not unique.
        var requestedRequestsByKey = request.RequestUids
            .DistinctBy(ToRequestKey)
            .ToDictionary(ToRequestKey);

        if (requestedRequestsByKey.Count == 0)
        {
            return [];
        }

        // SampleId is used only as a broad lookup key; exact request matching is done with IsRequestedTube.
        var requestedSampleIds = requestedRequestsByKey.Keys
            .Select(x => x.SampleId)
            .ToHashSet(StringComparer.Ordinal);

        var storageTubes = GetRequestedStorageTubes(requestedSampleIds, requestedRequestsByKey);

        var dbTubesNotLoadedInInstrument = await GetRequestedDbTubesNotLoadedInInstrument(
            requestedSampleIds,
            storageTubes,
            cancellationToken);

        // Match the full request identity after loading. SampleId can be reused, so SampleId alone
        // is only a broad database filter.
        var allTubes = storageTubes
            .Concat(dbTubesNotLoadedInInstrument)
            .Where(tube => IsRequestedTube(tube, requestedRequestsByKey))
            .GroupBy(ToRequestKey)
            .ToDictionary(g => g.Key, g => g.ToCurrentSampleTubeDtos());

        // Return one result per requested request, even when no matching tubes were found.
        return requestedRequestsByKey
            .Select(request => new CurrentSampleTubesDto(
                request.Value,
                allTubes.GetValueOrDefault(request.Key, [])))
            .ToList();
    }

    /// <summary>
    /// Gets matching tubes from in-memory instrument storage, which represents tubes currently known by instruments.
    /// </summary>
    private List<SampleTube> GetRequestedStorageTubes(
        HashSet<string> requestedSampleIds,
        IReadOnlyDictionary<RequestKey, RequestUid> requestedRequestsByKey)
    {
        // In-memory storage contains tubes currently known by the instruments.
        // It can be searched broadly by SampleId, then narrowed to exact request identity.
        return sampleStorage
            .GetTubesBySampleIds(requestedSampleIds, includeTubesWithError: true)
            .Where(tube => IsRequestedTube(tube, requestedRequestsByKey))
            .ToList();
    }

    /// <summary>
    /// Gets persisted tubes for the requested samples that are not currently loaded in an instrument.
    /// </summary>
    private async Task<List<SampleTube>> GetRequestedDbTubesNotLoadedInInstrument(
        HashSet<string> requestedSampleIds,
        IReadOnlyCollection<SampleTube> storageTubes,
        CancellationToken cancellationToken)
    {
        // Keep only persisted tubes that are not currently loaded in an instrument.
        // Match by tube id to avoid duplicates between storage and DB records.
        var storageTubeIds = storageTubes
            .Where(t => !string.IsNullOrWhiteSpace(t.TubeId))
            .Select(t => t.TubeId!)
            .ToHashSet(StringComparer.Ordinal);

        // Use SampleId as the database filter, then apply exact request matching before grouping.
        return await dbContext.SampleTubes
            .Where(t => t.SampleId != null
                        && requestedSampleIds.Contains(t.SampleId)
                        && string.IsNullOrEmpty(t.InstrumentNumber)
                        && (string.IsNullOrEmpty(t.TubeId) || !storageTubeIds.Contains(t.TubeId)))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Creates the request identity key from an application request identifier.
    /// </summary>
    private static RequestKey ToRequestKey(RequestUid requestUid)
    {
        return new RequestKey(requestUid.SampleId, requestUid.RequestCreatedDate);
    }

    /// <summary>
    /// Creates the request identity key from a sample tube.
    /// </summary>
    private static RequestKey ToRequestKey(SampleTube sampleTube)
    {
        return new RequestKey(sampleTube.SampleId!, sampleTube.RequestCreatedDate!.Value);
    }

    /// <summary>
    /// Checks the exact request identity after broad SampleId filtering.
    /// </summary>
    private static bool IsRequestedTube(
        SampleTube tube,
        IReadOnlyDictionary<RequestKey, RequestUid> requestedRequestsByKey)
    {
        return tube is { SampleId: not null, RequestCreatedDate: not null }
               && requestedRequestsByKey.ContainsKey(ToRequestKey(tube));
    }
}
