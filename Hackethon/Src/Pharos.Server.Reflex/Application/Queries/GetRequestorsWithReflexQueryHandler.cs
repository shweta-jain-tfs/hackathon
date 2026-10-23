using Pharos.Server.Application.Reflex;
using Pharos.Server.Reflex.Interfaces;

namespace Pharos.Server.Reflex.Application.Queries;

internal class GetRequestorsWithReflexQueryHandler(IReflexDbContext context) : IRequestHandler<GetRequestorsWithReflexQuery, List<string>>
{
    public async Task<List<string>> Handle(GetRequestorsWithReflexQuery request, CancellationToken cancellationToken)
    {
        var requestorIdsWithReflex = await context.ReflexDefinitions
            .Where(r => r.RequestorId != null && request.RequestorIds.Contains(r.RequestorId))
            .Select(r => r.RequestorId!)
            .Distinct()
            .ToListAsync(cancellationToken);
        return requestorIdsWithReflex;
    }
}