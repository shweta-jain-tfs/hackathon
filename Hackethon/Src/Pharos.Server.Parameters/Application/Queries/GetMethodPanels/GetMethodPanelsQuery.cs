using MediatR;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetMethodPanels;

internal record GetMethodPanelsQuery : IRequest<List<MethodPanelDto>>;

internal class GetPanelsQueryHandler(IParametersDbContext context) : IRequestHandler<GetMethodPanelsQuery, List<MethodPanelDto>>
{
    public async Task<List<MethodPanelDto>> Handle(GetMethodPanelsQuery request, CancellationToken cancellationToken)
    {
        var query = await context.Panels
            .OrderBy(x => x.PanelName)
            .Select(x => new MethodPanelDto
            {
                Id = x.Id,
                PanelName = x.PanelName,
                PanelLongName = x.PanelLongName,
                Grouped = x.GroupedPanel,
                Enabled = x.Enabled
            })
            .ToListAsync(cancellationToken);

        return query;
    }
}
