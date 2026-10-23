using MediatR;
using Pharos.Server.Application.Security;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetMethods;

internal record GetMethodsQuery : IRequest<List<MethodDto>>;

internal sealed class GetMethodsQueryHandler(IParametersDbContext dbContext, IAuthorizationService authorizationService) : IRequestHandler<GetMethodsQuery, List<MethodDto>>
{
    public async Task<List<MethodDto>> Handle(GetMethodsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Methods
            .Where(x => !x.IsTemplate && ((!x.IsRuo && !x.IsSpecialMethod) || x.IsSpecializedMethodActivated));

        if (!authorizationService.RuoMode)
        {
            query = query.Where(x => !x.IsRuo);
        }

        return await query.OrderBy(x => x.SortOrder)
            .ThenBy(x => x.MethodName)
            .AsNoTracking()
            .Select(s => new MethodDto
            {
                Id = s.Id,
                MethodName = s.MethodName,
                FullMethodName = s.FullMethodName,
                Enabled = s.Enabled,
                MethodNameLis = s.MethodNameLis ?? s.MethodName
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
