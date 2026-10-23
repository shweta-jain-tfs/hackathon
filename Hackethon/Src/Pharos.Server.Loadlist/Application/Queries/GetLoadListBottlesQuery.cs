using System.Collections.Concurrent;
using Pharos.Server.Application.Reagents;
using Pharos.Server.LoadList.Application.Commands.HandleProcessListCommand;
using Pharos.Server.LoadList.Domain;

namespace Pharos.Server.LoadList.Application.Queries;

internal record GetLoadListBottlesQuery : GetLoadListItemsQuery<LoadListBottleDto>;

internal class GetLoadListBottlesQueryHandler : GetLoadListItemBaseQueryHandler<GetLoadListBottlesQuery, LoadListBottleDto>
{
    private readonly ILoadListService _loadListService;

    public GetLoadListBottlesQueryHandler(
        ILoadListService loadListService,
        IInstrumentManager instrumentManager,
        IInstrumentReagentStorageUtilization storageUtilization)
        : base(instrumentManager, storageUtilization)
    {
        _loadListService = loadListService;
    }

    protected override ConcurrentDictionary<string, Dictionary<string, LoadListBottleDto>> GetLoadListItems()
    {
        return _loadListService.GetLoadListBottles();
    }

    protected override LoadListItemType LoadListItemType => LoadListItemType.Bottle;

    protected override bool SupportsCapQuantity => false;
}