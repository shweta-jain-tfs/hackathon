using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetQualityControls;

internal class IsTubeTypeUsedByAnyQualityControlQueryHandler(IParametersDbContext dbContext) : IRequestHandler<IsTubeTypeUsedByAnyQualityControlQuery, bool>
{
    /// <summary>
    /// Checks whether any quality control references the provided tube type.
    /// </summary>
    public async Task<bool> Handle(IsTubeTypeUsedByAnyQualityControlQuery request, CancellationToken cancellationToken)
    {
        var isUsedByLabQualityControl = await dbContext.LabQualityControls
            .AnyAsync(x => x.TubeTypeId == request.TubeTypeId, cancellationToken);

        if (isUsedByLabQualityControl)
        {
            return true;
        }

        return await dbContext.PhadiaQualityControl
            .AnyAsync(x => x.TubeTypeId == request.TubeTypeId, cancellationToken);
    }
}