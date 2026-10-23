using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Reagents.Application.Queries.Inventory;
public class CartridgeListItemDto
{
    public int Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public ArticleIndicatorShape? Shape { get; set; }
    public ArticleType Type { get; set; }
    public List<string?> Colors { get; set; } = default!;
    public List<string?> Texts { get; set; } = default!;
    public string Method { get; set; } = string.Empty;
    public string? Name { get; set; }
    public QuantityDto Quantity { get; set; } = default!;
    public string Lot { get; set; } = string.Empty;
    public ReagentPositionStatus Status { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public ExpiryDateOpenCell ExpiryDateOpenCell { get; set; } = default!;
    public string Instrument { get; set; } = string.Empty;
    public CartridgeStorageArea StorageArea { get; set; }
    public int Position { get; set; }
    public string InstrumentNumber { get; set; } = string.Empty;
}

public class CartridgeListResultDto
{
    public int Id { get; set; }
    public List<CartridgeListItemDto> Items { get; set; } = new();
    public int FreePositionsFront { get; set; }
    public int TotalPositionsFront { get; set; }
    public int FreePositionsRear { get; set; }
    public int TotalPositionsRear { get; set; }
    public int TotalQuantity => Items.Sum(x => x.Quantity?.Value ?? 0);
}

public class ExpiryDateOpenCell
{
    public bool HasPiercedCell { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public QuantityDto? Quantity { get; set; }
}