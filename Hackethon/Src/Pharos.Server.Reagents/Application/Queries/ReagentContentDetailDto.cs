using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Reagents.Application.Queries;

public class ReagentContentDetailDto
{
    public List<string?> Colors { get; set; } = default!;
    public List<string?> Texts { get; set; } = default!;
    public ArticleIndicatorShape Shape { get; set; }
    public string? Test { get; set; }
    public string? Name { get; set; }
    public ArticleType? Type { get; set; }
    public int Quantity { get; set; }
    public ArticleQuantityUnit UnitOfMeasure { get; set; }
    public string? LotNumber { get; set; }
    public DateTimeOffset? ExpiryDate { get; set; }
    public ReagentSize Size { get; set; }
}

public enum ReagentSize
{
    Small,
    Large,
    NotApplicable
}
