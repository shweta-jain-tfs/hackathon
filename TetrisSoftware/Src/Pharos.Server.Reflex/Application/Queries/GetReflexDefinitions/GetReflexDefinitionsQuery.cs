using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Reflex.Domain;
using Pharos.Server.Reflex.Interfaces;

namespace Pharos.Server.Reflex.Application.Queries.GetReflexDefinitions;

public class GetReflexDefinitionsQuery : SortedPartialQuery<ReflexDefinitionSummaryDto>
{
    public string? Name { get; set; }
    public bool? Automatic { get; set; }
    public bool? Active { get; set; }
    public string? RequestorId { get; set; }
}

internal sealed class GetReflexDefinitionsQueryHandler(IReflexDbContext dbContext, IOptionsSnapshot<RequestSettings> requestSettings, IAuthorizationService authorizationService)
    : PartialQueryHandlerBase<GetReflexDefinitionsQuery, ReflexDefinition, ReflexDefinitionSummaryDto>,
      IRequestHandler<GetReflexDefinitionsQuery, PartialQueryResult<ReflexDefinitionSummaryDto>>
{
    private bool _canHandleRequestorInfo;

    public async Task<PartialQueryResult<ReflexDefinitionSummaryDto>> Handle(GetReflexDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var personalAndHealthPermissions = await authorizationService.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData);

        _canHandleRequestorInfo =
            requestSettings.Value.IsRequestorInformationEnabled &&
            personalAndHealthPermissions;

        var query = dbContext.ReflexDefinitions
            .Include(x => x.ReflexCondition)
            .Include(x => x.ReflexTests)
            .AsQueryable();

        return await GetPartialQueryResultAsync(query, request, cancellationToken);
    }

    protected override IQueryable<ReflexDefinition> AddFiltering(IQueryable<ReflexDefinition> query, GetReflexDefinitionsQuery request)
    {
        if (request.Name != null)
        {
            query = query.Where(x => x.Name != null && x.Name.Contains(request.Name));
        }

        if (request.RequestorId != null)
        {
            query = query.Where(x => x.RequestorId != null && x.RequestorId.Contains(request.RequestorId));
        }

        return query;
    }

    protected override IQueryable<ReflexDefinition> AddSorting(IQueryable<ReflexDefinition> query, string? sortProperty, bool ascending)
    {
        query = sortProperty switch
        {
            nameof(ReflexDefinitionSummaryDto.Name) => ascending ? query.OrderBy(x => x.Name) : query.OrderByDescending(x => x.Name),
            nameof(ReflexDefinitionSummaryDto.RequestorId) => ascending ? query.OrderBy(x => x.RequestorId) : query.OrderByDescending(x => x.RequestorId),
            _ => query.OrderByDescending(x => x.Created)
        };

        return query;
    }

    protected override IQueryable<ReflexDefinitionSummaryDto> ApplyProjection(IQueryable<ReflexDefinition> query)
    {
        return query.Select(r => new ReflexDefinitionSummaryDto
        {
            Id = r.Id,
            Name = r.Name,
            Automatic = r.Automatic,
            Active = r.Active,
            HasRequestorEnabled = _canHandleRequestorInfo && r.RequestorId != null,
            RequestorId = _canHandleRequestorInfo ? r.RequestorId : null
        });
    }
}