using MediatR;
using Pharos.Server.Application.Queries;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetMethodsAndTest;

public class GetReflexTestsQuery : GetTestsQuery;

internal class GetGetReflexTestsQueryHandler(IParametersDbContext context) : PartialQueryHandlerBase<GetReflexTestsQuery, MethodTest, MethodTestDto>, IRequestHandler<GetReflexTestsQuery, PartialQueryResult<MethodTestDto>>
{
    public async Task<PartialQueryResult<MethodTestDto>> Handle(GetReflexTestsQuery request, CancellationToken cancellationToken)
    {
        var methods = context.Methods.Where(x => x.Enabled && !x.IsTemplate);
        var query = context.MethodTests
            .Where(x => methods.Contains(x.Method) && x.Active && !x.Disabled)
            .Include(x => x.Method)
            .AsQueryable();

        return await GetPartialQueryResultAsync(query, request, cancellationToken);
    }

    protected override IQueryable<MethodTestDto> ApplyProjection(IQueryable<MethodTest> query)
    {
        return query.Select(x =>
            new MethodTestDto
            {
                Id = x.Id,
                MethodName = x.Method.MethodName,
                TestName = x.TestName,
                LabTestName = x.TestName,
                LongMethodName = x.Method.FullMethodName,
                FullTestName = x.FullTestName,
                Replicates = x.Replicates,
                DilutionFactor = x.DilutionFactor,
                Technology = x.Method.Technology,
                ReportConcentration = x.ReportConcentration,
                ReportClass = x.ReportClass,
                ReportCutOff = x.ReportCutOff,
                QuotientReportMode = x.ReportQuotient,
                UsesInstrumentDilution = x.DilutionMethod == DilutionMethod.InstrumentDilution
            });
    }

    protected override IQueryable<MethodTest> AddFiltering(IQueryable<MethodTest> query, GetReflexTestsQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.TestName))
        {
            query = query.Where(x => x.TestName.Contains(request.TestName));
        }

        if (!string.IsNullOrWhiteSpace(request.LongTestName))
        {
            query = query.Where(x => x.FullTestName.Contains(request.LongTestName));
        }

        if (!string.IsNullOrWhiteSpace(request.MethodName))
        {
            query = query.Where(x => x.Method.MethodName.Contains(request.MethodName));
        }

        if (request.SelectedMethods is { Count: > 0 })
        {
            query = query.Where(x => request.SelectedMethods.Contains(x.Method.MethodName));
        }

        return query;
    }

    protected override IQueryable<MethodTest> AddSorting(IQueryable<MethodTest> query, string? sortProperty, bool ascending)
    {
        query = sortProperty switch
        {
            _ => ascending
                ? query.OrderBy(x => x.Method.SortOrder).ThenBy(x => x.Method.MethodName).ThenBy(x => x.TestNameNaturalSortKey)
                : query.OrderByDescending(x => x.Method.SortOrder).ThenByDescending(x => x.Method.MethodName).ThenByDescending(x => x.TestNameNaturalSortKey)
        };

        return query;
    }
}