using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Localization;
using Pharos.Server.Reflex.Interfaces;

namespace Pharos.Server.Reflex.Application.Commands.SetActiveReflexDefinitionCommand;

internal record SetActiveReflexDefinitionCommand(int Id, bool Active) : IRequest;

internal sealed class SetActiveReflexDefinitionCommandHandler(IReflexDbContext dbContext) : IRequestHandler<SetActiveReflexDefinitionCommand>
{
    public async Task Handle(SetActiveReflexDefinitionCommand reflex, CancellationToken cancellationToken)
    {
        var reflexDefinition = await dbContext.ReflexDefinitions
            .FirstOrDefaultAsync(r => reflex.Id == r.Id, cancellationToken)
            .ConfigureAwait(false);

        if (reflexDefinition == null)
        {
            throw new ObjectGoneException(Translations.ObjectGone);
        }

        reflexDefinition.Active = reflex.Active;

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}