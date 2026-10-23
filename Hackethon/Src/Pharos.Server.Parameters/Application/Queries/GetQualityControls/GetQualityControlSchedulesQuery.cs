using MediatR;
using Pharos.Server.Application.Queries;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetQualityControls;

public class QualityControlScheduleDto
{
    public int Id { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public string QualityControlName { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public bool FirstInRun { get; set; }
    public bool LastInRun { get; set; }
    public bool AfterCalibration { get; set; }
    public Periodically Periodically { get; set; }
    public int StartAfter { get; set; } //number of tests or minutes
    public int RunEvery { get; set; } //number of tests or minutes
}

public class GetQualityControlSchedulesQuery : SortedPartialQuery<QualityControlScheduleDto>
{
    public List<string> Methods { get; set; } = [];
}


internal class GetQualityControlSchedulesQueryHandler : PartialQueryHandlerBase<GetQualityControlSchedulesQuery, QualityControlSchedule, QualityControlScheduleDto>, IRequestHandler<GetQualityControlSchedulesQuery, PartialQueryResult<QualityControlScheduleDto>>
{
    private readonly IParametersDbContext _dbContext;

    public GetQualityControlSchedulesQueryHandler(IParametersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override IQueryable<QualityControlSchedule> AddFiltering(IQueryable<QualityControlSchedule> query, GetQualityControlSchedulesQuery request)
    {
        query = query.Where(x => x.QualityControlTest.Enabled && x.QualityControlTest.Test.Active && x.QualityControlTest.Test.Method.Enabled);
        if (request.Methods.Count > 0)
        {
            query = query.Where(x => request.Methods.Contains(x.QualityControlTest.Test.Method.MethodName));
        }
        return query;
    }

    protected override IQueryable<QualityControlSchedule> AddSorting(IQueryable<QualityControlSchedule> query, string? sortProperty, bool ascending)
    {
        return query.OrderBy(x => x.QualityControlTest.Test.Method.MethodName)
            .ThenBy(x => x.QualityControlTest.QualityControl.Name)
            .ThenBy(x => x.QualityControlTest.Test.TestName);
    }

    protected override async Task<int?> GetGroupedCountAsync(IQueryable<QualityControlSchedule> query, GetQualityControlSchedulesQuery request, CancellationToken cancellationToken)
    {
        // One logical group per Method
        return await query
            .Select(x => x.QualityControlTest.Test.MethodId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    protected override IQueryable<QualityControlScheduleDto> ApplyProjection(IQueryable<QualityControlSchedule> query)
    {
        return query.Select(x => new QualityControlScheduleDto
        {
            Id = x.Id,
            MethodName = x.QualityControlTest.Test.Method.MethodName,
            QualityControlName = x.QualityControlTest.QualityControl.Name,
            TestName = x.QualityControlTest.Test.TestName,
            FirstInRun = x.FirstInRun,
            LastInRun = x.LastInRun,
            AfterCalibration = x.AfterCalibration,
            Periodically = x.Periodically,
            StartAfter = x.StartAfter,
            RunEvery = x.RunEvery
        });
    }

    public async Task<PartialQueryResult<QualityControlScheduleDto>> Handle(GetQualityControlSchedulesQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.QualityControlSchedules
            .Include(x => x.QualityControlTest)
            .ThenInclude(x => x.Test)
            .ThenInclude(x => x.Method)
            .Include(x => x.QualityControlTest)
            .ThenInclude(x => x.QualityControl)
            .AsSplitQuery()
            .AsNoTracking();

        return await GetPartialQueryResultAsync(query, request, cancellationToken);
    }
}