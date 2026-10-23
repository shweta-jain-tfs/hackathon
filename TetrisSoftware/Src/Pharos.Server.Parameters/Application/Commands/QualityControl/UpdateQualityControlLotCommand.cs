using MediatR;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Localization;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Commands.QualityControl;

public record UpdateQualityControlLotCommand : IRequest<int>
{
    public int Id { get; set; }
    public required LotType LotType { get; set; }
    public double? RangeMin { get; set; }
    public double? RangeMax { get; set; }
    public double? ExpectedConcentration { get; set; }
    public double? ExpectedStdDev { get; set; }
    public double? StdDevLimit { get; set; }
};

internal sealed class UpdateQualityControlLotCommandHandler : IRequestHandler<UpdateQualityControlLotCommand, int>
{
    private readonly IParametersDbContext _dbContext;

    public UpdateQualityControlLotCommandHandler(IParametersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> Handle(UpdateQualityControlLotCommand request, CancellationToken cancellationToken)
    {
        var qcLot = await _dbContext.MethodQualityControlLot.FindAsync([request.Id], cancellationToken);
        if (qcLot == null)
        {
            throw new ObjectGoneException(Translations.ObjectGone);
        }
        qcLot.LotType = request.LotType;
        qcLot.RangeMin = request.RangeMin;
        qcLot.RangeMax = request.RangeMax;
        qcLot.ExpectedConcentration = request.ExpectedConcentration;
        qcLot.ExpectedStdDev = request.ExpectedStdDev;
        qcLot.StdDevLimit = request.StdDevLimit;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return qcLot.Id;
    }
}
