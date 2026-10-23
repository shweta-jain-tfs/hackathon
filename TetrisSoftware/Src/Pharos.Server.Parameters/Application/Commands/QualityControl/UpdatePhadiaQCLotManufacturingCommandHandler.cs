using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.QualityControl;

internal sealed class UpdatePhadiaQCLotManufacturingCommandHandler : IRequestHandler<UpdatePhadiaQCLotManufacturingCommand, int>
{
    private readonly IParametersDbContext _dbContext;

    public UpdatePhadiaQCLotManufacturingCommandHandler(IParametersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> Handle(UpdatePhadiaQCLotManufacturingCommand request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request), "UpdatePhadiaQCLotManufacturingCommand cannot be null.");
        }

        var qcLot = await _dbContext.MethodQualityControlLot.FirstOrDefaultAsync(qc => qc.MethodQualityControlId == request.Id && qc.LotNumber == request.LotNumber, cancellationToken);

        if (qcLot == null)
        {
            throw new KeyNotFoundException($"Quality control lot with ID {request.Id} not found.");
        }

        qcLot.ManufacturingRangeMin = request.ManufacturingRangeMin;
        qcLot.ManufacturingRangeMax = request.ManufacturingRangeMax;
        qcLot.ManufacturingExpectedConcentration = request.ManufacturingExpectedConcentration;
        qcLot.ManufacturingLimitCheck = request.LimitCheck;

        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
