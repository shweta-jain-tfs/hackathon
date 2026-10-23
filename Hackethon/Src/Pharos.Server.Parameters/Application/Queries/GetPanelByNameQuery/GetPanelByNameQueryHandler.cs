using MediatR;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetPanelByNameQuery;

internal class GetPanelByNameQueryHandler : IRequestHandler<Server.Application.Parameters.GetPanelByNameQuery, Panel?>
{
    private readonly IParametersDbContext _dbContext;

    public GetPanelByNameQueryHandler(IParametersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Panel?> Handle(Server.Application.Parameters.GetPanelByNameQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Panels
                               .Where(x => x.PanelName == request.PanelName)
                               .Include(x => x.Tests)
                               .ThenInclude(t => t.MethodTest)
                               .ThenInclude(x => x.Method)
                               .FirstOrDefaultAsync(cancellationToken);
    }
}