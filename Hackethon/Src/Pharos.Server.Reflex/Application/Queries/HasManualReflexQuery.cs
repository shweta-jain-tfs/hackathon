using Pharos.Server.Reflex.Interfaces;

namespace Pharos.Server.Reflex.Application.Queries;

/// <summary>
/// Query for if Pharos has any reflex definitions that has manual reflex adding.
/// </summary>
internal record HasManualReflexQuery() : IRequest<bool>;

internal sealed class HasManualReflexQueryHandler(IReflexDbContext context) : IRequestHandler<HasManualReflexQuery, bool>
{
    public async Task<bool> Handle(HasManualReflexQuery request, CancellationToken cancellationToken)
    {
        var reflexEntity = await context.ReflexDefinitions
            .FirstOrDefaultAsync(x => !x.Automatic, cancellationToken);

        return reflexEntity != null;
    }
}