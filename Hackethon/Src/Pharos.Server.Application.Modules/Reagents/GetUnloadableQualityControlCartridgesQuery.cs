using MediatR;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Application.Reagents;

public record GetUnloadableQualityControlCartridgesQuery(string QualityControlName) : IRequest<List<UnloadableQualityControlCartridgeDto>>;

public record UnloadableQualityControlCartridgeDto
{
    public Guid Id { get; init; }
    public int Doses { get; init; }
    public string Barcode { get; init; } = string.Empty;
    public string Lot { get; init; } = string.Empty;
    public ReagentPositionStatus Status { get; init; }
    public DateTimeOffset? ExpiryDate { get; init; }
    public DateTimeOffset? ExpiryDateOpenCell { get; init; }
    public string InstrumentName { get; init; } = string.Empty;
    public string InstrumentNumber { get; init; } = string.Empty;
    public int Position { get; init; }
    public CartridgeStorageArea Area { get; init; }
}