namespace Pharos.Server.Parameters.Application.Queries.GetPackages;
public class PackageDto
{
    public int Id { get; set; }
    public string PackageBarcode { get; set; } = string.Empty;
    public string OrderingNumber { get; set; } = string.Empty;
    public string? PackageFullName { get; set; }
    public string IntendedMarket { get; set; } = default!;
}