using MediatR;
using Pharos.Server.Application.Queries;
using Pharos.Server.Domain.Common;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Queries.GetRequestor;

public class GetRequestorQuery : SortedPartialQuery<RequestorDto>
{
    public string? RequestorId { get; set; }

    [ProtectedData]
    public string? Name { get; set; }
}

internal sealed class GetRequestorQueryHandler : PartialQueryHandlerBase<GetRequestorQuery, Requestor, RequestorDto>, IRequestHandler<GetRequestorQuery, PartialQueryResult<RequestorDto>>
{
    private readonly IRequestDbContext _context;

    public GetRequestorQueryHandler(IRequestDbContext context)
    {
        _context = context;
    }

    public async Task<PartialQueryResult<RequestorDto>> Handle(GetRequestorQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Requestors;

        var result = await GetPartialQueryResultAsync(query, request, cancellationToken);

        return result;
    }

    protected override IQueryable<Requestor> AddFiltering(IQueryable<Requestor> query, GetRequestorQuery request)
    {
        if (string.IsNullOrWhiteSpace(request.RequestorId) || string.IsNullOrWhiteSpace(request.Name))
        {
            return query;
        }

        var allRequestors = query.ToList();
        var filteredRequestorIds = allRequestors
            .Where(x => (x.Name != null && x.Name.Contains(request.Name, StringComparison.CurrentCultureIgnoreCase)) || x.RequestorId.Contains(request.RequestorId, StringComparison.CurrentCultureIgnoreCase))
            .Select(x => x.Id)
            .ToList();
        query = query.Where(x => filteredRequestorIds.Contains(x.Id));
        return query;
    }

    protected override IQueryable<Requestor> AddSorting(IQueryable<Requestor> query, string? sortProperty, bool ascending)
    {
        return query;
    }

    protected override IQueryable<RequestorDto> ApplyProjection(IQueryable<Requestor> query)
    {
        return query.Select(r => new RequestorDto
        {
            Id = r.Id,
            RequestorId = r.RequestorId,
            Name = r.Name,
            ReflexEnabled = r.ReflexEnabled
        });
    }
}