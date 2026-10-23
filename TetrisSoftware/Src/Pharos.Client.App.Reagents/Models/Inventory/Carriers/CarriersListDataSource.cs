using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;

namespace Pharos.Client.App.Reagents.Models.Inventory.Carriers;

internal class CarriersListDataSource : IEntityDataSource<CarrierListResult>, ISupportsRowLimitBypass
{
    private readonly HttpClient _httpClient;

    public CarriersListDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DataSourceResult<CarrierListResult>> GetAsync(DataSourceRequest request)
    {
        var client = new InventoryClient(_httpClient);
        var searchText = string.Empty;
        List<string>? selectedStatuses = [];
        List<string>? selectedInstrument = [];
        var isRefillLevelMissing = false;
        Filter(request, ref searchText, selectedStatuses, selectedInstrument, ref isRefillLevelMissing);
        var result = await client.GetCarriersAsync(
            searchText,
            selectedInstrument,
            selectedStatuses,
            isRefillLevelMissing,
            request.RequiresRowLimitBypass,
            request.StartIndex,
            request.Count,
            request.SortByProperty,
            request.SortByAscending,
            request.CancellationToken
        );
        return new DataSourceResult<CarrierListResult>(result.Items.ToList(), result.TotalCount);
    }

    public Task<CarrierListResult> GetByIdAsync(int entityId)
    {
        throw new NotImplementedException();
    }

    private static void Filter(
        DataSourceFilterRequest request,
        ref string searchText,
        List<string> statuses,
        List<string> instrumentNameList,
        ref bool isRefillLevelMissing)
    {
        foreach (var f in request.Filters)
        {
            if (f.PropertyName == nameof(CarrierListItem.Test))
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
            if (f.PropertyName == nameof(CarrierListItem.Status))
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

            if (f.PropertyName == nameof(CarrierListItem.Instrument))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.EqualsTo:
                        instrumentNameList?.Add(f.FilterValue as string ?? string.Empty);
                        continue;
                }
            }

            if (f.PropertyName == nameof(CarrierListItem.IsRefillLevelDefined))
            {
                if (f.FilterOperator == FilterOperator.Contains)
                {
                    isRefillLevelMissing = f.FilterValue is true;
                }
                continue;
            }

            throw new NotSupportedException(
                $"The specified filter is not supported by:  {nameof(DataSourceFilter.PropertyName)}={f.PropertyName}, {nameof(DataSourceFilter.FilterOperator)} = {f.FilterOperator}");
        }
    }
}
