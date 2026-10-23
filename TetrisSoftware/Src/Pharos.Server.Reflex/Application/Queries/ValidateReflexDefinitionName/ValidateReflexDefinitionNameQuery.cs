using Pharos.Server.Reflex.Interfaces;

namespace Pharos.Server.Reflex.Application.Queries.ValidateReflexDefinitionName;

public record ValidateReflexDefinitionNameQuery(string Name) : IRequest<bool>;

internal sealed class ValidateReflexDefinitionNameQueryHandler : IRequestHandler<ValidateReflexDefinitionNameQuery, bool>
{
    private readonly IReflexDbContext _dbContext;

    public ValidateReflexDefinitionNameQueryHandler(IReflexDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(ValidateReflexDefinitionNameQuery request, CancellationToken cancellationToken)
    {
        var existInDb = await _dbContext.ReflexDefinitions
            .AnyAsync(r => r.Name == request.Name, cancellationToken);

        // Returns true if name is unique (does NOT exist in database).
        return !existInDb;
    }
}
