using MediatR;
using Pharos.Server.Application.Quality;

namespace Pharos.Server.Quality.Application.Queries;

public record GetQcCartridgeInfoQuery(string DataMatrix) : IRequest<QcCartridgeInformationDto?>;

internal class GetQcCartridgeInfoQueryHandler(IQcCartridgeInfoService qcDataMatrixService) : IRequestHandler<GetQcCartridgeInfoQuery, QcCartridgeInformationDto?>
{
    public Task<QcCartridgeInformationDto?> Handle(GetQcCartridgeInfoQuery request, CancellationToken cancellationToken)
    {
        return qcDataMatrixService.ParseQcDataMatrixAsync(request.DataMatrix, cancellationToken);
    }
}
