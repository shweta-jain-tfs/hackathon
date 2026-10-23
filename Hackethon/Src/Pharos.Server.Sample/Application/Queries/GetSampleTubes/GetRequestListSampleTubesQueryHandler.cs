using Pharos.Server.Application.Sample;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Mapping;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Application.Queries.GetSampleTubes;

internal sealed class GetRequestListSampleTubesQueryHandler(
    IInstrumentSampleStorage sampleStorage,
    ISampleDbContext dbContext) : IRequestHandler<GetRequestListSampleTubesQuery, List<CurrentSampleTubesDto>>
{
    /// <summary>
    /// Identifies one request. SampleId can be reused, so the creation date is part of the key.
    /// </summary>
    private readonly record struct RequestKey(string SampleId, DateTimeOffset RequestCreatedDate);

    public async Task<List<CurrentSampleTubesDto>> Handle(GetRequestListSampleTubesQuery request, CancellationToken cancellationToken)
    {
        // Request identity is SampleId + RequestCreatedDate. SampleId alone is not unique.
        var requestedRequestsByKey = request.RequestUids
            .DistinctBy(ToRequestKey)
            .ToDictionary(ToRequestKey);

        if (requestedRequestsByKey.Count == 0)
        {
            return [];
        }

        // SampleId is used as a broad lookup key because request-list Last Tray follows current Tube tab rows.
        var requestedSampleIds = requestedRequestsByKey.Keys
            .Select(x => x.SampleId)
            .ToHashSet(StringComparer.Ordinal);

        var requestedRequestKeysBySampleId = requestedRequestsByKey.Keys
            .GroupBy(x => x.SampleId, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.Ordinal);

        var storageTubes = sampleStorage
            .GetTubesBySampleIds(requestedSampleIds, includeTubesWithError: true)
            .ToList();

        var dbTubesNotInStorage = await GetRequestedDbTubesNotInStorage(
            requestedSampleIds,
            storageTubes,
            cancellationToken);

        // Storage has the live tube-tab view; match it by SampleId so reused requests see the current Last Tray.
        var storageTubesByRequest = storageTubes
            .SelectMany(tube => ResolveStorageTubeRequestKeys(tube, requestedRequestKeysBySampleId)
                .Select(requestKey => new { Tube = tube, RequestKey = requestKey }))
            .GroupBy(x => x.RequestKey, x => x.Tube)
            .ToDictionary(g => g.Key, g => GetCurrentPhysicalTubes(g).ToCurrentSampleTubeDtos());

        // DB fallback lets Last Tray survive restart when live instrument storage is empty.
        var dbTubesByRequest = dbTubesNotInStorage
            .Where(tube => IsRequestedTube(tube, requestedRequestsByKey))
            .Select(tube => new { Tube = tube, RequestKey = ToRequestKey(tube) })
            .GroupBy(x => x.RequestKey, x => x.Tube)
            .ToDictionary(g => g.Key, g => GetCurrentPhysicalTubes(g).ToCurrentSampleTubeDtos());

        // Prefer live current tubes. Fall back to persisted location rows only when live storage has no match.
        return requestedRequestsByKey
            .Select(request => new CurrentSampleTubesDto(
                request.Value,
                storageTubesByRequest.GetValueOrDefault(request.Key, dbTubesByRequest.GetValueOrDefault(request.Key, []))))
            .ToList();
    }

    /// <summary>
    /// Gets persisted tubes for the requested samples that are not currently represented in instrument storage.
    /// </summary>
    private async Task<List<SampleTube>> GetRequestedDbTubesNotInStorage(
        HashSet<string> requestedSampleIds,
        IReadOnlyCollection<SampleTube> storageTubes,
        CancellationToken cancellationToken)
    {
        // Match by tube id to avoid duplicate rows between live storage and persisted fallback.
        var storageTubeIds = storageTubes
            .Where(t => !string.IsNullOrWhiteSpace(t.TubeId))
            .Select(t => t.TubeId!)
            .ToHashSet(StringComparer.Ordinal);

        // Include persisted in-instrument rows so Last Tray can be rebuilt after an app restart.
        return await dbContext.SampleTubes
            .Where(t => t.SampleId != null
                        && requestedSampleIds.Contains(t.SampleId)
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

    private static IEnumerable<SampleTube> GetCurrentPhysicalTubes(IEnumerable<SampleTube> tubes)
    {
        var materializedTubes = tubes.ToList();

        // One physical tube can have multiple persisted rows after moves. Keep only the latest location per TubeId.
        var identifiedTubes = materializedTubes
            .Where(tube => !string.IsNullOrWhiteSpace(tube.TubeId))
            .GroupBy(tube => tube.TubeId)
            .Select(group => group
                .OrderByDescending(tube => tube.LoadTimestamp ?? DateTimeOffset.MinValue)
                .ThenByDescending(tube => tube.Id)
                .First());

        var unidentifiedTubes = materializedTubes
            .Where(tube => string.IsNullOrWhiteSpace(tube.TubeId));

        return identifiedTubes.Concat(unidentifiedTubes);
    }

    private static List<RequestKey> ResolveStorageTubeRequestKeys(
        SampleTube tube,
        Dictionary<string, List<RequestKey>> requestedRequestKeysBySampleId)
    {
        if (tube.SampleId == null || !requestedRequestKeysBySampleId.TryGetValue(tube.SampleId, out var requestKeys))
        {
            return [];
        }

        if (tube.RequestCreatedDate is { } requestCreatedDate)
        {
            var key = new RequestKey(tube.SampleId, requestCreatedDate);

            return requestKeys.Contains(key)
                ? [key]
                : [];
        }

        // A tube without request creation date cannot be safely tied to one request when SampleId is reused.
        // Assign it only when exactly one request exists for the SampleId, otherwise keep ownership unresolved.
        return requestKeys.Count == 1
            ? requestKeys
            : [];
    }
}
