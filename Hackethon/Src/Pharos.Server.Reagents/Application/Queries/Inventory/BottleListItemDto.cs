using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Reagents.Application.Queries.Inventory;

public class BottleListItemDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public ArticleIndicatorShape? Shape { get; set; }
    public ArticleType? Type { get; set; }
    public List<string?> Colors { get; set; } = default!;
    public QuantityDto Quantity { get; set; } = default!;
    public string Lot { get; set; } = string.Empty;
    public ReagentPositionStatus Status { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Instrument { get; set; } = string.Empty;
    public int Position { get; set; }
    public string InstrumentNumber { get; set; } = string.Empty;
}

public class BottleListResultDto
{
    public int Id { get; set; }
    public List<BottleListItemDto> Items { get; set; } = new();
    public int FreePositions { get; set; }
    public int TotalPositions { get; set; }
}