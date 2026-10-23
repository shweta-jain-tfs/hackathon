using System.Collections.Concurrent;
using Pharos.Server.Application.Reagents;
using Pharos.Server.LoadList.Application.Commands.HandleProcessListCommand;
using Pharos.Server.LoadList.Domain;

namespace Pharos.Server.LoadList.Application.Queries;

internal record GetLoadListCartridgesQuery : GetLoadListItemsQuery<LoadListCartridgeDto>;

internal class GetLoadListCartridgeQueryHandler(ILoadListService loadListService, IInstrumentManager instrumentManager, IInstrumentReagentStorageUtilization storageUtilization) :
    GetLoadListItemBaseQueryHandler<GetLoadListCartridgesQuery, LoadListCartridgeDto>(instrumentManager, storageUtilization)
{
    protected override LoadListItemType LoadListItemType => LoadListItemType.Cartridge;

    protected override ConcurrentDictionary<string, Dictionary<string, LoadListCartridgeDto>> GetLoadListItems()
    {
        return loadListService.GetLoadListCartridges();
    }
}