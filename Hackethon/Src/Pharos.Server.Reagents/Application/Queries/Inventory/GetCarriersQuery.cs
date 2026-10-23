using System.Drawing;
using FluentValidation;
using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.LoadList;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Localization;
using Pharos.Server.Reagents.Application.Interfaces;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Queries.Inventory;

public class GetCarriersQuery : SortedPartialQuery<CarrierListItemDto>, IRequest<PartialQueryResult<CarrierListResultDto>>
{
    public string? SearchText { get; set; }
    public List<string>? SelectedInstruments { get; set; }
    public List<string>? SelectedStatuses { get; set; }
    public bool IsRefillLevelMissing { get; set; }
    public bool BypassRowLimit { get; set; }
}

internal class GetCarrierQueryHandler(IInstrumentManager instrumentManager, IReagentBarcodeService reagentBarcodeService,
    IInstrumentReagentStorage instrumentReagentStorage, ISender sender)
    : IRequestHandler<GetCarriersQuery, PartialQueryResult<CarrierListResultDto>>
{

    public async Task<PartialQueryResult<CarrierListResultDto>> Handle(GetCarriersQuery request, CancellationToken cancellationToken)
    {
        var instruments = instrumentManager.GetInstruments();
        var carrierListItemDtos = new List<CarrierListItemDto>();

        // Filter instruments based on SelectedInstruments, or use all if null/empty
        var filteredInstruments = request.SelectedInstruments != null && request.SelectedInstruments.Count > 0
            ? instruments.Where(i => request.SelectedInstruments.Contains(i.DisplayName ?? "")).ToList()
            : instruments;

        var carrierRefillLevels = await GetCarrierRefillLevels(
            filteredInstruments,
            cancellationToken);

        foreach (var instrument in filteredInstruments)
        {
            var carriers = instrumentReagentStorage.GetCarriers(instrument.SerialNumber)
                .Where(x => x.Position.PositionInternalStorage > 0)
                .ToList();

            foreach (var carrier in carriers)
            {
                var reagentInfo = await GetReagentInfo(carrier).ConfigureAwait(false);

                if (reagentInfo != null)
                {
                    carrierListItemDtos.Add(new CarrierListItemDto
                    {
                        Id = HashCode.Combine(carrier.ReagentBarcode, carrier.Position.PositionInternalStorage, instrument.SerialNumber),
                        Barcode = carrier.ReagentBarcode,
                        Udi = carrier.ReagentUdiBarcode,
                        Shape = reagentInfo.Article.Shape,
                        Colors = reagentInfo.Article.Colors.Select(c => c.HasValue ? ColorTranslator.ToHtml(c.Value) : null).ToList(),
                        Test = reagentInfo.Article.Identity,
                        Type = reagentInfo.Article.ArticleType,
                        Quantity = new QuantityDto
                        {
                            Value = carrier.Content.RemainingQuantity,
                            Unit = ArticleQuantityUnit.Tests
                        },
                        Lot = reagentInfo.Lot.LotNumber,
                        Status = GetCarrierStatus(carrier),
                        ExpiryDate = GetExpiryDate(carrier, reagentInfo),
                        IsRefillLevelDefined = IsRefillLevelDefined(
                            instrument.SerialNumber,
                            reagentInfo.Article.ArticleType,
                            reagentInfo.Article.Identity,
                            carrierRefillLevels),
                        Instrument = instrument.DisplayName ?? "",
                        InstrumentNumber = instrument.SerialNumber,
                        Position = carrier.Position.PositionInternalStorage,
                    });
                }
            }
        }

        // Apply filtering
        ApplyFiltering(request, ref carrierListItemDtos, filteredInstruments.Count, out var freePositions, out var totalPositions);

        // Apply default or Property Sorting
        carrierListItemDtos = ApplySorting(carrierListItemDtos, request.SortByProperty, request.SortByAscending);

        // Pagination
        var totalCount = carrierListItemDtos.Count;
        var pagedCarriersList = carrierListItemDtos
            .Skip(request.StartIndex)
            .Take(request.Count)
            .ToList();

        var carrierListResult = new CarrierListResultDto
        {
            //Id is placeholder to make UI grid work
            Id = HashCode.Combine(carrierListItemDtos.Count, totalCount),
            Items = pagedCarriersList,
            FreePositions = freePositions,
            TotalPositions = totalPositions,
        };

        return new PartialQueryResult<CarrierListResultDto>(
            new List<CarrierListResultDto> { carrierListResult },
            request.StartIndex,
            totalCount
        );
    }

    private async Task<ReagentInfo?> GetReagentInfo(CarrierInformation carrier)
    {
        var (articleType, _) = await reagentBarcodeService.DetectArticleType(carrier.ReagentBarcode).ConfigureAwait(false);
        var reagentInfo = await reagentBarcodeService.GetReagentInfo(articleType, carrier.ReagentBarcode, carrier.ReagentUdiBarcode);
        return reagentInfo;
    }

    private async Task<Dictionary<(string InstrumentNumber, ArticleType ArticleType, string Identity), int>> GetCarrierRefillLevels(
        ICollection<IInstrument> instruments,
        CancellationToken cancellationToken)
    {
        if (instruments.Count == 0)
        {
            return [];
        }

        var configurations = await sender.Send(
            new GetCarrierRefillLevelConfigurationsQuery(instruments.Select(instrument => instrument.SerialNumber).ToHashSet()),
            cancellationToken);

        return configurations.ToDictionary(
            configuration => (configuration.InstrumentNumber, configuration.ArticleType, configuration.Identity),
            configuration => configuration.RefillLevel);
    }

    private static bool IsRefillLevelDefined(
        string instrumentNumber,
        ArticleType articleType,
        string? identity,
        Dictionary<(string InstrumentNumber, ArticleType ArticleType, string Identity), int> carrierRefillLevels)
    {
        return !string.IsNullOrEmpty(identity) &&
               carrierRefillLevels.TryGetValue((instrumentNumber, articleType, identity), out var refillLevel) &&
               refillLevel > 0;
    }

    private static List<CarrierListItemDto> ApplySorting(List<CarrierListItemDto> items, string? sortProperty, bool ascending)
    {
        if (string.IsNullOrEmpty(sortProperty))
        {
            //default sort by multiple properties
            return items
                .OrderBy(x => x.Test)
                .ThenBy(x => x.Instrument)
                .ThenBy(x => x.Position)
                .ToList();
        }

        switch (sortProperty)
        {
            case nameof(CarrierListItemDto.Test):
                return SortBy(items, x => x.Test, ascending);
            case nameof(CarrierListItemDto.Instrument):
                return SortBy(items, x => x.Instrument, ascending);
            case nameof(CarrierListItemDto.Position):
                return SortBy(items, x => x.Position, ascending);
            case nameof(CarrierListItemDto.Type):
                return SortBy(items, x => x.Type.ToString(), ascending);
            case nameof(CarrierListItemDto.Quantity):
                return SortBy(items, x => x.Quantity?.Value, ascending);
            case nameof(CarrierListItemDto.Lot):
                return SortBy(items, x => x.Lot, ascending);
            case nameof(CarrierListItemDto.Status):
                return SortBy(items, x => x.Status, ascending);
            case nameof(CarrierListItemDto.ExpiryDate):
                return SortBy(items, x => x.ExpiryDate, ascending);
            default:
                return items;
        }
    }

    private static List<CarrierListItemDto> SortBy<T>(List<CarrierListItemDto> items, Func<CarrierListItemDto, T> keySelector, bool ascending)
    {
        return ascending
            ? items.OrderBy(keySelector).ToList()
            : items.OrderByDescending(keySelector).ToList();
    }

    private static void ApplyFiltering(GetCarriersQuery request, ref List<CarrierListItemDto> carrierListItemDtos,
        int? totalInstrumentCount, out int freePositions, out int totalPositions)
    {
        if (request.SelectedInstruments is { Count: > 0 })
        {
            // computes how many carrier positions are available(free) for the selected instruments
            totalPositions = request.SelectedInstruments.Count * Constants.MaxCarrierStoragePositions;
            freePositions = totalPositions - carrierListItemDtos.Count;
        }
        else
        {
            // computes how many carrier positions are available(free) for all instruments
            totalPositions = totalInstrumentCount.GetValueOrDefault() * Constants.MaxCarrierStoragePositions;
            freePositions = totalPositions - carrierListItemDtos.Count;
        }

        if (request.SearchText is { Length: > 0 })
        {
            carrierListItemDtos = carrierListItemDtos
                .Where(c =>
                (!string.IsNullOrEmpty(c.Test) && c.Test.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(c.Lot) && c.Lot.Trim().Equals(request.SearchText.Trim(), StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        if (request.SelectedStatuses is { Count: > 0 })
        {
            carrierListItemDtos = carrierListItemDtos
                .Where(c => ApplyStatusFilter(request, c))
                .ToList();
        }

        if (request.IsRefillLevelMissing)
        {
            carrierListItemDtos = carrierListItemDtos
                .Where(c => !c.IsRefillLevelDefined)
                .ToList();
        }
    }

    private static bool ApplyStatusFilter(GetCarriersQuery request, CarrierListItemDto c)
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
             c.Status == ReagentPositionStatus.DispenseError ||
             c.Status == ReagentPositionStatus.Expired))
        {
            return true;
        }

        return false;
    }

    private static DateTime? GetExpiryDate(CarrierInformation carrier, ReagentInfo reagentInfo)
    {
        var carrierExpiryDate = carrier.Content.ExpiryDate.ToDateTime();
        var reagentInfoExpiryDate = reagentInfo.Lot.ExpiryDate.DateTime;
        return carrierExpiryDate < reagentInfoExpiryDate
            ? carrierExpiryDate
            : reagentInfoExpiryDate;
    }

    private static ReagentPositionStatus GetCarrierStatus(CarrierInformation carrier)
    {
        return carrier.Error switch
        {
            EnumCarrierError.CarrierUnreadableOcrError => ReagentPositionStatus.UnreadableOcr,
            EnumCarrierError.CarrierGripError => ReagentPositionStatus.TransferNotPossible,
            EnumCarrierError.CarrierUnreadableBarcodeError => ReagentPositionStatus.UnreadableBarcode,
            EnumCarrierError.CarrierUnknownCarrierError => ReagentPositionStatus.UnknownCarrier,
            EnumCarrierError.CarrierStorageFullError => ReagentPositionStatus.InternalStorageFull,
            EnumCarrierError.CarrierCapDispenseError => ReagentPositionStatus.DispenseError,
            EnumCarrierError.CarrierExpiredError => ReagentPositionStatus.Expired,
            EnumCarrierError.CarrierNoError => CheckRemainingStatus(carrier),
            EnumCarrierError.UnspecifiedCarrierError => throw new ValidationException($"{nameof(CarrierInformation)}.{nameof(CarrierInformation.Error)} value is not set."),
            _ => throw new ValidationException($"{nameof(CarrierInformation)}.{nameof(CarrierInformation.Error)} value is unknown."),
        };
    }

    private static ReagentPositionStatus CheckRemainingStatus(CarrierInformation carrier)
    {
        if (carrier.UnloadPending)
        {
            return ReagentPositionStatus.Unloading;
        }

        if (carrier.Content.ReservedStartedQuantity > 0 || carrier.Content.ReservedScheduledQuantity > 0)
        {
            return ReagentPositionStatus.Reserved;
        }

        return carrier.Content.RemainingQuantity <= 0 ? ReagentPositionStatus.Empty : ReagentPositionStatus.Available;
    }
}
