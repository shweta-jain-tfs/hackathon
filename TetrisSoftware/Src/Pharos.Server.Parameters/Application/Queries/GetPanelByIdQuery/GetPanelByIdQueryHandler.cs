using MediatR;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetPanelByIdQuery;

internal class GetPanelByIdQueryHandler : IRequestHandler<Server.Application.Parameters.GetPanelByIdQuery, Panel?>
{
    private readonly IParametersDbContext _dbContext;

    public GetPanelByIdQueryHandler(IParametersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Panel?> Handle(Server.Application.Parameters.GetPanelByIdQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Panels
                               .Where(x => x.Id == request.PanelId)
                               .Include(x => x.Tests)
                               .ThenInclude(t => t.MethodTest)
                               .ThenInclude(x => x.Method).FirstOrDefaultAsync(cancellationToken);
    }
}