using Pharos.Client.App.Parameters.Common.Models;
using Pharos.Client.App.Parameters.QualityControl.Models;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.Parameters.QualityControl.ViewModels;

public class PhadiaQualityControlListViewModel : VirtualGridViewModel<PhadiaQualityControl>
{
    private readonly IPhadiaQualityControlItemSource _itemSource;
    private readonly IMethodTestDataSource _methodTestDataSource;

    public PhadiaQualityControlListViewModel(IPhadiaQualityControlItemSource itemSource, IMethodTestDataSource methodTestDataSource, IExceptionHandlerService exceptionHandler) : base(itemSource, exceptionHandler)
    {
        _itemSource = itemSource;
        _methodTestDataSource = methodTestDataSource;
    }

    public async Task UpdateEnabled(int id, bool enabled)
    {
        await _itemSource.UpdateEnabled(id, enabled);
    }

    public async Task<Dictionary<string, string>> GetMethodNames()
    {
        var names = await _methodTestDataSource.GetMethodNamesAsync();
        return names.ToDictionary(x => x, x => x);
    }
}