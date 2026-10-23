using System.Collections.Concurrent;
using Pharos.Server.Application.Reagents;
using Pharos.Server.LoadList.Application.Commands.HandleProcessListCommand;
using Pharos.Server.LoadList.Domain;

namespace Pharos.Server.LoadList.Application.Queries;

internal record GetLoadListSampleTubesQuery : GetLoadListItemsQuery<LoadListSampleTubeDto>;

internal class GetLoadListSampleTubesQueryHandler : GetLoadListItemBaseQueryHandler<GetLoadListSampleTubesQuery, LoadListSampleTubeDto>
{
    private readonly ILoadListService _loadListService;

    public GetLoadListSampleTubesQueryHandler(
        ILoadListService loadListService,
        IInstrumentManager instrumentManager,
        IInstrumentReagentStorageUtilization storageUtilization)
        : base(instrumentManager, storageUtilization)
    {
        _loadListService = loadListService;
    }

    protected override LoadListItemType LoadListItemType => LoadListItemType.SampleTube;

    protected override bool SupportsCapQuantity => false;

    protected override ConcurrentDictionary<string, Dictionary<string, LoadListSampleTubeDto>> GetLoadListItems()
    {
        return _loadListService.GetLoadListSampleTubes();
    }
}