using MediatR;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.MethodQualityControls;

internal record DeleteLabQualityControlsCommand(HashSet<int> Ids) : IRequest;

internal class DeleteLabQualityControlsCommandHandler : IRequestHandler<DeleteLabQualityControlsCommand>
{
    private readonly IParametersDbContext _context;

    public DeleteLabQualityControlsCommandHandler(IParametersDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteLabQualityControlsCommand request, CancellationToken cancellationToken)
    {
        await _context.LabQualityControls
            .Where(x => request.Ids.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }
}