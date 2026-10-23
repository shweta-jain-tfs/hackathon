using Pharos.Client.App.ServiceTools.Models;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.ServiceTools.ViewModels.RequestGenerator;

public sealed class AddTestViewModel : VirtualGridViewModel<MethodTest>
{
    private readonly IMethodTestDataSource _methodTestDataSource;

    public AddTestViewModel(IMethodTestDataSource methodTestDataSource, IExceptionHandlerService exceptionHandler)
        : base(methodTestDataSource, exceptionHandler)
    {
        _methodTestDataSource = methodTestDataSource;
    }

    public async Task<Dictionary<string, string>> GetMultiFilterDataSource()
    {
        var methodNames = await _methodTestDataSource.GetMethodNamesAsync();
        return methodNames.ToDictionary(x => x, x => x);
    }
}
