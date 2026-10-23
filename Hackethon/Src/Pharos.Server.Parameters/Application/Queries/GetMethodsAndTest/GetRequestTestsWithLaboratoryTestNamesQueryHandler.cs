using MediatR;
using Pharos.Server.Application.Queries;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetMethodsAndTest;

public class GetRequestTestsWithLaboratoryTestNamesQuery : GetTestsQuery;

internal class GetRequestTestsWithLaboratoryTestNamesQueryHandler(IParametersDbContext context) : PartialQueryHandlerBase<GetRequestTestsWithLaboratoryTestNamesQuery, MethodTestLabName, MethodTestDto>, IRequestHandler<GetRequestTestsWithLaboratoryTestNamesQuery, PartialQueryResult<MethodTestDto>>
{
    public async Task<PartialQueryResult<MethodTestDto>> Handle(GetRequestTestsWithLaboratoryTestNamesQuery request, CancellationToken cancellationToken)
    {
        var methods = context.Methods.Where(x => x.Enabled && !x.IsTemplate);
        var query = context.MethodTestLabNames
            .Where(x => methods.Contains(x.MethodTest.Method) && x.MethodTest.Active && !x.MethodTest.Disabled)
            .Include(x => x.MethodTest)
            .ThenInclude(x => x.Method)
            .AsQueryable();

        return await GetPartialQueryResultAsync(query, request, cancellationToken);
    }

    protected override IQueryable<MethodTestDto> ApplyProjection(IQueryable<MethodTestLabName> query)
    {
        return query.Select(x =>
            new MethodTestDto
            {
                Id = x.Id,
                MethodName = x.MethodTest.Method.MethodName,
                TestName = x.MethodTest.TestName,
                LabTestName = x.TestName,
                LongMethodName = x.MethodTest.Method.FullMethodName,
                FullTestName = x.MethodTest.FullTestName,
                Replicates = x.MethodTest.Replicates,
                DilutionFactor = x.MethodTest.DilutionFactor,
                Technology = x.MethodTest.Method.Technology,
                ReportConcentration = x.MethodTest.ReportConcentration,
                ReportClass = x.MethodTest.ReportClass,
                ReportCutOff = x.MethodTest.ReportCutOff,
                QuotientReportMode = x.MethodTest.ReportQuotient,
                UsesInstrumentDilution = x.MethodTest.DilutionMethod == DilutionMethod.InstrumentDilution
            });
    }

    protected override IQueryable<MethodTestLabName> AddFiltering(IQueryable<MethodTestLabName> query, GetRequestTestsWithLaboratoryTestNamesQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.TestName))
        {
            query = query.Where(x => x.TestName.Contains(request.TestName));
        }

        if (!string.IsNullOrWhiteSpace(request.LongTestName))
        {
            query = query.Where(x => x.MethodTest.FullTestName.Contains(request.LongTestName));
        }

        if (!string.IsNullOrWhiteSpace(request.MethodName))
        {
            query = query.Where(x => x.MethodTest.Method.MethodName.Contains(request.MethodName));
        }

        if (request.SelectedMethods is { Count: > 0 })
        {
            query = query.Where(x => request.SelectedMethods.Contains(x.MethodTest.Method.MethodName));
        }

        return query;
    }

    protected override IQueryable<MethodTestLabName> AddSorting(IQueryable<MethodTestLabName> query, string? sortProperty, bool ascending)
    {
        query = sortProperty switch
        {
            _ => ascending
                ? query.OrderBy(x => x.MethodTest.Method.SortOrder).ThenBy(x => x.MethodTest.Method.MethodName).ThenBy(x => x.TestNameNaturalSortKey)
                : query.OrderByDescending(x => x.MethodTest.Method.SortOrder).ThenByDescending(x => x.MethodTest.Method.MethodName).ThenByDescending(x => x.TestNameNaturalSortKey)
        };

        return query;
    }
}