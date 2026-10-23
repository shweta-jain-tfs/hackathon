using System.Drawing;
using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain;
using Pharos.Server.Localization;
using Pharos.Server.Reagents.Application.Interfaces;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Queries.Inventory;

public class GetCartridgesQuery : SortedPartialQuery<CartridgeListItemDto>, IRequest<PartialQueryResult<CartridgeListResultDto>>
{
    public string? SearchText { get; set; }
    public List<string>? SelectedInstruments { get; set; }
    public List<string>? SelectedStatuses { get; set; }
    public bool BypassRowLimit { get; set; }
}

internal class GetCartridgesQueryHandler(IInstrumentManager instrumentManager, IReagentBarcodeService reagentBarcodeService, IInstrumentReagentStorage instrumentReagentStorage)
    : IRequestHandler<GetCartridgesQuery, PartialQueryResult<CartridgeListResultDto>>
{
    public async Task<PartialQueryResult<CartridgeListResultDto>> Handle(GetCartridgesQuery request, CancellationToken cancellationToken)
    {
        var instruments = instrumentManager.GetInstruments();
        var cartridgeListItemDtos = new List<CartridgeListItemDto>();
        var freePositionsFront = 0;
        var freePositionsRear = 0;
        var totalPositionsFront = 0;
        var totalPositionsRear = 0;

        // Filter instruments based on SelectedInstruments, or use all if null/empty
        var filteredInstruments = request.SelectedInstruments != null && request.SelectedInstruments.Count > 0
            ? instruments.Where(i => request.SelectedInstruments.Contains(i.DisplayName ?? "")).ToList()
            : instruments;

        foreach (var instrument in filteredInstruments)
        {
            var cartridges = instrumentReagentStorage.GetCartridges(instrument.SerialNumber)
                .Where(x => x.Position.Area is EnumCartridgeArea.RearStorageArea or EnumCartridgeArea.FrontStorageArea);

            foreach (var cartridge in cartridges)
            {
                var (articleType, _) = await reagentBarcodeService.DetectArticleType(cartridge.ReagentBarcode).ConfigureAwait(false);
                var reagentInfo = await reagentBarcodeService.GetReagentInfo(articleType, cartridge.ReagentBarcode);
                if (reagentInfo != null)
                {
                    cartridgeListItemDtos.Add(new CartridgeListItemDto
                    {
                        Id = HashCode.Combine(cartridge.ReagentBarcode, cartridge.Position.PositionInternalStorage, instrument.SerialNumber),
                        Barcode = cartridge.ReagentBarcode,
                        Shape = reagentInfo.Article.Shape,
                        Colors = reagentInfo.Article.Colors.Select(c => c.HasValue ? ColorTranslator.ToHtml(c.Value) : null).ToList(),
                        Texts = reagentInfo.Article.Texts,
                        Method = GetMethod(reagentInfo),
                        Name = reagentInfo.Article.FriendlyName,
                        Type = reagentInfo.Article.ArticleType,
                        Quantity = GetQuantity(cartridge, reagentInfo),
                        Lot = reagentInfo.Lot.LotNumber,
                        Status = GetCartridgeStatus(cartridge),
                        ExpiryDate = GetExpiryDate(cartridge, reagentInfo),
                        ExpiryDateOpenCell = GetExpiryDateOpenCell(cartridge, reagentInfo),
                        Instrument = instrument.DisplayName ?? "",
                        InstrumentNumber = instrument.SerialNumber,
                        StorageArea = MapStorageAreaEnum(cartridge.Position.Area),
                        Position = cartridge.Position.PositionInternalStorage
                    });
                }
            }
        }

        // Apply filtering
        ApplyFiltering(request, ref cartridgeListItemDtos, ref freePositionsFront, ref totalPositionsFront, ref freePositionsRear, ref totalPositionsRear, filteredInstruments.Count);

        // Apply default or Property Sorting
        cartridgeListItemDtos = ApplySorting(cartridgeListItemDtos, request.SortByProperty, request.SortByAscending);

        // Pagination
        var totalCount = cartridgeListItemDtos.Count;
        var pagedCartridgesList = cartridgeListItemDtos
            .Skip(request.StartIndex)
            .Take(request.Count)
            .ToList();

        var cartridgeListResult = new CartridgeListResultDto
        {
            Id = HashCode.Combine(cartridgeListItemDtos.Count, totalCount),
            Items = pagedCartridgesList,
            FreePositionsFront = freePositionsFront,
            TotalPositionsFront = totalPositionsFront,
            FreePositionsRear = freePositionsRear,
            TotalPositionsRear = totalPositionsRear
        };

        // Return the result
        return new PartialQueryResult<CartridgeListResultDto>(new List<CartridgeListResultDto> { cartridgeListResult }, request.StartIndex, totalCount);
    }

    public static ReagentPositionStatus GetCartridgeStatus(Cartridge cartridge)
    {
        return cartridge.Error switch
        {
            EnumCartridgeError.UnreadableCartridgeBarcodeError => ReagentPositionStatus.UnreadableBarcode,
            EnumCartridgeError.UnknownCartridgeError => ReagentPositionStatus.UnknownCartridge,
            EnumCartridgeError.CartridgeStorageFullError => ReagentPositionStatus.CartridgeStorageFull,
            EnumCartridgeError.CartridgeGripError => ReagentPositionStatus.TransferNotPossible,
            EnumCartridgeError.CartridgeAspirationError => ReagentPositionStatus.AspirationError,
            EnumCartridgeError.CartridgeExpiredError => ReagentPositionStatus.Expired,
            EnumCartridgeError.CartridgeNoError => CheckRemainingStatus(cartridge),
            EnumCartridgeError.UnspecifiedCartridgeError => throw new ValidationException($"{nameof(Cartridge)}.{nameof(Cartridge.Error)} value is not set."),
            _ => throw new ValidationException($"{nameof(Cartridge)}.{nameof(Cartridge.Error)} value is unknown."),
        };
    }

    public static ReagentPositionStatus CheckRemainingStatus(Cartridge cartridge)
    {
        if (cartridge.UnloadPending)
        {
            return ReagentPositionStatus.Unloading;
        }

        if (cartridge.Cells.Any(c => c.Content.ReservedStartedQuantity > 0 || c.Content.ReservedScheduledQuantity > 0))
        {
            return ReagentPositionStatus.Reserved;
        }

        return cartridge.Cells.Sum(c => c.Content.RemainingQuantity) <= 0 ? ReagentPositionStatus.Empty : ReagentPositionStatus.Available;
    }

    public static DateTime? GetExpiryDate(Cartridge cartridge, ReagentInfo reagentInfo)
    {
        var earliestCellExpiryDate = cartridge.Cells
           .Where(cell => IsDateEmpty(cell.Content.OpenTimestamp))
           .Min(cell => cell.Content.ExpiryDate);

        var cellExpiryDate = earliestCellExpiryDate == null ? DateTime.MaxValue : earliestCellExpiryDate.ToDateTime();

        var reagentInfoExpiryDate = reagentInfo.Lot.ExpiryDate.DateTime;

        return cellExpiryDate < reagentInfoExpiryDate
            ? cellExpiryDate
            : reagentInfoExpiryDate;
    }

    public static ExpiryDateOpenCell GetExpiryDateOpenCell(Cartridge cartridge, ReagentInfo reagentInfo)
    {
        var expiryDateOpenCell = new ExpiryDateOpenCell();

        if (reagentInfo.Article.ArticleType == ArticleType.CalibratorCartridge
              || reagentInfo.Article.ArticleType == ArticleType.CurveControlCartridge)
        {
            expiryDateOpenCell.HasPiercedCell = false;
            return expiryDateOpenCell;
        }

        var piercedCells = cartridge.Cells
           .Where(cell => !IsDateEmpty(cell.Content.OpenTimestamp) && cell.Content.RemainingQuantity > 0)
           .ToList();

        if (piercedCells.Count > 0)
        {
            expiryDateOpenCell.HasPiercedCell = true;
            expiryDateOpenCell.ExpiryDate = piercedCells.Min(cell => cell.Content.ExpiryDate)?.ToDateTime();
            expiryDateOpenCell.Quantity = new QuantityDto
            {
                Value = piercedCells.Sum(cell => cell.Content.RemainingQuantity),
                Unit = reagentInfo.Article.UnitOfMeasure
            };
        }
        else
        {
            expiryDateOpenCell.HasPiercedCell = false;
        }

        return expiryDateOpenCell;
    }

    public static QuantityDto GetQuantity(Cartridge cartridge, ReagentInfo reagentInfo)
    {
        // Sum cell quantities for all cartridge types
        var quantity = new QuantityDto
        {
            Value = cartridge.Cells.Sum(cell => cell.Content.RemainingQuantity),
            Unit = reagentInfo.Article.UnitOfMeasure
        };
        return quantity;
    }

    public static string GetUnit(ReagentInfo reagentInfo)
    {
        return reagentInfo.Article.UnitOfMeasure.ToString();
    }

    public static CartridgeStorageArea MapStorageAreaEnum(EnumCartridgeArea storageArea)
    {
        return storageArea switch
        {
            EnumCartridgeArea.ReagentDrawerArea => CartridgeStorageArea.ReagentDrawer,
            EnumCartridgeArea.RearStorageArea => CartridgeStorageArea.BackStorage,
            EnumCartridgeArea.FrontStorageArea => CartridgeStorageArea.FrontStorage,
            EnumCartridgeArea.UnspecifiedCartridgeArea => throw new ValidationException("The cartridge storage area is not specified."),
            _ => throw new ArgumentOutOfRangeException(nameof(storageArea), storageArea, null)
        };
    }

    public static bool IsDateEmpty(Timestamp timestamp)
    {
        return timestamp is { Seconds: 0, Nanos: 0 } || timestamp.ToDateTime() == DateTime.MinValue;
    }

    public static string GetMethod(ReagentInfo reagentInfo)
    {
        var texts = reagentInfo.Article.Texts;

        return texts is { Count: > 0 }
            ? string.Join("/", texts.Where(text => !string.IsNullOrEmpty(text)))
            : string.Empty;
    }

    private static void ApplyFiltering(GetCartridgesQuery request, ref List<CartridgeListItemDto> cartridgeListItemDtos,
        ref int freePositionsFront, ref int totalPositionsFront,
        ref int freePositionsRear, ref int totalPositionsRear, int? totalInstrumentCount)
    {
        if (request.SelectedInstruments != null && request.SelectedInstruments.Count > 0)
        {
            // computes how many cartridge positions are available(free) for the selected instruments
            totalPositionsFront = request.SelectedInstruments.Count * Constants.MaxCartridgeStoragePositionsFront;
            freePositionsFront = totalPositionsFront - GetFrontStorageCount(cartridgeListItemDtos);
            totalPositionsRear = request.SelectedInstruments.Count * Constants.MaxCartridgeStoragePositionsRear;
            freePositionsRear = totalPositionsRear - GetRearStorageCount(cartridgeListItemDtos);
        }
        else
        {
            // computes how many cartridge positions are available(free) for all instruments
            totalPositionsFront = totalInstrumentCount.GetValueOrDefault() * Constants.MaxCartridgeStoragePositionsFront;
            freePositionsFront = totalPositionsFront - GetFrontStorageCount(cartridgeListItemDtos);
            totalPositionsRear = totalInstrumentCount.GetValueOrDefault() * Constants.MaxCartridgeStoragePositionsRear;
            freePositionsRear = totalPositionsRear - GetRearStorageCount(cartridgeListItemDtos);
        }

        if (request.SearchText != null && request.SearchText.Length > 0)
        {
            cartridgeListItemDtos = cartridgeListItemDtos
                .Where(c =>
                (!string.IsNullOrEmpty(c.Method) && c.Method.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(c.Name) && c.Name.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(c.Lot) && c.Lot.Trim().Equals(request.SearchText.Trim(), StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        if (request.SelectedStatuses != null && request.SelectedStatuses.Count > 0)
        {
            cartridgeListItemDtos = cartridgeListItemDtos
                .Where(c => ApplyStatusFilter(request, c))
                .ToList();
        }
    }

    private static bool ApplyStatusFilter(GetCartridgesQuery request, CartridgeListItemDto c)
    {
        if (request.SelectedStatuses == null)
        {
            return false;
        }
        if (request.SelectedStatuses.Contains(ReagentPositionStatus.Reserved.ToString()) && c.Status == ReagentPositionStatus.Reserved)
        {
            return true;
        }

        if (request.SelectedStatuses.Contains(ReagentPositionStatus.Unloading.ToString()) && c.Status == ReagentPositionStatus.Unloading)
        {
            return true;
        }

        if (request.SelectedStatuses.Contains(ReagentPositionStatus.Empty.ToString()) && c.Status == ReagentPositionStatus.Empty)
        {
            return true;
        }

        if (request.SelectedStatuses.Contains(Translations.Inventory_ErrorFilter) &&
            (c.Status == ReagentPositionStatus.TransferNotPossible ||
             c.Status == ReagentPositionStatus.UnreadableBarcode ||
             c.Status == ReagentPositionStatus.AspirationError ||
             c.Status == ReagentPositionStatus.Expired))
        {
            return true;
        }

        return false;
    }

    private static List<CartridgeListItemDto> ApplySorting(List<CartridgeListItemDto> items, string? sortProperty, bool ascending)
    {
        if (string.IsNullOrEmpty(sortProperty))
        {
            //default sort by multiple properties
            return items
                .OrderBy(x => x.Method)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Instrument)
                .ThenBy(x => x.StorageArea)
                .ThenBy(x => x.Position)
                .ToList();
        }

        switch (sortProperty)
        {
            case nameof(CartridgeListItemDto.Method):
                return SortBy(items, x => x.Method, ascending);
            case nameof(CartridgeListItemDto.Name):
                return SortBy(items, x => x.Name, ascending);
            case nameof(CartridgeListItemDto.Lot):
                return SortBy(items, x => x.Lot, ascending);
            case nameof(CartridgeListItemDto.Status):
                return SortBy(items, x => x.Status, ascending);
            case nameof(CartridgeListItemDto.ExpiryDate):
                return SortBy(items, x => x.ExpiryDate, ascending);
            case nameof(CartridgeListItemDto.Instrument):
                return SortBy(items, x => x.Instrument, ascending);
            case nameof(CartridgeListItemDto.Position):
                return SortByPosition(items, ascending);
            case nameof(CartridgeListItemDto.Quantity):
                return SortByQuantity(items, ascending);
            case nameof(CartridgeListItemDto.ExpiryDateOpenCell):
                return SortByExpiryDateOpenCell(items, ascending);
            default:
                return items;
        }
    }

    private static List<CartridgeListItemDto> SortBy<T>(List<CartridgeListItemDto> items, Func<CartridgeListItemDto, T> keySelector, bool ascending)
    {
        return ascending
            ? items.OrderBy(keySelector).ToList()
            : items.OrderByDescending(keySelector).ToList();
    }

    private static List<CartridgeListItemDto> SortByPosition(List<CartridgeListItemDto> items, bool ascending)
    {
        return ascending
            ? items.OrderBy(x => x.StorageArea).ThenBy(x => x.Position).ToList()
            : items.OrderByDescending(x => x.StorageArea).ThenByDescending(x => x.Position).ToList();
    }

    private static List<CartridgeListItemDto> SortByQuantity(List<CartridgeListItemDto> items, bool ascending)
    {
        return ascending
            ? items.OrderBy(x => x.Quantity?.Value).ThenBy(x => x.Quantity?.Unit).ToList()
            : items.OrderByDescending(x => x.Quantity?.Value).ThenByDescending(x => x.Quantity?.Unit).ToList();
    }

    private static List<CartridgeListItemDto> SortByExpiryDateOpenCell(List<CartridgeListItemDto> items, bool ascending)
    {
        return ascending
            ? items.OrderBy(x => x.ExpiryDateOpenCell?.ExpiryDate)
                .ThenBy(x => x.ExpiryDateOpenCell.Quantity?.Value)
                .ThenBy(x => x.ExpiryDateOpenCell.Quantity?.Unit).ToList()
            : items.OrderByDescending(x => x.ExpiryDateOpenCell.ExpiryDate)
                .ThenByDescending(x => x.ExpiryDateOpenCell.Quantity?.Value)
                .ThenByDescending(x => x.ExpiryDateOpenCell.Quantity?.Unit).ToList();
    }

    private static int GetFrontStorageCount(List<CartridgeListItemDto> cartridges)
    {
        return cartridges.Count(c => c.StorageArea == CartridgeStorageArea.FrontStorage);
    }

    private static int GetRearStorageCount(List<CartridgeListItemDto> cartridges)
    {
        return cartridges.Count(c => c.StorageArea == CartridgeStorageArea.BackStorage);
    }
}

