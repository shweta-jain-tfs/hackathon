using MediatR;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.QualityControl;

public record DeleteMethodQualityControlLotsCommand(List<int> LotIds) : IRequest<bool>;

internal sealed class DeleteMethodQualityControlLotsCommandHandler : IRequestHandler<DeleteMethodQualityControlLotsCommand, bool>
{
    private readonly IParametersDbContext _dbContext;

    public DeleteMethodQualityControlLotsCommandHandler(IParametersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteMethodQualityControlLotsCommand request, CancellationToken cancellationToken)
    {
        foreach (var lotId in request.LotIds)
        {
            // Use FirstOrDefaultAsync instead of FirstOrDefault for async query
            var lot = await _dbContext.MethodQualityControlLot.FirstOrDefaultAsync(x => x.Id == lotId, cancellationToken);
            if (lot != null)
            {
                _dbContext.MethodQualityControlLot.Remove(lot);
            }
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}