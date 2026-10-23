using MediatR;
using Pharos.Server.Application.Queries;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetQualityControls;

public class GetLabQualityControlsQuery : SortedPartialQuery<LabQualityControlDto>
{
    public bool EnabledOnly { get; set; }
    public List<string> Methods { get; set; } = [];
}

public class LabQualityControlDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IdOnBarcode { get; set; } = string.Empty;
    public string BarcodeFormatName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public int TestId { get; set; }
    public bool Enabled { get; set; }
}

internal class GetLabQualityControlsQueryHandler : PartialQueryHandlerBase<GetLabQualityControlsQuery, LabQualityControlTest, LabQualityControlDto>, IRequestHandler<GetLabQualityControlsQuery, PartialQueryResult<LabQualityControlDto>> //IRequestHandler<GetLabQualityControlsQuery, List<LabQualityControlDto>>
{
    private readonly IParametersDbContext _dbContext;

    public GetLabQualityControlsQueryHandler(IParametersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PartialQueryResult<LabQualityControlDto>> Handle(GetLabQualityControlsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.LabQualityControlTests
            .Include(x => x.QualityControl)
            .AsNoTracking();

        return await GetPartialQueryResultAsync(query, request, cancellationToken);
    }

    protected override IQueryable<LabQualityControlTest> AddFiltering(IQueryable<LabQualityControlTest> query, GetLabQualityControlsQuery request)
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

    protected override async Task<int?> GetGroupedCountAsync(IQueryable<LabQualityControlTest> query, GetLabQualityControlsQuery request, CancellationToken cancellationToken)
    {
        // One logical group per LabQualityControl (multiple tests may exist per QC).
        return await query
            .Select(x => x.QualityControlId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    protected override IQueryable<LabQualityControlTest> AddSorting(IQueryable<LabQualityControlTest> query, string? sortProperty, bool ascending)
    {
        return query.OrderBy(x => x.QualityControl.Name).ThenBy(x => x.Test.Method.MethodName).ThenBy(x => x.Test.TestName);
    }

    protected override IQueryable<LabQualityControlDto> ApplyProjection(IQueryable<LabQualityControlTest> query)
    {
        return query.Select(qc => new LabQualityControlDto
        {
            Id = qc.QualityControl.Id,
            Name = qc.QualityControl.Name,
            IdOnBarcode = ((LabQualityControl)qc.QualityControl).IdOnBarcode,
            BarcodeFormatName = ((LabQualityControl)qc.QualityControl).BarcodeFormat!.Name,
            MethodName = qc.Test.Method.MethodName,
            TestName = qc.Test.TestName,
            TestId = qc.Id,
            Enabled = qc.Enabled
        });
    }
}