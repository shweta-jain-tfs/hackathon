using MediatR;
using Pharos.Server.Application.Parameters;

namespace Pharos.Server.Parameters.Application.Queries.GetMethodNames;

internal record GetMethodNamesQuery : IRequest<List<string>>;

internal class GetMethodNamesQueryHandler(IMethodsService methodsService) : IRequestHandler<GetMethodNamesQuery, List<string>>
{
    public Task<List<string>> Handle(GetMethodNamesQuery request, CancellationToken cancellationToken)
    {
        return methodsService.GetMethods(x => true, x => x.MethodName);
    }
}