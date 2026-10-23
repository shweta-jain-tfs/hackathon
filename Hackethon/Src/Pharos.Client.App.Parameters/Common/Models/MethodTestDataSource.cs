namespace Pharos.Client.App.Parameters.Common.Models;

public interface IMethodTestDataSource : IEntityDataSource<MethodTest>
{
    Task<List<string>> GetMethodNamesAsync();

    Task<ParameterInfo> GetParameterInfoAsync(string testName, string methodName);
}

internal sealed class MethodTestDataSource : IMethodTestDataSource
{
    private readonly HttpClient _httpClient;

    public MethodTestDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DataSourceResult<MethodTest>> GetAsync(DataSourceRequest request)
    {
        string? testName = null;
        string? longTestName = null;
        string? methodName = null;

        List<string>? selectedMethods = new();

        SetFilter(request, ref methodName, ref testName, ref longTestName, ref selectedMethods);

        var client = new MethodsClient(_httpClient);
        var result = await client.GetReflexTestsAsync(testName, longTestName, methodName, selectedMethods, request.StartIndex, request.Count ?? 0, request.SortByProperty, request.SortByAscending);

        return new DataSourceResult<MethodTest>(result.Items.ToList(), result.TotalCount);
    }

    public async Task<List<string>> GetMethodNamesAsync()
    {
        var client = new MethodsClient(_httpClient);
        var methodNames = await client.GetMethodNamesAsync();
        return methodNames.ToList();
    }

    public async Task<ParameterInfo> GetParameterInfoAsync(string testName, string methodName)
    {
        var client = new MethodsClient(_httpClient);
        return await client.GetParameterInfoAsync(testName, methodName);
    }

    public Task<MethodTest> GetByIdAsync(int entityId)
    {
        throw new NotSupportedException();
    }

    private static void SetFilter(DataSourceRequest request, ref string? methodName, ref string? testName, ref string? longTestName, ref List<string>? selectedStrings)
    {
        foreach (var f in request.Filters)
        {
            if (f.PropertyName == nameof(MethodTest.MethodName))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.Contains:
                        methodName = f.FilterValue as string;
                        continue;

                    case FilterOperator.EqualsTo:
                        selectedStrings?.Add(f.FilterValue as string ?? string.Empty);
                        continue;
                }
            }

            if (f.PropertyName == nameof(MethodTest.TestName))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.Contains:
                        testName = f.FilterValue as string;
                        continue;
                }
            }

            if (f.PropertyName == nameof(MethodTest.FullTestName))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.Contains:
                        longTestName = f.FilterValue as string;
                        continue;
                }
            }

            throw new NotSupportedException(
                $"The specified filter is not supported by:  {nameof(DataSourceFilter.PropertyName)}={f.PropertyName}, {nameof(DataSourceFilter.FilterOperator)} = {f.FilterOperator}");
        }
    }
}