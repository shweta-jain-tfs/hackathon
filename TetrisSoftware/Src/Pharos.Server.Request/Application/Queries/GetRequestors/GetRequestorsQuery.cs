using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Domain.Common;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Queries.GetRequestors;

public class GetRequestorsQuery : SortedPartialQuery<RequestorSummaryDto>
{
    public string? RequestorId { get; set; }

    [ProtectedData]
    public string? Name { get; set; }

    public bool BypassRowLimit { get; set; }
}

internal sealed class GetRequestorsQueryHandler : PartialQueryHandlerBase<GetRequestorsQuery, Requestor, RequestorSummaryDto>, IRequestHandler<GetRequestorsQuery, PartialQueryResult<RequestorSummaryDto>>
{
    private readonly IRequestDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly IOptionsSnapshot<RequestSettings> _requestSettings;

    public GetRequestorsQueryHandler(IRequestDbContext context, IAuthorizationService authorizationService, IOptionsSnapshot<RequestSettings> requestSettings)
    {
        _context = context;
        _authorizationService = authorizationService;
        _requestSettings = requestSettings;
    }

    public async Task<PartialQueryResult<RequestorSummaryDto>> Handle(GetRequestorsQuery request, CancellationToken cancellationToken)
    {
        var personalAndHealthPermissions = await _authorizationService.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData);

        var canHandleRequestorInfo =
            _requestSettings.Value.IsRequestorInformationEnabled &&
            personalAndHealthPermissions;

        if (!canHandleRequestorInfo)
        {
            throw new UnauthorizedAccessException("User is not authorized to view requestors.");
        }

        var query = _context.Requestors.AsQueryable();

        return await GetPartialQueryResultAsync(query, request, cancellationToken);
    }

    protected override IQueryable<Requestor> AddFiltering(IQueryable<Requestor> query, GetRequestorsQuery request)
    {
        if (!string.IsNullOrEmpty(request.RequestorId))
        {
            query = query.Where(x => x.RequestorId.Contains(request.RequestorId));
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            // Since Name is encrypted, we need to load all data and filter in memory
            var allRequestors = query.Where(x => x.Name != null).ToList();
            var filteredRequestorIds = allRequestors
                .Where(x => x.Name != null && x.Name.Contains(request.Name, StringComparison.CurrentCultureIgnoreCase))
                .Select(x => x.Id)
                .ToList();

            query = query.Where(x => filteredRequestorIds.Contains(x.Id));
        }

        return query;
    }

    protected override IQueryable<Requestor> AddSorting(IQueryable<Requestor> query, string? sortProperty, bool ascending)
    {
        query = sortProperty switch
        {
            nameof(RequestorSummaryDto.RequestorId) => ascending ? query.OrderBy(x => x.RequestorId) : query.OrderByDescending(x => x.RequestorId),
            nameof(RequestorSummaryDto.Name) => ascending ? query.OrderBy(x => x.Name) : query.OrderByDescending(x => x.Name),
            _ => query.OrderBy(x => x.RequestorId)
        };

        return query;
    }

    protected override IQueryable<RequestorSummaryDto> ApplyProjection(IQueryable<Requestor> query)
    {
        return query.Select(r => new RequestorSummaryDto
        {
            Id = r.Id,
            RequestorId = r.RequestorId,
            Name = r.Name,
            ReflexEnabled = r.ReflexEnabled,
            LatestSampleDate = r.LatestSampleDate,
            NumberOfReportedSamples = r.NumberOfReportedSamples,
            NumberOfReportedTests = r.NumberOfReportedTests,
            CounterResetDate = r.CounterResetDate
        });
    }
}