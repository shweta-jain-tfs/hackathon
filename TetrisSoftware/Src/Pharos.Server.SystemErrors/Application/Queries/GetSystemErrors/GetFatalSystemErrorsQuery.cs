using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Domain.Enums;
using Pharos.Server.SystemErrors.Application.Interfaces;

namespace Pharos.Server.SystemErrors.Application.Queries.GetSystemErrors;

internal record GetFatalSystemErrorsQuery(string SerialNumber) : IRequest<List<int>>;


internal class GetFatalSystemErrorsQueryHandler : IRequestHandler<GetFatalSystemErrorsQuery, List<int>>
{
    private readonly ISystemErrorsDbContext _dbContext;

    public GetFatalSystemErrorsQueryHandler(ISystemErrorsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<int>> Handle(GetFatalSystemErrorsQuery request, CancellationToken cancellationToken)
    {
        var result = await _dbContext.SystemErrors.Where(x => x.Classification == ErrorClassification.Fatal &&
                                                       x.SerialNumber == request.SerialNumber &&
                                                       !x.Response.HasValue &&
                                                       !x.IsMuted)
                                .Select(x => x.Id).ToListAsync(cancellationToken);

        return result;
    }
}