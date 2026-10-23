using MediatR;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetMethodPanelDetails;

internal record GetMethodPanelDetailsQuery(int PanelId) : IRequest<MethodPanelDetailsDto>;

internal class GetMethodPanelDetailsQueryHandler(IParametersDbContext context) : IRequestHandler<GetMethodPanelDetailsQuery, MethodPanelDetailsDto>
{
    public async Task<MethodPanelDetailsDto> Handle(GetMethodPanelDetailsQuery request, CancellationToken cancellationToken)
    {
        var result = await context.Panels
            .Include(x => x.Tests)
            .ThenInclude(x => x.MethodTest)
            .ThenInclude(x => x.Method)
            .Where(x => x.Id == request.PanelId)
            .Select(x => new MethodPanelDetailsDto
            {
                Id = x.Id,
                PanelName = x.PanelName,
                PanelLongName = x.PanelLongName,
                Grouped = x.GroupedPanel,
                Enabled = x.Enabled,
                Tests = x.Tests.OrderBy(t => t.SortOrder).Select(t => new MethodPanelTestDto
                {
                    Id = t.Id,
                    Replicates = t.Replicates,
                    InstrumentDilutionFactor = t.InstrumentDilutionFactor,
                    SortOrder = t.SortOrder,
                    MethodName = t.MethodTest.Method.MethodName,
                    FullMethodName = t.MethodTest.Method.FullMethodName,
                    TestName = t.MethodTest.TestName,
                    FullTestName = t.MethodTest.FullTestName
                }).ToList()
            })
            .SingleAsync(cancellationToken) ?? throw new NotFoundException($"Panel not found. Id: {request.PanelId}");

        return result;
    }
}
