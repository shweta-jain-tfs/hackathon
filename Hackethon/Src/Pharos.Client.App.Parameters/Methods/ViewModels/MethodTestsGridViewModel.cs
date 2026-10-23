using Pharos.Client.App.Parameters.Methods.Models;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Client.App.Parameters.Methods.ViewModels;

/// <summary>
/// View model responsible for fetching and providing the paged list of tests associated with a method.
/// Consumed by the method details test tab grid, where <c>methodId</c> is passed per request
/// rather than stored as state on this view model.
/// </summary>
public sealed class MethodTestsGridViewModel : VirtualGridViewModel<MethodTest, int>
{
    private readonly IMethodTestPagedDataSource _dataSource;

    public MethodTestsGridViewModel(
        IMethodTestPagedDataSource dataSource,
        IExceptionHandlerService exceptionHandler)
        : base(dataSource, exceptionHandler)
    {
        _dataSource = dataSource;
    }

    public async ValueTask<GridItemsProviderResult<MethodTest>> ProvideGridItems(
        GridItemsProviderRequest<MethodTest> request, int methodId)
    {
        var dsRequest = ToDataSourceRequest(request);
        var result = await _dataSource.GetAsync(dsRequest, methodId);
        return GridItemsProviderResult.From(result.Items, result.TotalItemCount);
    }
}
