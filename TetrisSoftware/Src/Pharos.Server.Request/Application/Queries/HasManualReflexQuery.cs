using MediatR;
using Pharos.Server.Application.Result;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Queries;

/// <summary>
/// Query for if Pharos has any reflex definitions that has manual reflex adding.
/// </summary>
internal record HasManualReflexQuery(int AnalyticalRunId) : IRequest<bool>;

internal sealed class HasManualReflexQueryHandler(IRequestDbContext context, ISender sender) : IRequestHandler<HasManualReflexQuery, bool>
{
    public async Task<bool> Handle(HasManualReflexQuery request, CancellationToken cancellationToken)
    {
        var testOrderIds = await sender.Send(new GetTestOrderIdsFromAnalyticalRunQuery(request.AnalyticalRunId), cancellationToken);

        return testOrderIds.Count != 0 && await context.ReflexDefinitions
            .Where(x => testOrderIds.Contains(x.TestOrderId) && !x.Automatic)
            .AnyAsync(cancellationToken);
    }
}