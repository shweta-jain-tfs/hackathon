using MediatR;
using Pharos.Server.Application.Request;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Queries;

internal class DoesTestOrderExistQueryHandler(IRequestDbContext dbContext) : IRequestHandler<DoesTestOrderExistQuery, bool>
{
    public async Task<bool> Handle(DoesTestOrderExistQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.TestOrders.AnyAsync(x => x.Uid == request.TestOrderId, cancellationToken);
    }
}