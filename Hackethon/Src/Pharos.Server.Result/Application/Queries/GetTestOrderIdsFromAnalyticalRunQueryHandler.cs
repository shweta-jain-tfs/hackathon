using MediatR;
using Pharos.Server.Application.Result;
using Pharos.Server.Result.Application.Interfaces;

namespace Pharos.Server.Result.Application.Queries;
internal class GetTestOrderIdsFromAnalyticalRunQueryHandler(IResultDbContext dbContext) : IRequestHandler<GetTestOrderIdsFromAnalyticalRunQuery, IReadOnlyList<Guid>>
{
    public async Task<IReadOnlyList<Guid>> Handle(GetTestOrderIdsFromAnalyticalRunQuery request, CancellationToken cancellationToken)
    {
        var ids = await dbContext.SampleTestResults
            .Where(r => r.AnalyticalRunId == request.AnalyticalRunId)
            .Select(r => r.TestOrderId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return ids;
    }
}