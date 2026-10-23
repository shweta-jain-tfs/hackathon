using MediatR;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.Request;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Parameters.Application.Queries.GetMethodsAndTest;
using Pharos.Server.Parameters.Application.Queries.GetPanelTests;

namespace Pharos.Server.Parameters.Application.Queries.GetPanels;

public class GetPanelsQuery : SortedPartialQuery<PanelDto>
{
    public string? PanelName { get; set; }
    public string? LongPanelName { get; set; }
}

internal class GetPanelsQueryHandler : PartialQueryHandlerBase<GetPanelsQuery, Panel, PanelDto>, IRequestHandler<GetPanelsQuery, PartialQueryResult<PanelDto>>
{
    private readonly IParametersDbContext _context;
    private readonly ISender _sender;
    private bool _isUsingLabTestNames;

    public GetPanelsQueryHandler(IParametersDbContext context, ISender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<PartialQueryResult<PanelDto>> Handle(GetPanelsQuery request, CancellationToken cancellationToken)
    {
        _isUsingLabTestNames = await _sender.Send(new GetIsUsingLaboratoryTestNamesQuery(), cancellationToken);

        var query = _context.Panels
            .Include(p => p.Tests)
            .ThenInclude(t => t.MethodTest)
            .ThenInclude(t => t.Method)
            .AsQueryable();

        if (_isUsingLabTestNames)
        {
            query = query
                .Include(p => p.Tests)
                .ThenInclude(t => t.MethodTest)
                .ThenInclude(x => x.LabTestNames);
        }

        query = query
            .Where(x => x.Enabled && x.Tests.All(t => t.MethodTest.Active && !t.MethodTest.Disabled && t.MethodTest.Method.Enabled))
            .AsSplitQuery();

        return await GetPartialQueryResultAsync(query, request, cancellationToken);
    }

    protected override IQueryable<PanelDto> ApplyProjection(IQueryable<Panel> query)
    {
        return query.Select(x =>
            new PanelDto
            {
                Id = x.Id,
                PanelName = x.PanelName,
                PanelLongName = x.PanelLongName,
                PanelTests = Map(x.Tests, _isUsingLabTestNames)
            });
    }

    private static List<PanelTestDto> Map(IList<PanelTest> panelTests, bool isUsingLabTestNames)
    {
        var tests = panelTests.Select(test => new PanelTestDto
        {
            Id = test.Id,
            MethodTest = new MethodTestDto
            {
                Id = test.MethodTest.Id,
                TestName = test.MethodTest.TestName,
                LabTestName = isUsingLabTestNames ? (test.MethodTest.LabTestNames.FirstOrDefault()?.TestName ?? test.MethodTest.TestName) : test.MethodTest.TestName,
                FullTestName = test.MethodTest.FullTestName,
                MethodName = test.MethodTest.Method.MethodName,
                LongMethodName = test.MethodTest.Method.FullMethodName,
                Replicates = test.MethodTest.Replicates,
                DilutionFactor = test.MethodTest.DilutionFactor,
                Technology = test.MethodTest.Method.Technology,
                UsesInstrumentDilution = test.MethodTest.DilutionMethod == DilutionMethod.InstrumentDilution,
                ReportClass = test.MethodTest.ReportClass,
                ReportConcentration = test.MethodTest.ReportConcentration,
                ReportCutOff = test.MethodTest.ReportCutOff,
                QuotientReportMode = test.MethodTest.ReportQuotient
            }

        }).ToList();

        return tests;
    }

    protected override IQueryable<Panel> AddFiltering(IQueryable<Panel> query, GetPanelsQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.PanelName))
        {
            query = query.Where(x => x.PanelName.Contains(request.PanelName));
        }
        if (!string.IsNullOrWhiteSpace(request.LongPanelName))
        {
            query = query.Where(x => x.PanelLongName.Contains(request.LongPanelName));
        }

        return query;
    }

    protected override IQueryable<Panel> AddSorting(IQueryable<Panel> query, string? sortProperty, bool ascending)
    {
        query = sortProperty switch
        {
            nameof(PanelDto.PanelName) => ascending ? query.OrderBy(x => x.PanelName) : query.OrderByDescending(x => x.PanelName),
            nameof(PanelDto.PanelLongName) => ascending ? query.OrderBy(x => x.PanelLongName) : query.OrderByDescending(x => x.PanelLongName),
            _ => query.OrderBy(x => x.PanelName)
        };

        return query;
    }
}