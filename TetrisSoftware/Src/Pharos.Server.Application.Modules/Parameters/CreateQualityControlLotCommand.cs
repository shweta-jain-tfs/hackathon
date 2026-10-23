using MediatR;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Application.Parameters;

public record CreatePhadiaQualityControlLotCommand : IRequest<int>
{
    public required int QualityControlId { get; set; }
    public required LotType LotType { get; set; }
    public string? LotNumber { get; set; }
    public double? RangeMin { get; set; }
    public double? RangeMax { get; set; }
    public double? ExpectedConcentration { get; set; }
    public double? ExpectedStdDev { get; set; }
    public double? StdDevLimit { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
}

public record CreateLabQualityControlLotCommand : IRequest<int>
{
    public required int QualityControlId { get; set; }
    public required LotType LotType { get; set; }
    public string? LotNumber { get; set; }
    public double? RangeMin { get; set; }
    public double? RangeMax { get; set; }
    public double? ExpectedConcentration { get; set; }
    public double? ExpectedStdDev { get; set; }
    public double? StdDevLimit { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
}