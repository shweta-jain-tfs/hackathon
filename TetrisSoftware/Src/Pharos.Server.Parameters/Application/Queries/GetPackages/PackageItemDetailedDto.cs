using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Parameters.Application.Queries.GetPackages;
public class PackageItemDetailedDto
{
    public int Id { get; set; }
    public int ArticleAmount { get; set; }
    public string ArticleNumber { get; set; } = default!;
    public ArticleType ArticleType { get; set; }
    public string? FullName { get; set; }
    public string? Identity { get; set; }
    public string? MethodName { get; set; }
    public int Quantity { get; set; }
    public ArticleQuantityUnit UnitOfMeasure { get; set; }
}
