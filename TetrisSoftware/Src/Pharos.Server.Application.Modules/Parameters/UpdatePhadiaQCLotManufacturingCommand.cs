using MediatR;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Application.Parameters;

public record UpdatePhadiaQCLotManufacturingCommand : IRequest<int>
{
    public required int Id { get; set; }
    public required string LotNumber { get; set; }
    public double? ManufacturingRangeMin { get; set; }
    public double? ManufacturingRangeMax { get; set; }
    public double? ManufacturingExpectedConcentration { get; set; }
    public ManufacturingLimitCheck LimitCheck { get; set; }
};
