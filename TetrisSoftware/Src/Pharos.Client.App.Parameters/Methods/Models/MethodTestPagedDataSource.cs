namespace Pharos.Client.App.Parameters.Methods.Models;

internal sealed class MethodTestPagedDataSource(HttpClient httpClient) : IMethodTestPagedDataSource
{
    public async Task<DataSourceResult<MethodTest>> GetAsync(DataSourceRequest request, int methodId)
    {
        string? searchFilter = null;

        foreach (var f in request.Filters)
        {
            if (f.PropertyName == nameof(MethodTest.TestName) && f.FilterOperator == FilterOperator.Contains)
            {
                searchFilter = f.FilterValue as string;
            }
        }

        var client = new MethodsClient(httpClient);
        var result = await client.GetMethodTestsAsync(methodId, searchFilter, request.StartIndex, request.Count ?? 0, request.SortByProperty, request.SortByAscending);

        return new DataSourceResult<MethodTest>(result.Items.ToList(), result.TotalCount);
    }

    public Task<DataSourceResult<MethodTest>> GetAsync(DataSourceRequest request)
    {
        throw new NotImplementedException("Use GetAsync(DataSourceRequest request, int methodId) instead.");
    }
}
