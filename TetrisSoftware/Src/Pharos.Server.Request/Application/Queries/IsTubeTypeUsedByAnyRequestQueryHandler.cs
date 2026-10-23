using MediatR;
using Pharos.Server.Application.Request;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Queries;

internal class IsTubeTypeUsedByAnyRequestQueryHandler(IRequestDbContext dbContext) : IRequestHandler<IsTubeTypeUsedByAnyRequestQuery, bool>
{
    /// <summary>
    /// Checks whether any request references the provided tube type.
    /// </summary>
    public async Task<bool> Handle(IsTubeTypeUsedByAnyRequestQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Requests.AnyAsync(x => x.SampleTubeType == request.TubeTypeId, cancellationToken);
    }
}
