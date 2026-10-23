using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.MethodQualityControls;

internal record UpdateLabQualityControlEnabledCommand(int Id, bool Enabled) : IRequest;

internal class UpdateLabQualityControlEnabledCommandHandler : IRequestHandler<UpdateLabQualityControlEnabledCommand>
{
    private readonly IParametersDbContext _context;

    public UpdateLabQualityControlEnabledCommandHandler(IParametersDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateLabQualityControlEnabledCommand request, CancellationToken cancellationToken)
    {
        var qcTest = await _context.LabQualityControlTests.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (qcTest == null)
        {
            throw new NotFoundException();
        }

        if(qcTest.Enabled != request.Enabled)
        {
            qcTest.Enabled = request.Enabled;

            qcTest.AddDomainEvent(new QcEnableDisableEvent());

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}