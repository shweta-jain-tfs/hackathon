using Pharos.Client.App.Quality.Models;
using Pharos.Client.Common.ViewModels;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.Quality.ViewModels;

internal class MethodQualityControlOverviewViewModel(IMethodQualityControlsOverviewDataSource dataSource, IExceptionHandlerService exceptionHandler)
    : VirtualGridViewModel<MethodQualityControlsOverview>(dataSource, exceptionHandler)
{
    public async Task<Dictionary<string, string>> GetQualityControlsMethodNames()
    {
        var result = await dataSource.GetQualityControlsMethodNames();
        var dict = result.ToDictionary(x => x, x => x);
        return dict;
    }
}
