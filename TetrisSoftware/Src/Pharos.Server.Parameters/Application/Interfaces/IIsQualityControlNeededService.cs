using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Services;

namespace Pharos.Server.Parameters.Application.Interfaces;

internal interface IIsQualityControlNeededService
{
    Task<QualityControlNeededDto> IsQualityControlNeeded(HashSet<string> methodNames, string instrumentNumber, CancellationToken cancellationToken);
    Task<QualityControlNeededDto> IsQualityControlNeeded(LabQualityControl labQualityControl, string instrumentNumber, CancellationToken cancellationToken);
}