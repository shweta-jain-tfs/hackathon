using Pharos.Client.Common.Models.DataServices;

namespace Pharos.Client.App.ServiceTools.Models;

public interface IMethodTestDataSource : IEntityDataSource<MethodTest>
{
    Task<List<string>> GetMethodNamesAsync();
}

internal sealed class MethodTestDataSource : IMethodTestDataSource
{
    private readonly HttpClient _httpClient;

    public MethodTestDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<string>> GetMethodNamesAsync()
    {
        var client = new MethodsClient(_httpClient);
        var methodNames = await client.GetMethodNamesAsync();
        return methodNames.ToList();
    }

    public async Task<DataSourceResult<MethodTest>> GetAsync(DataSourceRequest request)
    {
        string? testName = null;
        string? longTestName = null;
        string? methodName = null;
        List<string>? selectedMethods = [];

        SetFilter(request, ref methodName, ref testName, ref longTestName, ref selectedMethods);

        var client = new MethodsClient(_httpClient);
        var result = await client.GetRequestTestsAsync(
            testName,
            longTestName,
            methodName,
            selectedMethods,
            request.StartIndex,
            request.Count ?? 0,
            request.SortByProperty,
            request.SortByAscending);

        return new DataSourceResult<MethodTest>(result.Items.ToList(), result.TotalCount);
    }

    public Task<MethodTest> GetByIdAsync(int entityId)
    {
        throw new NotSupportedException();
    }

    private static void SetFilter(
        DataSourceRequest request,
        ref string? methodName,
        ref string? testName,
        ref string? longTestName,
        ref List<string>? selectedMethods)
    {
        foreach (var filter in request.Filters)
        {
            if (filter.PropertyName == nameof(MethodTest.MethodName))
            {
                switch (filter.FilterOperator)
                {
                    case FilterOperator.Contains:
                        methodName = filter.FilterValue as string;
                        continue;
                    case FilterOperator.EqualsTo:
                        selectedMethods?.Add(filter.FilterValue as string ?? string.Empty);
                        continue;
                }
            }

            if (filter.PropertyName == nameof(MethodTest.TestName))
            {
                if (filter.FilterOperator == FilterOperator.Contains)
                {
                    testName = filter.FilterValue as string;
                    continue;
                }
            }

            if (filter.PropertyName == nameof(MethodTest.FullTestName))
            {
                if (filter.FilterOperator == FilterOperator.Contains)
                {
                    longTestName = filter.FilterValue as string;
                    continue;
                }
            }

            throw new NotSupportedException(
                $"The specified filter is not supported by: {nameof(DataSourceFilter.PropertyName)}={filter.PropertyName}, {nameof(DataSourceFilter.FilterOperator)}={filter.FilterOperator}");
        }
    }
}
