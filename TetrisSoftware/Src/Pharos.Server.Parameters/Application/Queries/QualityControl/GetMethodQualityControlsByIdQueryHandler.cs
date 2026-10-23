using MediatR;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Application.Sample;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.QualityControl;

public record GetMethodQualityControlsByIdQuery(int QualityControlId) : IRequest<QualityControlDto>;

internal class GetMethodQualityControlsByIdQueryHandler(IParametersDbContext context, ISender sender) : IRequestHandler<GetMethodQualityControlsByIdQuery, QualityControlDto>
{
    public async Task<QualityControlDto> Handle(GetMethodQualityControlsByIdQuery request, CancellationToken cancellationToken)
    {
        var qc = await context.QualityControlTests
            .Where(qc => qc.Id == request.QualityControlId && qc.Enabled)
            .Select(qc => new QualityControlDto
            {
                Id = qc.Id,
                IsUserDefined = qc.SystemType == QualityControlSystemType.Lab,
                Name = qc.QualityControl.Name,
                MethodName = qc.Test.Method.MethodName,
                TestName = qc.Test.TestName,
                IsQcInTube = qc.SystemType == QualityControlSystemType.Lab || ((PhadiaQualityControl)qc.QualityControl).IsQcInTube,
                
                Lots = qc.Lots.OrderByDescending(lot => lot.Created).Select(lot => new QualityControlLotDto
                {
                    Id = lot.Id,
                    LotType = lot.LotType,
                    DefaultUsesRange = lot.LotType == LotType.UseDefaultQcValues && (qc.ApplyAcceptanceRange && qc.UseAcceptanceRange),
                    DefaultUsesTarget = lot.LotType == LotType.UseDefaultQcValues && (qc.ApplyAcceptanceRange && !qc.UseAcceptanceRange),
                    LotNumber = lot.LotNumber,
                    RegistrationDate = lot.Created,
                    ExpirationDate = lot.ExpirationDate,
                    RangeMin = lot.RangeMin,
                    RangeMax = lot.RangeMax,
                    ManufacturingRangeMax = lot.ManufacturingRangeMax,
                    ManufacturingRangeMin = lot.ManufacturingRangeMin,
                    ExpectedConcentration = lot.ExpectedConcentration,
                    ExpectedStdDev = lot.ExpectedStdDev,
                    StdDevLimit = lot.StdDevLimit,
                    Issue = string.Empty
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (qc == null)
        {
            throw new KeyNotFoundException($"Quality control with ID {request.QualityControlId} not found.");
        }

        if (qc.IsQcInTube)
        {
            qc.Tubes = await FindTubes(qc.Name, cancellationToken);
        }
        else
        {
            qc.Cartridges = await FindCartridges(qc.Name, cancellationToken);
        }

        return qc;
    }

    private async Task<List<UnloadableQualityControlTubeDto>> FindTubes(string qcName, CancellationToken cancellationToken)
    {
       return await sender.Send(new GetUnloadableQualityControlTubesQuery(qcName), cancellationToken);
    }

    private async Task<List<UnloadableQualityControlCartridgeDto>> FindCartridges(string qcName, CancellationToken cancellationToken)
    {
        return await sender.Send(new GetUnloadableQualityControlCartridgesQuery(qcName), cancellationToken);
    }
}