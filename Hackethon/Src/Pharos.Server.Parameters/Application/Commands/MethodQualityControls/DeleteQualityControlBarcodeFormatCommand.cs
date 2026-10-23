using MediatR;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Parameters.Application.Events;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.MethodQualityControls;

public record DeleteQualityControlBarcodeFormatCommand(int Id) : IRequest;

internal class DeleteQualityControlBarcodeFormatCommandHandler : IRequestHandler<DeleteQualityControlBarcodeFormatCommand>
{
    private readonly IParametersDbContext _context;

    public DeleteQualityControlBarcodeFormatCommandHandler(IParametersDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteQualityControlBarcodeFormatCommand request, CancellationToken cancellationToken)
    {
        var format = await _context.QualityControlBarcodeFormats
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        
        if (format == null)
        {
            throw new NotFoundException();
        }

        _context.QualityControlBarcodeFormats.Remove(format);
        format.AddDomainEvent(new QualityControlBarcodeFormatDeletedEvent(format));
        await _context.SaveChangesAsync(cancellationToken);
    }
}
