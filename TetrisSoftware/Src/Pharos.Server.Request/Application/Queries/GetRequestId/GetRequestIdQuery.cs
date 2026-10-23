using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Infrastructure;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Queries.GetRequestId;

internal class GetRequestIdQueryHandler : IRequestHandler<GetRequestIdQuery, RequestIdResponseDto>
{
    private readonly IRequestDbContext _dbContext;
    private readonly IDateTime _dateTime;
    private readonly RequestSettings _settings;
    private readonly IOptionsMonitor<SamplesSettings> _samplesSettings;

    public GetRequestIdQueryHandler(IRequestDbContext dbContext, IDateTime dateTime, IOptionsSnapshot<RequestSettings> requestSettings, IOptionsMonitor<SamplesSettings> samplesSettings)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
        _settings = requestSettings.Value;
        _samplesSettings = samplesSettings;
    }

    public async Task<RequestIdResponseDto> Handle(GetRequestIdQuery request, CancellationToken cancellationToken)
    {
        var sampleId = request.SampleId;

        // Use the Sample ID index for the normal lookup.
        var candidates = await _dbContext.Requests
            .Where(x => x.SampleId == sampleId)
            .ToListAsync(cancellationToken);

        // Apply the setting explicitly because SQL may ignore letter casing.
        var caseInsensitiveSampleId = _samplesSettings.CurrentValue.CaseInsensitiveSampleId;
        var comparison = caseInsensitiveSampleId
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var firstMatch = FindPreferredMatch(candidates, request, comparison);

        if (firstMatch == null && caseInsensitiveSampleId)
        {
            var sampleIdUpper = sampleId.ToUpperInvariant();

            // Search casing variants when the database collation did not find them.
#pragma warning disable CA1862
            candidates = await _dbContext.Requests
                .Where(x => x.SampleId.ToUpper() == sampleIdUpper)
                .ToListAsync(cancellationToken);
#pragma warning restore CA1862

            firstMatch = FindPreferredMatch(candidates, request, comparison);
        }

        return new RequestIdResponseDto(firstMatch?.Id ?? -1, firstMatch?.Created ?? DateTimeOffset.MinValue, firstMatch != null, firstMatch?.SampleId);
    }

    /// <summary>
    /// Prefers an exact-case match, then selects the newest matching request.
    /// </summary>
    private Domain.Request? FindPreferredMatch(
        IEnumerable<Domain.Request> candidates,
        GetRequestIdQuery request,
        StringComparison comparison)
    {
        var matchingRequests = candidates
            .Where(x => string.Equals(x.SampleId, request.SampleId, comparison));

        if (request.RequestCreatedDate.HasValue)
        {
            matchingRequests = matchingRequests
                .Where(x => x.Created == request.RequestCreatedDate.Value);
        }
        else if (_settings.ReuseSampleId)
        {
            var date = _dateTime.Now.Subtract(TimeSpan.FromDays(_settings.DaysBeforeReuseOfSampleId));
            matchingRequests = matchingRequests.Where(x => x.Created >= date);
        }

        return matchingRequests
            .OrderByDescending(x => string.Equals(x.SampleId, request.SampleId, StringComparison.Ordinal))
            .ThenByDescending(x => x.Created)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();
    }
}
