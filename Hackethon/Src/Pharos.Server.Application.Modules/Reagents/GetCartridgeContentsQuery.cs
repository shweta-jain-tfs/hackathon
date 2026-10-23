using MediatR;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Application.Reagents;

public record GetCartridgeContentsQuery(IReadOnlyList<string> CartridgeBarcodeList) : IRequest<IReadOnlyDictionary<string, ReagentContentDto>>;

public record ReagentContentDto
{
    public string ReagentBarcode { get; set; } = default!;
    public string? CarrierUdi { get; set; }
    public ArticleType ArticleType { get; set; }
    public string ArticleNumber { get; set; } = default!;
    public string LotNumber { get; set; } = default!;
    public DateTimeOffset LotExpiryDate { get; set; }
    public IList<ReagentContentCellDto> ContentCells { get; set; } = default!;
    public string? Name { get; set; }
    public string? SerialNumber { get; set; }
}

public class ReagentContentCellDto
{
    public int Position { get; init; }
    public int RemainingQuantity { get; init; }
    public bool UseRemainingQuantity { get; init; }
    public DateTimeOffset? OpenTimestamp { get; init; }
}