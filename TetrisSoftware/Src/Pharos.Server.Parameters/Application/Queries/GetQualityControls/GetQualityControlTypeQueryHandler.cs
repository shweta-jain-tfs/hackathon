using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetQualityControls;

internal class GetQualityControlTypeQueryHandler : IRequestHandler<GetQualityControlTypeQuery, QualityControlTypeDto>
{
    private readonly IParametersDbContext _dbContext;

    public GetQualityControlTypeQueryHandler(IParametersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QualityControlTypeDto> Handle(GetQualityControlTypeQuery request, CancellationToken cancellationToken)
    {
        var phadiaQc = await _dbContext.PhadiaQualityControl
            .FirstOrDefaultAsync(x => x.Name == request.QualityControlName, cancellationToken);

        if(phadiaQc != null)
        {
            return QualityControlTypeDto.Phadia(!phadiaQc.IsQcInTube);
        }

        var isLabQc = await _dbContext.LabQualityControls
            .AnyAsync(x => x.Name == request.QualityControlName, cancellationToken);

        if (isLabQc)
        {
            return QualityControlTypeDto.Lab();
        }

        return QualityControlTypeDto.DoNotExist();
    }
}