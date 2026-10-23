namespace Pharos.Server.Parameters.Application.Queries.GetPackages;

public class PackageDetailsDto : PackageDto
{
    public List<PackageItemDetailedDto> PackageItems { get; set; } = default!;
}