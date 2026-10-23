using MediatR;
using Pharos.Server.Application.Command;
using Pharos.Server.Application.Request;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.Panels;

public record DeletePanelsCommand(int[] Ids, bool RemoveTestPanelReferences) : IRequest<DeleteResponse>;

internal sealed class DeletePanelsCommandHandler(IParametersDbContext context, ISender sender) : IRequestHandler<DeletePanelsCommand, DeleteResponse>
{
    public async Task<DeleteResponse> Handle(DeletePanelsCommand request, CancellationToken cancellationToken)
    {
        var idsToDelete = request.Ids.ToHashSet();
        var panels = await context.Panels
            .Include(x => x.Tests)
            .Where(x => idsToDelete.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (panels.Count == 0)
        {
            return new DeleteResponse
            {
                Deleted = 0,
                Skipped = request.Ids.Length,
                SkippedIds = request.Ids.ToList()
            };
        }

        var deletedIds = panels.Select(x => x.Id).ToArray();
        var deletedIdLookup = deletedIds.ToHashSet();
        var skippedIds = request.Ids.Where(x => !deletedIdLookup.Contains(x)).ToList();

        foreach (var panelTest in panels.SelectMany(x => x.Tests).ToList())
        {
            context.PanelTests.Remove(panelTest);
        }

        foreach (var panel in panels)
        {
            context.Panels.Remove(panel);
        }

        await context.SaveChangesAsync(cancellationToken);

        if (request.RemoveTestPanelReferences)
        {
            await sender.Send(new RemoveTestPanelReferencesCommand(deletedIds), cancellationToken);
        }

        return new DeleteResponse
        {
            Deleted = panels.Count,
            Skipped = skippedIds.Count,
            SkippedIds = skippedIds
        };
    }
}
