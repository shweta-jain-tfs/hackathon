using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;

namespace Pharos.Client.App.Reagents.Models.Inventory.Cartridges;

internal class CartridgesListDataSource : IEntityDataSource<CartridgeListResult>, ISupportsRowLimitBypass
{
    private readonly HttpClient _httpClient;

    public CartridgesListDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DataSourceResult<CartridgeListResult>> GetAsync(DataSourceRequest request)
    {
        var client = new InventoryClient(_httpClient);
        var searchText = string.Empty;
        List<string>? selectedStatuses = [];
        List<string>? selectedInstrument = [];
        Filter(request, ref searchText, selectedStatuses, selectedInstrument);
        var result = await client.GetCartridgesAsync(
            searchText,
            selectedInstrument,
            selectedStatuses,
            request.RequiresRowLimitBypass,
            request.StartIndex,
            request.Count,
            request.SortByProperty,
            request.SortByAscending);
        return new DataSourceResult<CartridgeListResult>(result.Items.ToList(), result.TotalCount);
    }

    public Task<CartridgeListResult> GetByIdAsync(int entityId)
    {
        throw new NotImplementedException();
    }

    private static void Filter(DataSourceFilterRequest request, ref string searchText, List<string> statuses, List<string> instrumentNameList)
    {
        foreach (var f in request.Filters)
        {
            if (f.PropertyName == nameof(CartridgeListItem.Method))
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
            if (f.PropertyName == nameof(CartridgeListItem.Status))
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

            if (f.PropertyName == nameof(CartridgeListItem.Instrument))
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
