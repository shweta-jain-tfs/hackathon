using Pharos.Client.App.Parameters.Reflex.Models;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.Parameters.Reflex.ViewModels;

public class ReflexDefinitionListPageViewModel : VirtualGridViewModel<ReflexDefinitionSummary>
{
    private readonly IReflexDataService _dataService;
    public ReflexDefinitionListPageViewModel(IReflexDataService dataService, IExceptionHandlerService exceptionHandler)
        : base(dataService, exceptionHandler)
    {
        _dataService = dataService;
    }

    public async Task DeleteReflexDefinition(int[] entityIds)
    {
        await _dataService.DeleteAsync(entityIds);
    }

    public async Task SetActiveReflexDefinition(int entityId, bool active)
    {
        await _dataService.SetActiveReflexDefinitionAsync(entityId, active);
    }
}