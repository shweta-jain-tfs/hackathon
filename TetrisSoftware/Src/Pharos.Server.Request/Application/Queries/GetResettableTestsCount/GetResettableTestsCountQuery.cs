using MediatR;
using Pharos.Server.Request.Application.Helpers;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Queries.GetResettableTestsCount;

public record GetResettableTestsCountQuery(int[] RequestIds, bool IncludeAbortedTests) : IRequest<int>;

internal class GetResettableTestsCountQueryHandler(IRequestDbContext dbContext)
    : IRequestHandler<GetResettableTestsCountQuery, int>
{
    public async Task<int> Handle(GetResettableTestsCountQuery request, CancellationToken cancellationToken)
    {
        var requests = await dbContext.Requests
            .Where(x => request.RequestIds.Contains(x.Id))
            .Include(x => x.TestOrders)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var testsCount = 0;

        foreach (var req in requests)
        {
            foreach (var testOrder in req.TestOrders)
            {
                if (TestOrderResetHelper.CanReset(testOrder.State, request.IncludeAbortedTests))
                {
                    testsCount++;
                }
            }
        }

        return testsCount;
    }
}
