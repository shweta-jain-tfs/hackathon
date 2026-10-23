namespace Pharos.Server.Parameters.Application.Queries.QualityControl;

public class QualityControlLotDto
{
    public int Id { get; set; }
    public LotType LotType { get; set; }
    public bool DefaultUsesRange { get; set; }
    public bool DefaultUsesTarget { get; set; }
    public string? LotNumber { get; set; }
    public double? RangeMin { get; set; }
    public double? RangeMax { get; set; }
    public double? ManufacturingRangeMin { get; set; }
    public double? ManufacturingRangeMax { get; set; }
    public double? ExpectedConcentration { get; set; }
    public double? ExpectedStdDev { get; set; }
    public double? StdDevLimit { get; set; }
    public DateTimeOffset? RegistrationDate { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
    public string Issue { get; set; } = string.Empty;
}