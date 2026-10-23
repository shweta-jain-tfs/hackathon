using Pharos.Server.Reflex.Application.Events;
using Pharos.Server.Reflex.Interfaces;

namespace Pharos.Server.Reflex.Application.Commands.DeleteReflexDefinitionCommand;

internal record DeleteReflexDefinitionCommand(IList<int> Ids) : IRequest;

internal sealed class DeleteReflexDefinitionCommandHandler(IReflexDbContext dbContext) : IRequestHandler<DeleteReflexDefinitionCommand>
{
    public async Task Handle(DeleteReflexDefinitionCommand reflex, CancellationToken cancellationToken)
    {
        var reflexDefinitions = await dbContext.ReflexDefinitions
            .Where(r => reflex.Ids.Contains(r.Id))
            .Include(t => t.ReflexTests)
            .Include(c => c.ReflexCondition)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (reflexDefinitions.Count == 0)
        {
            // If no reflex definitions are found, we can consider the delete operation successful (idempotent).
            return;
        }

        foreach (var reflexEntity in reflexDefinitions)
        {
            reflexEntity.AddDomainEvent(new ReflexDefinitionDeletedEvent(reflexEntity));
            dbContext.ReflexDefinitions.Remove(reflexEntity);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}