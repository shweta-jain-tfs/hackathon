using MediatR;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetTestResults;

public class GetTestResultIndexQuery : IRequest<int>
{
    public Guid TestOrderId { get; set; }
    public int AnalyticalRunId { get; set; }
    public TestResultStatus[]? TestResultStatus { get; set; }
    public string? Identity { get; set; }
    public TestResultType[]? ResultTypes { get; set; }
    public string? SortByProperty { get; set; }
    public bool SortByAscending { get; set; }
}

internal class GetTestResultIndexQueryHandler(IResultDbContext context) : IRequestHandler<GetTestResultIndexQuery, int>
{
    public async Task<int> Handle(GetTestResultIndexQuery request, CancellationToken cancellationToken)
    {
        var query = context.TestResults
            .Where(x => x.ResultType == TestResultType.Sample || x.ResultType == TestResultType.QualityControl);

        query = ApplyFiltering(query, request);
        query = TestResultSortingHelper.ApplySorting(query, request.SortByProperty, request.SortByAscending);

        var sortedTestOrderIds = await query
            .Select(x => x is SampleTestResult ? (Guid?)((SampleTestResult)x).TestOrderId : null)
            .ToListAsync(cancellationToken);

        // QC rows project to null and simply occupy a position in the list (contributing to the
        // index count) without ever matching request.TestOrderId.
        var index = sortedTestOrderIds.IndexOf(request.TestOrderId);
        return index;
    }

    private static IQueryable<TestResult> ApplyFiltering(IQueryable<TestResult> query, GetTestResultIndexQuery request)
    {
        query = query.Where(x => x.AnalyticalRunId == request.AnalyticalRunId);

        if (request.Identity != null)
        {
            query = query.Where(x =>
                (x is SampleTestResult && ((SampleTestResult)x).RequestUid.SampleId.Contains(request.Identity)) ||
                (x is QualityControlTestResult && ((QualityControlTestResult)x).QualityControlName.Contains(request.Identity)));
        }

        if (request.TestResultStatus is { Length: > 0 })
        {
            query = query.Where(x => request.TestResultStatus.Contains(x.Status));
        }

        if (request.ResultTypes is { Length: > 0 })
        {
            query = query.Where(x => request.ResultTypes.Contains(x.ResultType));
        }

        return query;
    }
}