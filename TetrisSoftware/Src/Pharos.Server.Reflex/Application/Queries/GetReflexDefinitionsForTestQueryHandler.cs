using Microsoft.Extensions.Options;
using Pharos.Server.Application.Reflex;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Reflex.Interfaces;

namespace Pharos.Server.Reflex.Application.Queries;

internal class GetReflexDefinitionsForTestQueryHandler(
    IReflexDbContext dbContext,
    IOptionsSnapshot<RequestSettings> reflexOptions) : IRequestHandler<GetReflexDefinitionsForTestQuery, List<IReflexDefinition>>
{
    public async Task<List<IReflexDefinition>> Handle(GetReflexDefinitionsForTestQuery query, CancellationToken cancellationToken)
    {
        var settings = reflexOptions.Value;
        if (!settings.ReflexTestingEnabled)
        {
            return [];
        }

        var dbQuery = dbContext.ReflexDefinitions
            .Where(x => x.ReflexCondition.TestName == query.TestName
                        && x.ReflexCondition.MethodName == query.MethodName
                        && x.Active
                        && x.ReflexTests.Count > 0);

        var result = await dbQuery.Include(x => x.ReflexCondition)
            .Include(x => x.ReflexTests)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        //imagine that requestor is null if this setting is not enabled.
        //there might be code looking at this property further up the stack, so we will set it to null if the setting is not enabled.
        if (!settings.IsRequestorInformationEnabled)
        {
            result.ForEach(x => x.RequestorId = null);
        }

        return result.Select(x => (IReflexDefinition)x).ToList();
    }
}