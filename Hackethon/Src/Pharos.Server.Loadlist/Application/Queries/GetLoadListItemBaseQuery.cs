using System.Collections.Concurrent;
using Pharos.Server.Application.Reagents;
using Pharos.Server.LoadList.Application.Commands.HandleProcessListCommand;
using Pharos.Server.LoadList.Domain;

namespace Pharos.Server.LoadList.Application.Queries;

internal abstract record GetLoadListItemsQuery<TLoadListDto> : IRequest<List<TLoadListDto>>;

internal abstract class GetLoadListItemBaseQueryHandler<TQuery, TLoadListDto>(IInstrumentManager instrumentManager, IInstrumentReagentStorageUtilization storageUtilization)
    : IRequestHandler<TQuery, List<TLoadListDto>>
    where TQuery : GetLoadListItemsQuery<TLoadListDto>
    where TLoadListDto : LoadListItemBase
{
    public Task<List<TLoadListDto>> Handle(TQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(MapToFlatList());
    }

    protected abstract LoadListItemType LoadListItemType { get; }

    protected virtual bool SupportsCapQuantity => true;

    protected abstract ConcurrentDictionary<string, Dictionary<string, TLoadListDto>> GetLoadListItems();

    private List<TLoadListDto> MapToFlatList()
    {
        var loadListItems = GetLoadListItems();

        var flatList = new List<TLoadListDto>();

        foreach (var (instrumentNumber, list) in loadListItems)
        {
            if(SupportsCapQuantity)
            {
                CapQuantityToLoad(list, storageUtilization.GetAvailablePositions(instrumentNumber, Map(LoadListItemType)));
            }

            var displayName = instrumentManager.FindInstrument(instrumentNumber)?.DisplayName;

            if (displayName == null)
            {
                continue;
            }

            foreach (var item in list)
            {
                //Set instrument display name
                item.Value.InstrumentName = displayName;
            }

            flatList.AddRange(list.Values.Where(x => x.QuantityToLoad > 0));
        }

        return flatList;
    }

    private static ReagentStorageType Map(LoadListItemType loadListItemType)
    {
        return loadListItemType switch
        {
            LoadListItemType.Cartridge => ReagentStorageType.Cartridge,
            LoadListItemType.Carrier => ReagentStorageType.Carrier,
            LoadListItemType.DilutionPlate => ReagentStorageType.DilutionPlate,
            _ => throw new ArgumentOutOfRangeException(nameof(loadListItemType), loadListItemType, null)
        };
    }

    private static void CapQuantityToLoad(Dictionary<string, TLoadListDto> list, int availablePositions)
    {
        var items = list
            .Select(x => new ScaledQuantity(x.Key, x.Value, x.Value.RequestedQuantityToLoad, x.Value.RequestedQuantityToLoad, 0))
            .ToList();

        foreach (var item in items)
        {
            item.Item.OverrideQuantityToLoad(item.OriginalQuantity);
        }

        if (availablePositions <= 0)
        {
            return;
        }

        var itemsWithQuantity = items
            .Where(x => x.OriginalQuantity > 0)
            .ToList();

        var totalQuantityToLoad = itemsWithQuantity.Sum(x => x.Quantity);

        if (totalQuantityToLoad <= availablePositions)
        {
            return;
        }

        var scale = availablePositions / (double)totalQuantityToLoad;

        var scaledItems = itemsWithQuantity
            .Select(x =>
            {
                var scaledQuantity = x.Quantity * scale;
                var flooredQuantity = (int)Math.Floor(scaledQuantity);

                return new ScaledQuantity(x.Key, x.Item, x.OriginalQuantity, flooredQuantity, scaledQuantity - flooredQuantity);
            })
            .ToList();

        var remaining = availablePositions - scaledItems.Sum(x => x.Quantity);

        if (remaining > 0)
        {
            var totalOriginalQuantity = scaledItems.Sum(x => x.OriginalQuantity);

            var baseExtraAssignments = scaledItems
                .Select(x =>
                {
                    var proportionalExtra = remaining * (x.OriginalQuantity / (double)totalOriginalQuantity);
                    var baseExtra = (int)Math.Floor(proportionalExtra);

                    return new ScaledQuantity(x.Key, x.Item, x.OriginalQuantity, baseExtra, proportionalExtra - baseExtra);
                })
                .ToList();

            foreach (var assignment in baseExtraAssignments)
            {
                var item = scaledItems.First(x => x.Key == assignment.Key);
                item.Quantity += assignment.Quantity;
            }

            var remainingAfterBaseAssignments = remaining - baseExtraAssignments.Sum(x => x.Quantity);

            foreach (var assignment in baseExtraAssignments
                         .OrderByDescending(x => x.Remainder)
                         .ThenBy(x => x.Key)
                         .Take(remainingAfterBaseAssignments))
            {
                var item = scaledItems.First(x => x.Key == assignment.Key);
                item.Quantity++;
            }
        }

        foreach (var item in scaledItems)
        {
            item.Item.OverrideQuantityToLoad(item.Quantity);
        }
    }

    private sealed class ScaledQuantity(string key, TLoadListDto item, int originalQuantity, int quantity, double remainder)
    {
        public string Key { get; } = key;
        public TLoadListDto Item { get; } = item;
        public int OriginalQuantity { get; } = originalQuantity;
        public int Quantity { get; set; } = quantity;
        public double Remainder { get; } = remainder;
    }
}