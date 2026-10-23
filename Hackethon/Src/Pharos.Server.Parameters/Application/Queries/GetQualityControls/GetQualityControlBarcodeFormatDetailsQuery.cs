using MediatR;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Queries.GetQualityControls;

internal record GetQualityControlBarcodeFormatDetailsQuery(int Id) : IRequest<QualityControlBarcodeFormatDetailDto>;

public class QualityControlBarcodeFormatDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalLength { get; set; }
    public int QcStartPosition { get; set; }
    public int QcLength { get; set; }
    public bool ContainsLotInformation { get; set; }
    public int LotStartPosition { get; set; }
    public int LotLength { get; set; }
}

internal class GetQualityControlBarcodeFormatDetailsQueryHandler : IRequestHandler<GetQualityControlBarcodeFormatDetailsQuery, QualityControlBarcodeFormatDetailDto>
{
    private readonly IParametersDbContext _dbContext;

    public GetQualityControlBarcodeFormatDetailsQueryHandler(IParametersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QualityControlBarcodeFormatDetailDto> Handle(GetQualityControlBarcodeFormatDetailsQuery request, CancellationToken cancellationToken)
    {
        var format = await _dbContext.QualityControlBarcodeFormats
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        
        if (format == null)
        {
            throw new KeyNotFoundException("Quality Control Barcode Format not found.");
        }

        return new QualityControlBarcodeFormatDetailDto
        {
            Id = format.Id,
            Name = format.Name,
            TotalLength = format.TotalLength,
            QcStartPosition = format.QcStartPosition,
            QcLength = format.QcLength,
            ContainsLotInformation = format.ContainsLotInformation,
            LotStartPosition = format.LotStartPosition,
            LotLength = format.LotLength
        };
    }
}
