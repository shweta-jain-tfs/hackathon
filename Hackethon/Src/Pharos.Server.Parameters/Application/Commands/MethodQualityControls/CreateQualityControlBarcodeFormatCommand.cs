using MediatR;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Parameters.Application.Events;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.MethodQualityControls;

public record CreateQualityControlBarcodeFormatCommand(
    string Name,
    int TotalLength,
    int QcStartPosition,
    int QcLength,
    bool ContainsLotInformation,
    int LotStartPosition,
    int LotLength) : IRequest;

internal class CreateQualityControlBarcodeFormatCommandHandler : IRequestHandler<CreateQualityControlBarcodeFormatCommand>
{
    private readonly IParametersDbContext _context;

    public CreateQualityControlBarcodeFormatCommandHandler(IParametersDbContext context)
    {
        _context = context;
    }

    public async Task Handle(CreateQualityControlBarcodeFormatCommand request, CancellationToken cancellationToken)
    {
        var format = new QualityControlBarcodeFormat
        {
            Name = request.Name,
            TotalLength = request.TotalLength,
            QcStartPosition = request.QcStartPosition,
            QcLength = request.QcLength,
            ContainsLotInformation = request.ContainsLotInformation
        };

        if (format.ContainsLotInformation)
        {
            format.LotStartPosition = request.LotStartPosition;
            format.LotLength = request.LotLength;
        }

        _context.QualityControlBarcodeFormats.Add(format);
        format.AddDomainEvent(new QualityControlBarcodeFormatCreatedEvent(format));
        await _context.SaveChangesAsync(cancellationToken);
    }
}