using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Reagents.Models.Inventory.Bottles;
internal class BottlesListDataSource : IEntityDataSource<BottleListResult>
{
    private readonly HttpClient _httpClient;

    public BottlesListDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DataSourceResult<BottleListResult>> GetAsync(DataSourceRequest request)
    {
        var client = new InventoryClient(_httpClient);
        var searchText = string.Empty;
        List<string>? selectedStatuses = [];
        List<string>? selectedInstrument = [];
        Filter(request, ref searchText, selectedStatuses, selectedInstrument);
        var result = await client.GetBottlesAsync(
            searchText,
            selectedInstrument,
            selectedStatuses,
            request.StartIndex,
            request.Count,
            request.SortByProperty,
            request.SortByAscending,
            request.CancellationToken
        );
        return new DataSourceResult<BottleListResult>(result.Items.ToList(), result.TotalCount);
    }

    Task<BottleListResult> IGetByIdDataSource<BottleListResult, int>.GetByIdAsync(int entityId)
    {
        throw new NotImplementedException();
    }

    private static void Filter(DataSourceFilterRequest request, ref string searchText, List<string> statuses, List<string> instrumentNameList)
    {
        foreach (var f in request.Filters)
        {
            if (f.PropertyName == nameof(BottleListItem.Name))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.Contains:
                        if (f.FilterValue is string text && !string.IsNullOrWhiteSpace(text))
                        {
                            searchText = text;
                            continue;
                        }
                        break;
                    case FilterOperator.EqualsTo:
                        if (f.FilterValue is string exactMatch && !string.IsNullOrWhiteSpace(exactMatch))
                        {
                            searchText = exactMatch;
                            continue;
                        }
                        break;
                }
            }
            if (f.PropertyName == nameof(BottleListItem.Status))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.EqualsTo:
                        if (f.FilterValue != null)
                        {
                            if (f.FilterValue is string status)
                            {
                                statuses?.Add(status);

                            }
                        }
                        continue;
                }
            }

            if (f.PropertyName == nameof(BottleListItem.Instrument))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.EqualsTo:
                        instrumentNameList?.Add(f.FilterValue as string ?? string.Empty);
                        continue;
                }
            }

            throw new NotSupportedException(
                $"The specified filter is not supported by:  {nameof(DataSourceFilter.PropertyName)}={f.PropertyName}, {nameof(DataSourceFilter.FilterOperator)} = {f.FilterOperator}");
        }
    }
}
