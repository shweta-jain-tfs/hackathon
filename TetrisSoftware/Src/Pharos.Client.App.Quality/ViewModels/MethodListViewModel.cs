using Pharos.Client.App.Quality.Models;
using Pharos.Client.App.Result.Models;
using Pharos.Client.Common.ViewModels;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.Quality.ViewModels;

internal sealed class MethodListViewModel(IMethodQualityDataSource dataSource, IMethodNameDataSource methodNameDataSource, IExceptionHandlerService exceptionHandler) : VirtualGridViewModel<MethodQuality>(dataSource, exceptionHandler)
{
    public async Task<Dictionary<string, string>> GetMethodNamesAsync()
    {
        var result = await methodNameDataSource.GetMethodNamesAsync();
        var dict = result.ToDictionary(x => x, x => x);
        return dict;
    }
}
