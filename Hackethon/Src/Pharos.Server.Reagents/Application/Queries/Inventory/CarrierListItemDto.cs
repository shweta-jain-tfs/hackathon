using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Reagents.Application.Queries.Inventory;
public class CarrierListItemDto
{
    public int Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Udi { get; set; } = string.Empty;
    public ArticleIndicatorShape? Shape { get; set; }
    public List<string?> Colors { get; set; } = default!;
    public string? Test { get; set; } = string.Empty;
    public ArticleType Type { get; set; }
    public QuantityDto? Quantity { get; set; }
    public string Lot { get; set; } = string.Empty;
    public ReagentPositionStatus Status { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsRefillLevelDefined { get; set; }
    public string Instrument { get; set; } = string.Empty;
    public int Position { get; set; }
    public string InstrumentNumber { get; set; } = string.Empty;
}

public class CarrierListResultDto
{
    public int Id { get; set; }
    public List<CarrierListItemDto> Items { get; set; } = new();
    public int FreePositions { get; set; }
    public int TotalPositions { get; set; }
    public int TotalQuantity => Items.Sum(x => x.Quantity?.Value ?? 0);
}

public class UnloadReagentRequestItemDto
{
    public string InstrumentNumber { get; set; } = string.Empty;
    public int Position { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public bool AbortUnload { get; set; }
    public string? Udi { get; set; }
    public ReagentStorageArea StorageArea { get; set; }
}

public enum ReagentStorageArea
{
    InternalCarrierStorageArea = 1,
    RearCartridgeStorageArea = 2,
    FrontCartridgeStorageArea = 3,
    DilutionStationArea = 5
}
