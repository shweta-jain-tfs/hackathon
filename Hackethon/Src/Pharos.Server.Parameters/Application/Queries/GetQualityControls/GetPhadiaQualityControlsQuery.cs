using MediatR;
using Pharos.Server.Application.Queries;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetQualityControls;

public class GetPhadiaQualityControlsQuery : SortedPartialQuery<PhadiaQualityControlDto>
{
    public bool EnabledOnly { get; set; }
    public List<string> Methods { get; set; } = [];
}

public class PhadiaQualityControlDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public int TestId { get; set; }
    public bool Enabled { get; set; }
}

internal class GetPhadiaQualityControlsQueryHandler : PartialQueryHandlerBase<GetPhadiaQualityControlsQuery, PhadiaQualityControlTest, PhadiaQualityControlDto>, IRequestHandler<GetPhadiaQualityControlsQuery, PartialQueryResult<PhadiaQualityControlDto>>
{
    private readonly IParametersDbContext _dbContext;

    public GetPhadiaQualityControlsQueryHandler(IParametersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PartialQueryResult<PhadiaQualityControlDto>> Handle(GetPhadiaQualityControlsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.PhadiaQualityControlTests
            .Include(x => x.QualityControl)
            .Include(x => x.Test)
            .ThenInclude(x => x.Method).AsNoTracking();

        return await GetPartialQueryResultAsync(query, request, cancellationToken);
    }

    protected override IQueryable<PhadiaQualityControlTest> AddFiltering(IQueryable<PhadiaQualityControlTest> query, GetPhadiaQualityControlsQuery request)
    {
        query = query.Where(x => x.Test.Method.Enabled);

        if (request.EnabledOnly)
        {
            query = query.Where(x => x.Enabled);
        }
        if (request.Methods.Count > 0)
        {
            query = query.Where(x => request.Methods.Contains(x.Test.Method.MethodName));
        }
        return query;
    }

    protected override async Task<int?> GetGroupedCountAsync(IQueryable<PhadiaQualityControlTest> query, GetPhadiaQualityControlsQuery request, CancellationToken cancellationToken)
    {
        // One logical group per PhadiaQualityControl (multiple tests may exist per QC).
        return await query
            .Select(x => x.QualityControl)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    protected override IQueryable<PhadiaQualityControlTest> AddSorting(IQueryable<PhadiaQualityControlTest> query, string? sortProperty, bool ascending)
    {
        return query.OrderBy(x => x.QualityControl.Name).ThenBy(x => x.Test.Method.MethodName).ThenBy(x => x.Test.TestName);
    }

    protected override IQueryable<PhadiaQualityControlDto> ApplyProjection(IQueryable<PhadiaQualityControlTest> query)
    {
        return query.Select(qc => new PhadiaQualityControlDto
        {
            Id = qc.QualityControl.Id,
            Name = qc.QualityControl.Name,
            MethodName = qc.Test.Method.MethodName,
            TestName = qc.Test.TestName,
            TestId = qc.Id,
            Enabled = qc.Enabled
        });
    }
}