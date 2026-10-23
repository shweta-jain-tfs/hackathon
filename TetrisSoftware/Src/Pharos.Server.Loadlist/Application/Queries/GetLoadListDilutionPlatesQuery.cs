using System.Collections.Concurrent;
using Pharos.Server.Application.Reagents;
using Pharos.Server.LoadList.Application.Commands.HandleProcessListCommand;
using Pharos.Server.LoadList.Domain;

namespace Pharos.Server.LoadList.Application.Queries;

internal record GetLoadListDilutionPlatesQuery : GetLoadListItemsQuery<LoadListDilutionPlateDto>;

internal class GetLoadListDilutionPlatesQueryHandler : GetLoadListItemBaseQueryHandler<GetLoadListDilutionPlatesQuery, LoadListDilutionPlateDto>
{
    private readonly ILoadListService _loadListService;

    public GetLoadListDilutionPlatesQueryHandler(ILoadListService loadListService,
        IInstrumentManager instrumentManager,
        IInstrumentReagentStorageUtilization storageUtilization)
        : base(instrumentManager, storageUtilization)
    {
        _loadListService = loadListService;
    }

    protected override LoadListItemType LoadListItemType => LoadListItemType.DilutionPlate;

    protected override ConcurrentDictionary<string, Dictionary<string, LoadListDilutionPlateDto>> GetLoadListItems()
    {
        return _loadListService.GetLoadListDilutionPlates();
    }
}