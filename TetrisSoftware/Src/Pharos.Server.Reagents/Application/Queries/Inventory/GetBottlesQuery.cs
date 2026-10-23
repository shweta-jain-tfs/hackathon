using System.Drawing;
using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain;
using Pharos.Server.Localization;
using Pharos.Server.Reagents.Application.Interfaces;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Queries.Inventory;

public class GetBottlesQuery : SortedPartialQuery<BottleListItemDto>, IRequest<PartialQueryResult<BottleListResultDto>>
{
    public string? SearchText { get; set; }
    public List<string>? SelectedInstruments { get; set; }
    public List<string>? SelectedStatuses { get; set; }
}

internal class GetBottlesQueryHandler(IInstrumentManager instrumentManager,
        IReagentBarcodeService reagentBarcodeService,
        IInstrumentReagentStorage instrumentReagentStorage) : IRequestHandler<GetBottlesQuery, PartialQueryResult<BottleListResultDto>>
{
    public async Task<PartialQueryResult<BottleListResultDto>> Handle(GetBottlesQuery request, CancellationToken cancellationToken)
    {
        var instruments = instrumentManager.GetInstruments();
        var bottleListItemDtos = new List<BottleListItemDto>();

        var filteredInstruments = request.SelectedInstruments != null && request.SelectedInstruments.Count > 0
            ? instruments.Where(i => request.SelectedInstruments.Contains(i.DisplayName ?? "")).ToList()
            : instruments;

        foreach (var instrument in filteredInstruments)
        {
            var bottles = instrumentReagentStorage.GetBottles(instrument.SerialNumber);

            foreach (var bottle in bottles)
            {
                var (articleType, _) = await reagentBarcodeService.DetectArticleType(bottle.ReagentUdiBarcode).ConfigureAwait(false);
                var reagentInfo = await reagentBarcodeService.GetReagentInfo(articleType, null, bottle.ReagentUdiBarcode);

                if (reagentInfo != null)
                {
                    bottleListItemDtos.Add(new BottleListItemDto
                    {
                        Id = HashCode.Combine(bottle.ReagentUdiBarcode, bottle.ReagentType, instrument.SerialNumber, bottle.Position),
                        Shape = reagentInfo.Article.Shape,
                        Colors = reagentInfo.Article.Colors.Select(c => c.HasValue ? ColorTranslator.ToHtml(c.Value) : null).ToList(),
                        Name = reagentInfo.Article.FriendlyName,
                        Type = reagentInfo.Article.ArticleType,
                        Quantity = new QuantityDto
                        {
                            Value = bottle.Content.RemainingQuantity,
                            Unit = reagentInfo.Article.UnitOfMeasure
                        },
                        Lot = reagentInfo.Lot.LotNumber,
                        ExpiryDate = GetExpiryDate(bottle, reagentInfo),
                        Status = GetStatus(bottle),
                        Instrument = instrument.DisplayName ?? "",
                        InstrumentNumber = instrument.SerialNumber,
                        Position = bottle.Position
                    });
                }
            }
        }

        // Apply filtering
        ApplyFiltering(request, ref bottleListItemDtos, filteredInstruments.Count, out var freePositions, out var totalPositions);

        // Apply sorting
        bottleListItemDtos = ApplySorting(bottleListItemDtos, request.SortByProperty, request.SortByAscending);

        // Pagination
        var totalCount = bottleListItemDtos.Count;
        var pagedBottlesList = bottleListItemDtos
            .Skip(request.StartIndex)
            .Take(request.Count)
            .ToList();

        var bottleListResult = new BottleListResultDto
        {
            //Id is placeholder to make UI grid work
            Id = HashCode.Combine(bottleListItemDtos.Count, totalCount),
            Items = pagedBottlesList,
            FreePositions = freePositions,
            TotalPositions = totalPositions,
        };

        return new PartialQueryResult<BottleListResultDto>(
            new List<BottleListResultDto> { bottleListResult },
            request.StartIndex, totalCount);
    }

    private static void ApplyFiltering(GetBottlesQuery request, ref List<BottleListItemDto> bottleListItemDtos, int? totalInstrumentCount, out int freePositions, out int totalPositions)
    {
        if (request.SelectedInstruments is { Count: > 0 })
        {
            // computes how many carrier positions are available(free) for the selected instruments
            totalPositions = request.SelectedInstruments.Count * Constants.MaxBottlesStoragePosition;
            freePositions = totalPositions - bottleListItemDtos.Count;
        }
        else
        {
            // computes how many carrier positions are available(free) for all instruments
            totalPositions = totalInstrumentCount.GetValueOrDefault() * Constants.MaxBottlesStoragePosition;
            freePositions = totalPositions - bottleListItemDtos.Count;
        }

        if (request.SearchText is { Length: > 0 })
        {
            bottleListItemDtos = bottleListItemDtos
                .Where(b =>
                    (!string.IsNullOrEmpty(b.Name) && b.Name.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(b.Lot) && b.Lot.Trim().Equals(request.SearchText.Trim(), StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        if (request.SelectedStatuses is { Count: > 0 })
        {
            bottleListItemDtos = bottleListItemDtos
                .Where(b => ApplyStatusFilter(request, b))
                .ToList();
        }
    }

    private static bool ApplyStatusFilter(GetBottlesQuery request, BottleListItemDto b)
    {
        if (request.SelectedStatuses == null)
        {
            return false;
        }
        if (request.SelectedStatuses.Contains(ReagentPositionStatus.Reserved.ToString()) && b.Status == ReagentPositionStatus.Reserved)
        {
            return true;
        }

        if (request.SelectedStatuses.Contains(ReagentPositionStatus.Empty.ToString()) && b.Status == ReagentPositionStatus.Empty)
        {
            return true;
        }

        if (request.SelectedStatuses.Contains(Translations.Inventory_ErrorFilter) &&
            (b.Status == ReagentPositionStatus.UndetectedVolume ||
             b.Status == ReagentPositionStatus.DisconnectedBottle ||
             b.Status == ReagentPositionStatus.UnexpectedVolume ||
             b.Status == ReagentPositionStatus.UnknownBottle ||
             b.Status == ReagentPositionStatus.Expired))
        {
            return true;
        }

        return false;
    }

    private static List<BottleListItemDto> ApplySorting(List<BottleListItemDto> items, string? sortProperty, bool ascending)
    {
        if (string.IsNullOrEmpty(sortProperty))
        {
            return items
                .OrderBy(x => x.Type.ToString())
                .ThenBy(x => x.Position)
                .ToList();
        }

        return sortProperty switch
        {
            nameof(BottleListItemDto.Name) => SortBy(items, x => x.Name, ascending),
            nameof(BottleListItemDto.Instrument) => SortBy(items, x => x.Instrument, ascending),
            nameof(BottleListItemDto.Type) => SortBy(items, x => x.Type, ascending),
            nameof(BottleListItemDto.ExpiryDate) => SortBy(items, x => x.ExpiryDate, ascending),
            nameof(BottleListItemDto.Quantity) => SortBy(items, x => x.Quantity.Value, ascending),
            nameof(BottleListItemDto.Lot) => SortBy(items, x => x.Lot, ascending),
            nameof(BottleListItemDto.Status) => SortBy(items, x => x.Status, ascending),
            nameof(BottleListItemDto.Position) => SortByPosition(items, ascending),
            _ => items
        };
    }

    private static List<BottleListItemDto> SortBy<T>(List<BottleListItemDto> items, Func<BottleListItemDto, T> keySelector, bool ascending)
    {
        return ascending
            ? items.OrderBy(keySelector).ToList()
            : items.OrderByDescending(keySelector).ToList();
    }

    private static List<BottleListItemDto> SortByPosition(List<BottleListItemDto> items, bool ascending)
    {
        return ascending
            ? items.OrderBy(x => x.Type.ToString()).ThenBy(x => x.Position).ToList()
            : items.OrderByDescending(x => x.Type.ToString()).ThenByDescending(x => x.Position).ToList();
    }

    private static ReagentPositionStatus GetStatus(BottledReagent bottle)
    {
        return bottle.Error switch
        {
            EnumBottledReagentError.VolumeReadError => ReagentPositionStatus.UndetectedVolume,
            EnumBottledReagentError.CannotPumpLiquid => ReagentPositionStatus.DisconnectedBottle,
            EnumBottledReagentError.WrongVolume => ReagentPositionStatus.UnexpectedVolume,
            EnumBottledReagentError.BottleUnknown => ReagentPositionStatus.UnknownBottle,
            EnumBottledReagentError.BottleExpired => ReagentPositionStatus.Expired,
            EnumBottledReagentError.UnspecifiedBottledReagentError => CheckRemainingStatus(bottle),
            _ => throw new NotImplementedException()
        };
    }

    private static ReagentPositionStatus CheckRemainingStatus(BottledReagent bottle)
    {
        if (bottle.Content.RemainingQuantity == 0)
        {
            return ReagentPositionStatus.Empty;
        }

        if (bottle.InUse)
        {
            return ReagentPositionStatus.InUse;
        }

        if (bottle.Content.ReservedStartedQuantity > 0 || bottle.Content.ReservedScheduledQuantity > 0)
        {
            return ReagentPositionStatus.Reserved;
        }

        return ReagentPositionStatus.Available;
    }

    private static DateTime? GetExpiryDate(BottledReagent bottle, ReagentInfo reagentInfo)
    {
        var bottleExpiryDate = bottle.Content.ExpiryDate.ToDateTime();
        var reagentInfoExpiryDate = reagentInfo.Lot.ExpiryDate.DateTime;
        return bottleExpiryDate < reagentInfoExpiryDate
            ? bottleExpiryDate
            : reagentInfoExpiryDate;
    }
}
