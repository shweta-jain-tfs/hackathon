using MediatR;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetVersion;

internal record GetParameterUpdatePackageQuery : IRequest<string?>;

internal class GetParameterUpdatePackageQueryHandler(IParametersDbContext context) : IRequestHandler<GetParameterUpdatePackageQuery, string?>
{
    public async Task<string?> Handle(GetParameterUpdatePackageQuery request, CancellationToken cancellationToken)
    {
        var result = await context.ParameterUpdatePackageVersion.SingleOrDefaultAsync(cancellationToken);
        return result?.VersionNumber.ToString() ?? "";
    }
}