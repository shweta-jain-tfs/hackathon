using Pharos.Client.App.Parameters.Panels.Models;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.Parameters.Panels.ViewModels;

public sealed class PanelsListViewModel(
    IMethodPanelsDataSource dataService,
    IExceptionHandlerService exceptionHandler) : VirtualGridViewModel<MethodPanel>(dataService, exceptionHandler)
{
    public Task<bool> CheckPanelsReferenced(int[] ids)
    {
        return dataService.CheckPanelsReferenced(ids);
    }

    public async Task<DeleteResponse> DeletePanels(int[] ids, bool removeTestPanelReferences)
    {
        return await dataService.DeleteAsync(ids, removeTestPanelReferences);
    }
}
