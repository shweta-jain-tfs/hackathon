using MediatR;
using Pharos.Server.Application.Request;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Queries;

internal sealed class CheckTestPanelsReferencedQueryHandler(IRequestDbContext dbContext) : IRequestHandler<CheckTestPanelsReferencedQuery, bool>
{
    public async Task<bool> Handle(CheckTestPanelsReferencedQuery request, CancellationToken cancellationToken)
    {
        var panelIds = request.PanelIds.ToHashSet();

        if (panelIds.Count == 0)
        {
            return false;
        }

        return await dbContext.TestOrders
            .AnyAsync(x => x.TestPanelIds.Any(id => panelIds.Contains(id)), cancellationToken);
    }
}
