namespace Pharos.Client.App.Quality.Models;

public interface IMethodQualityDataSource : IEntityDataSource<MethodQuality>;

internal sealed class MethodQualityDataSource(HttpClient httpClient) : IMethodQualityDataSource
{
    public async Task<DataSourceResult<MethodQuality>> GetAsync(DataSourceRequest request)
    {
        var client = new QualityClient(httpClient);
        List<string>? methodNameList = [];
        DateTimeOffset? fromDate = null;
        DateTimeOffset? toDate = null;
        List<string> instrumentNumberList = [];

        Filter(request, ref methodNameList, ref fromDate, ref toDate, instrumentNumberList);

        var methodQualityList = await client.GetMethodQualityAsync(methodNameList, fromDate, toDate, instrumentNumberList);

        // Grouping in MethodListView is on MethodName + Instrument,
        // so GroupedCount should reflect distinct (method, instrument) groups.
        var groupedCount = methodQualityList
            .Select(x => x.MethodInstrumentGroupKey)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var startIndex = Math.Max(0, request.StartIndex);
        var count = request.Count ?? methodQualityList.Count;

        var page = methodQualityList
            .Skip(startIndex)
            .Take(count)
            .ToList();

        return new DataSourceResult<MethodQuality>(page, methodQualityList.Count, groupedCount);
    }

    private static void Filter(DataSourceFilterRequest request,
        ref List<string> methodNameList, ref DateTimeOffset? fromDate, ref DateTimeOffset? toDate, List<string> instrumentNumberList)
    {
        fromDate = null;
        toDate = null;

        foreach (var f in request.Filters)
        {
            if (f.PropertyName == nameof(MethodQuality.MethodName))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.EqualsTo:
                        methodNameList?.Add(f.FilterValue as string ?? string.Empty);
                        continue;
                }
            }

            if (f.PropertyName == nameof(MethodQuality.InstrumentName))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.EqualsTo:
                        instrumentNumberList?.Add(f.FilterValue as string ?? string.Empty);
                        continue;
                }
            }

            throw new NotSupportedException(
                $"The specified filter is not supported by:  {nameof(DataSourceFilter.PropertyName)}={f.PropertyName}, {nameof(DataSourceFilter.FilterOperator)} = {f.FilterOperator}");
        }
    }

    public async Task<MethodQuality> GetByIdAsync(int entityId)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }
}