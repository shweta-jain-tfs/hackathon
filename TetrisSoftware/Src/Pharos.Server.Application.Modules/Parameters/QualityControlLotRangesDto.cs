using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Application.Parameters;
public record QualityControlLotRangeDto
{
    public QualityControlType QualityControlType { get; init; }
    public double? ExpectedConcentration { get; init; }
    public double? ExpectedStdDev { get; init; }
    public double? StdDevLimit { get; init; }
    public double? RangeMin { get; init; }
    public double? RangeMax { get; init; }
    public bool ExcludeRangeMax { get; init; }
    public bool ExcludeRangeMin { get; init; }
}