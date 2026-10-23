using System.Collections.Concurrent;
using Pharos.Server.Application.Reagents;
using Pharos.Server.LoadList.Application.Commands.HandleProcessListCommand;
using Pharos.Server.LoadList.Domain;

namespace Pharos.Server.LoadList.Application.Queries;

internal record GetLoadListCarriersQuery : GetLoadListItemsQuery<LoadListCarrierDto>;

internal class GetLoadListCarriersQueryHandler(ILoadListService loadListService, IInstrumentManager instrumentManager, IInstrumentReagentStorageUtilization storageUtilization) :
    GetLoadListItemBaseQueryHandler<GetLoadListCarriersQuery, LoadListCarrierDto>(instrumentManager, storageUtilization)
{
    protected override LoadListItemType LoadListItemType => LoadListItemType.Carrier;

    protected override ConcurrentDictionary<string, Dictionary<string, LoadListCarrierDto>> GetLoadListItems()
    {
        return loadListService.GetLoadListCarriers();
    }
}