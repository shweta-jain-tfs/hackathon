using MediatR;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetMethodCurveControls;

public record GetMethodCurveControlNamesQuery(string MethodName) : IRequest<List<string>>;

internal class GetMethodCurveControlNamesQueryHandler(IParametersDbContext context) : IRequestHandler<GetMethodCurveControlNamesQuery, List<string>>
{
    public async Task<List<string>> Handle(GetMethodCurveControlNamesQuery request, CancellationToken cancellationToken)
    {
        var result = await context.MethodCurveControls
            .Where(x => x.Method.MethodName == request.MethodName)
            .Select(x => x.CurveControlName).ToListAsync(cancellationToken);

        return result;
    }
}