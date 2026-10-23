using MediatR;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Parameters.Application.Events;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.MethodQualityControls;

public record UpdateQualityControlBarcodeFormatCommand(
    int Id,
    string Name,
    int TotalLength,
    int QcStartPosition,
    int QcLength,
    bool ContainsLotInformation,
    int LotStartPosition,
    int LotLength) : IRequest;

internal class UpdateQualityControlBarcodeFormatCommandHandler : IRequestHandler<UpdateQualityControlBarcodeFormatCommand>
{
    private readonly IParametersDbContext _context;

    public UpdateQualityControlBarcodeFormatCommandHandler(IParametersDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateQualityControlBarcodeFormatCommand request, CancellationToken cancellationToken)
    {
        var format = await _context.QualityControlBarcodeFormats
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        
        if (format == null)
        {
            throw new NotFoundException();
        }

        format.Name = request.Name;
        format.TotalLength = request.TotalLength;
        format.QcStartPosition = request.QcStartPosition;
        format.QcLength = request.QcLength;
        format.ContainsLotInformation = request.ContainsLotInformation;

        if (format.ContainsLotInformation)
        {
            format.LotStartPosition = request.LotStartPosition;
            format.LotLength = request.LotLength;
        }

        format.AddDomainEvent(new QualityControlBarcodeFormatUpdatedEvent(format));
        await _context.SaveChangesAsync(cancellationToken);
    }
}