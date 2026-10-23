using MediatR;
using Pharos.Server.Application.Request;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Commands;

internal sealed class RemoveTestPanelReferencesCommandHandler(IRequestDbContext dbContext) : IRequestHandler<RemoveTestPanelReferencesCommand>
{
    public async Task Handle(RemoveTestPanelReferencesCommand request, CancellationToken cancellationToken)
    {
        var panelIds = request.PanelIds.ToHashSet();

        if (panelIds.Count == 0)
        {
            return;
        }

        var testOrders = await dbContext.TestOrders
            .Where(x => x.TestPanelIds.Any(id => panelIds.Contains(id)))
            .ToListAsync(cancellationToken);

        foreach (var testOrder in testOrders)
        {
            testOrder.TestPanelIds = testOrder.TestPanelIds
                .Where(id => !panelIds.Contains(id))
                .ToList();
        }

        if (testOrders.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
