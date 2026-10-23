using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Localization;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.QualityControl;

internal sealed class CreateQualityControlLotCommandHandler : IRequestHandler<CreateLabQualityControlLotCommand, int>, IRequestHandler<CreatePhadiaQualityControlLotCommand, int>
{
    private readonly IParametersDbContext _dbContext;

    public CreateQualityControlLotCommandHandler(IParametersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> Handle(CreateLabQualityControlLotCommand request, CancellationToken cancellationToken)
    {
        var qc = await _dbContext.LabQualityControlTests.FindAsync([request.QualityControlId], cancellationToken);
        if (qc == null)
        {
            throw new ObjectGoneException(Translations.ObjectGone);
        }

        var lot = new MethodQualityControlLot
        {
            MethodQualityControlId = request.QualityControlId,
            LotType = request.LotType,
            LotNumber = request.LotNumber,
            RangeMin = request.RangeMin,
            RangeMax = request.RangeMax,
            ExpectedConcentration = request.ExpectedConcentration,
            ManufacturingLimitCheck = ManufacturingLimitCheck.None,
            ExpectedStdDev = request.ExpectedStdDev,
            StdDevLimit = request.StdDevLimit,
            ExpirationDate = request.ExpirationDate
        };

        _dbContext.MethodQualityControlLot.Add(lot);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return lot.Id;
    }

    public async Task<int> Handle(CreatePhadiaQualityControlLotCommand request, CancellationToken cancellationToken)
    {
        var qc = await _dbContext.PhadiaQualityControlTests.FindAsync([request.QualityControlId], cancellationToken);
        if (qc == null)
        {
            throw new ObjectGoneException(Translations.ObjectGone);
        }

        var lot = new MethodQualityControlLot
        {
            MethodQualityControlId = request.QualityControlId,
            LotType = request.LotType,
            LotNumber = request.LotNumber,
            RangeMin = request.RangeMin,
            RangeMax = request.RangeMax,
            ExpectedConcentration = request.ExpectedConcentration,
            ManufacturingLimitCheck = ManufacturingLimitCheck.None,
            ExpectedStdDev = request.ExpectedStdDev,
            StdDevLimit = request.StdDevLimit,
            ExpirationDate = request.ExpirationDate
        };

        _dbContext.MethodQualityControlLot.Add(lot);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return lot.Id;
    }
}
