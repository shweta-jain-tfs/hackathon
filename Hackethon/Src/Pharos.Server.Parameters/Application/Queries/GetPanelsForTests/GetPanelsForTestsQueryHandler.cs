using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetPanelsForTests;

internal class GetPanelsForTestsQueryHandler : IRequestHandler<Server.Application.Parameters.GetPanelsForTestsQuery, List<Server.Application.Parameters.PanelForRecognitionDto>>
{
    private readonly IParametersDbContext _dbContext;

    public GetPanelsForTestsQueryHandler(IParametersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<PanelForRecognitionDto>> Handle(GetPanelsForTestsQuery request, CancellationToken cancellationToken)
    {
        var requestMethodTests = request.MethodTests.ToHashSet();

        var panels = await _dbContext.Panels
            .AsNoTracking()
            .Include(p => p.Tests)
            .ThenInclude(pt => pt.MethodTest)
            .ThenInclude(mt => mt.Method)
            .Select(p => new 
            {
                Panel = p,
                Tests = p.Tests.Select(pt => new ValueTuple<string, string>(pt.MethodTest.Method.MethodName, pt.MethodTest.TestName)).ToList()
            })
            .ToListAsync(cancellationToken);

        var filteredPanels = panels
            .Where(p => p.Tests.Any(test => requestMethodTests.Contains(test)))
            .Select(p => new PanelForRecognitionDto
            {
                Id = p.Panel.Id,
                PanelName = p.Panel.PanelName,
                Tests = p.Tests
            })
            .ToList();

        return filteredPanels;
    }
}
