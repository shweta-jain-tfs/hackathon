using MediatR;
using Pharos.Server.Parameters.Application.Services;

namespace Pharos.Server.Parameters.Application.Queries.GetParameterExport;

public record GetParameterExportToClientQuery(int[] MethodIds, bool ExportPackage) : IRequest<Stream>;

internal sealed class GetParameterExportToClientQueryHandler(IExportParameterService exportParameterService) : IRequestHandler<GetParameterExportToClientQuery, Stream>
{
    public async Task<Stream> Handle(GetParameterExportToClientQuery request, CancellationToken cancellationToken)
    {
        var file =  await exportParameterService.ExportParametersAsZip(request.MethodIds, request.ExportPackage, cancellationToken);
        return file; 
    }
}
