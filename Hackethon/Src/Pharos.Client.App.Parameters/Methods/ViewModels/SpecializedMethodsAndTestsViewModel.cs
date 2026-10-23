using Pharos.Client.App.Parameters.Methods.Models;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Client.App.Parameters.Methods.ViewModels;

public sealed class SpecializedMethodsAndTestsViewModel : VirtualGridViewModel<SpecializedMethodOrTest>
{
    private readonly ISpecializedMethodsAndTestsDataSource _dataSource;
    private readonly Dictionary<int, bool> _originalActiveStates = new();
    private readonly Dictionary<int, SaveSpecializedRequest> _pendingChanges = new();

    public SpecializedMethodsAndTestsViewModel(
        ISpecializedMethodsAndTestsDataSource dataSource,
        IExceptionHandlerService exceptionHandler)
        : base(dataSource, exceptionHandler)
    {
        _dataSource = dataSource;
    }

    public bool HasChanges => _pendingChanges.Count > 0;

    public new async ValueTask<GridItemsProviderResult<SpecializedMethodOrTest>> ProvideGridItems(GridItemsProviderRequest<SpecializedMethodOrTest> request)
    {
        var result = await base.ProvideGridItems(request);

        foreach (var item in result.Items)
        {
            _originalActiveStates[item.Id] = item.IsSpecializedMethodOrTestActivated;

            if (_pendingChanges.TryGetValue(item.Id, out var pendingChange))
            {
                item.IsSpecializedMethodOrTestActivated = pendingChange.IsSpecializedMethodOrTestActivated;
            }
        }

        return result;
    }

    public async Task SetActive(SpecializedMethodOrTest item, bool isActive)
    {
        item.IsSpecializedMethodOrTestActivated = isActive;

        if (_originalActiveStates.TryGetValue(item.Id, out var originalActiveState) && originalActiveState == isActive)
        {
            _pendingChanges.Remove(item.Id);
        }
        else
        {
            _pendingChanges[item.Id] = new SaveSpecializedRequest
            {
                MethodId = item.MethodId,
                MethodTestId = item.MethodTestId,
                Type = item.Type,
                IsSpecializedMethodOrTestActivated = isActive
            };
        }

        await Task.CompletedTask;
    }

    public async Task Save(CancellationToken cancellationToken)
    {
        if (_pendingChanges.Count == 0)
        {
            return;
        }

        var pendingChanges = _pendingChanges.ToList();

        await _dataSource.SaveAsync(pendingChanges.Select(x => x.Value).ToList(), cancellationToken);

        foreach (var pendingChange in pendingChanges)
        {
            _originalActiveStates[pendingChange.Key] = pendingChange.Value.IsSpecializedMethodOrTestActivated;
        }

        _pendingChanges.Clear();
    }
}
