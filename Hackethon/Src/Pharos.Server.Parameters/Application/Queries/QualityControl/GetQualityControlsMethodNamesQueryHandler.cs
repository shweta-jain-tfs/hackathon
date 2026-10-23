using MediatR;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.QualityControl;

public record GetQualityControlsMethodNamesQuery : IRequest<List<string>>;

internal class GetQualityControlsMethodNamesQueryHandler(IParametersDbContext context) : IRequestHandler<GetQualityControlsMethodNamesQuery, List<string>>
{
    public async Task<List<string>> Handle(GetQualityControlsMethodNamesQuery request, CancellationToken cancellationToken)
    {
        var qualityControlsMethodNames = await context.QualityControlTests
            .Include(x => x.Test)
            .ThenInclude(t => t.Method)
            .Where(x => x.Enabled && x.Test.Method.Enabled)
            .AsNoTracking()
            .Select(x => x.Test.Method.MethodName)
            .Distinct()
            .ToListAsync(cancellationToken);
        return qualityControlsMethodNames;
    }
}