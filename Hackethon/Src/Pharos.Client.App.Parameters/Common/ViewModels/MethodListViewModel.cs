using Pharos.Client.App.Parameters.Common.Models;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.Parameters.Common.ViewModels;

public sealed class MethodListViewModel : VirtualGridViewModel<MethodTest>
{
    private readonly IMethodTestDataSource _methodsDataSource;

    public MethodListViewModel(IMethodTestDataSource methodsDataSource, IExceptionHandlerService exceptionHandler) : base(methodsDataSource, exceptionHandler)
    {
        _methodsDataSource = methodsDataSource;
    }

    public async Task<Dictionary<string, string>> GetMultiFilterDataSource()
    {
        var result = await _methodsDataSource.GetMethodNamesAsync();
        var dict = result.ToDictionary(x => x, x => x);
        return dict;
    }
}