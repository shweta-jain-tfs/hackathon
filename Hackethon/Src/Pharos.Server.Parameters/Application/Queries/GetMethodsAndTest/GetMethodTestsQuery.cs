using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Queries;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetMethodsAndTest;

public class GetMethodTestsQuery : SortedPartialQuery<MethodTestDto>
{
    public int MethodId { get; set; }
    public string? SearchString { get; set; }
}

internal sealed class GetMethodTestsByMethodIdQueryHandler(IParametersDbContext context)
    : PartialQueryHandlerBase<GetMethodTestsQuery, MethodTest, MethodTestDto>, IRequestHandler<GetMethodTestsQuery, PartialQueryResult<MethodTestDto>>
{
    public async Task<PartialQueryResult<MethodTestDto>> Handle(GetMethodTestsQuery request, CancellationToken cancellationToken)
    {
        var query = context.MethodTests
                        .Where(x => x.Method.Id == request.MethodId
                            && !x.Disabled
                            && ((!x.IsRuo && string.IsNullOrEmpty(x.SpecialMethodName))
                                || x.IsSpecializedTestActivated
                                || x.Method.IsRuo))
                        .AsQueryable();

        return await GetPartialQueryResultAsync(query, request, cancellationToken);
    }

    protected override IQueryable<MethodTestDto> ApplyProjection(IQueryable<MethodTest> query)
    {
        return query.Select(x => new MethodTestDto
        {
            Id = x.Id,
            MethodName = x.Method.MethodName,
            TestName = x.TestName,
            LabTestName = x.TestName,
            LongMethodName = x.Method.FullMethodName,
            FullTestName = x.FullTestName,
            Replicates = x.Replicates,
            DilutionFactor = x.InstrumentDilutionFactor,
            Technology = x.Method.Technology,
            ReportConcentration = x.ReportConcentration,
            ReportClass = x.ReportClass,
            ReportCutOff = x.ReportCutOff,
            QuotientReportMode = x.ReportQuotient,
            Active = x.Active,
            DefaultLabTestName = x.DefaultLabTestName,
            MethodConcentrationUnit = x.UserConcentrationUnit != null
                ? x.UserConcentrationUnit.ConcentrationUnit
                : x.DefaultConcentrationUnit.ConcentrationUnit,
            IsRuo = x.IsRuo,
            MethodTestLabNames = x.LabTestNames.Select(l => new MethodTestLabNameDto
            {
                Id = l.Id,
                TestName = l.TestName,
                SortOrder = l.SortOrder
            }).ToList(),
            UsesInstrumentDilution = x.DefaultDilutionMethod == DilutionMethod.InstrumentDilution
        });
    }

    protected override IQueryable<MethodTest> AddFiltering(IQueryable<MethodTest> query, GetMethodTestsQuery request)
    {
        if (string.IsNullOrWhiteSpace(request.SearchString))
        {
            return query;
        }

        var search = request.SearchString.ToLower();
        var isLongSearch = search.Length >= 3;

        // CA1862 suppressed: StringComparison overloads cannot be translated by EF Core to SQL.
        // ToLower() is used intentionally so the comparison is handled database-side via collation.
#pragma warning disable CA1862
        return query.Where(x =>
            x.TestName.ToLower().StartsWith(search) ||
            x.LabTestNames.Any(l => l.TestName.ToLower().StartsWith(search)) ||
            (isLongSearch && x.FullTestName.ToLower().Contains(search)));
#pragma warning restore CA1862
    }

    protected override IQueryable<MethodTest> AddSorting(IQueryable<MethodTest> query, string? sortProperty, bool ascending)
    {
        return ascending
            ? query.OrderBy(x => x.Method.SortOrder).ThenBy(x => x.Method.MethodName).ThenBy(x => x.TestNameNaturalSortKey)
            : query.OrderByDescending(x => x.Method.SortOrder).ThenByDescending(x => x.Method.MethodName).ThenByDescending(x => x.TestNameNaturalSortKey);
    }
}
