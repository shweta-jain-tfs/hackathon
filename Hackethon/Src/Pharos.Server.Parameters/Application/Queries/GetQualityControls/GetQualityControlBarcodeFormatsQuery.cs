using MediatR;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetQualityControls;

public record GetQualityControlBarcodeFormatsQuery : IRequest<List<QualityControlBarcodeFormatDetailDto>>;

internal class GetQualityControlBarcodeFormatsQueryHandler : IRequestHandler<GetQualityControlBarcodeFormatsQuery, List<QualityControlBarcodeFormatDetailDto>>
{
    private readonly IParametersDbContext _dbContext;

    public GetQualityControlBarcodeFormatsQueryHandler(IParametersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<QualityControlBarcodeFormatDetailDto>> Handle(GetQualityControlBarcodeFormatsQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.QualityControlBarcodeFormats
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(bc => new QualityControlBarcodeFormatDetailDto
            {
                Id = bc.Id,
                Name = bc.Name,
                TotalLength = bc.TotalLength,
                QcStartPosition = bc.QcStartPosition,
                QcLength = bc.QcLength,
                ContainsLotInformation = bc.ContainsLotInformation,
                LotStartPosition = bc.LotStartPosition,
                LotLength = bc.LotLength
            })
            .ToListAsync(cancellationToken);
    }
}