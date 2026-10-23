using MediatR;
using Pharos.Server.Application.Request;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Queries;

internal class DoesAnyTestOrderExistQueryHandler(IRequestDbContext dbContext) : IRequestHandler<DoesAnyTestOrderExistQuery, bool>
{
    public async Task<bool> Handle(DoesAnyTestOrderExistQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.TestOrders.AnyAsync(x => request.TestOrderIds.Contains(x.Uid), cancellationToken);
    }
}