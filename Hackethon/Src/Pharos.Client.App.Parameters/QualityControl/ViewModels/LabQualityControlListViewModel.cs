using Pharos.Client.App.Parameters.Common.Models;
using Pharos.Client.App.Parameters.QualityControl.Models;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.App.Parameters.QualityControl.ViewModels;

public class LabQualityControlListViewModel : VirtualGridViewModel<LabQualityControl>
{
    private readonly ILabQualityControlItemSource _itemSource;
    private readonly IMethodTestDataSource _methodTestDataSource;

    public LabQualityControlListViewModel(ILabQualityControlItemSource itemSource, IMethodTestDataSource methodTestDataSource, IExceptionHandlerService exceptionHandler) : base(itemSource, exceptionHandler)
    {
        _itemSource = itemSource;
        _methodTestDataSource = methodTestDataSource;
    }

    public async Task DeleteLabQualityControls(HashSet<LabQualityControl> selectedItems)
    {
        var ids = selectedItems.Select(x => x.Id).ToHashSet();
        await _itemSource.Delete(ids);
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