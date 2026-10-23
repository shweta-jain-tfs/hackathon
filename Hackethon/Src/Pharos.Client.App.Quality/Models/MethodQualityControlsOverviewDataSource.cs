namespace Pharos.Client.App.Quality.Models;

public interface IMethodQualityControlsOverviewDataSource : IEntityDataSource<MethodQualityControlsOverview>
{
    public Task<List<string>> GetQualityControlsMethodNames();
}

internal sealed class MethodQualityControlsOverviewDataSource(HttpClient httpClient) : IMethodQualityControlsOverviewDataSource
{

    private static void Filter(DataSourceFilterRequest request,
        ref List<string> qualityControlMethodNameList)
    {
        foreach (var f in request.Filters)
        {
            if (f.PropertyName == nameof(MethodQualityControlsOverview.MethodName))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.EqualsTo:
                        qualityControlMethodNameList?.Add(f.FilterValue as string ?? string.Empty);
                        continue;
                }
            }

            throw new NotSupportedException(
                $"The specified filter is not supported by:  {nameof(DataSourceFilter.PropertyName)}={f.PropertyName}, {nameof(DataSourceFilter.FilterOperator)} = {f.FilterOperator}");
        }
    }

    public async Task<DataSourceResult<MethodQualityControlsOverview>> GetAsync(DataSourceRequest request)
    {
        var client = new MethodsClient(httpClient);
        List<string>? qualityControlMethodNameList = [];
        Filter(request, ref qualityControlMethodNameList);
        var items = await client.GetMethodQualityControlsOverviewAsync(qualityControlMethodNameList);

        var startIndex = Math.Max(0, request.StartIndex);
        var count = request.Count ?? items.Count;

        var page = items
            .Skip(startIndex)
            .Take(count)
            .ToList();

        return new DataSourceResult<MethodQualityControlsOverview>(page, items.Count);
    }

    public Task<MethodQualityControlsOverview> GetByIdAsync(int entityId)
    {
        throw new NotSupportedException();
    }

    public async Task<List<string>> GetQualityControlsMethodNames()
    {
        var client = new MethodsClient(httpClient);
        var result = await client.GetQualityControlsMethodNamesAsync();
        return result != null ? result.ToList() : new List<string>();
    }
}
