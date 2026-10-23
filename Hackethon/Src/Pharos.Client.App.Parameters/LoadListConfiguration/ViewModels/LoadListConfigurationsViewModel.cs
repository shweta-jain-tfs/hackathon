using Pharos.Client.App.Parameters.LoadListConfiguration.Models;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Client.App.Parameters.LoadListConfiguration.ViewModels;

public class LoadListConfigurationsViewModel
{
    private readonly LoadListConfigurationsDataService _dataSource;

    public LoadListConfigurationsViewModel(LoadListConfigurationsDataService dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<RefillLevelInfo?> GetTotalRefillLevel(LoadListItemType loadListItemType, string selectedInstrument)
    {
        if (loadListItemType is LoadListItemType.Carrier or LoadListItemType.Cartridge)
        {
            return await _dataSource.GetTotalRefillLevel(loadListItemType, selectedInstrument);
        }

        return null;
    }

    public async ValueTask<GridItemsProviderResult<Client.Common.Models.LoadListConfiguration>> ProvideGridItems(GridItemsProviderRequest<Client.Common.Models.LoadListConfiguration> request, string instrument, LoadListItemType loadListItemType)
    {
        // Map column filters to DataSourceFilter  
        var colFilters = request.ColumnFilters?
            .SelectMany(cf => cf.FilterExpressions.Select(expr =>
                new DataSourceFilter(
                    cf.PropertyName,
                    MapPropertyFilterOperatorToFilterOperator(expr.Operator),
                    expr.UntypedValue
                )))
            .ToList();


        var filters = colFilters ?? new List<DataSourceFilter>();

        filters.Add(new DataSourceFilter(nameof(Client.Common.Models.LoadListConfiguration.InstrumentSerial), FilterOperator.EqualsTo, instrument));
        filters.Add(new DataSourceFilter(nameof(Client.Common.Models.LoadListConfiguration.LoadListItemType), FilterOperator.EqualsTo, loadListItemType));

        var dataSourceRequest = new DataSourceRequest
        {
            StartIndex = request.StartIndex,
            Count = request.Count,
            SortByProperty = request.SortByColumn,
            SortByAscending = request.SortByAscending,
            Filters = filters,
            RequiresRowLimitBypass = request.RequiresRowLimitBypass,
            CancellationToken = request.CancellationToken
        };

        var result = await _dataSource.GetAsync(dataSourceRequest);
        var totalCount = result.TotalItemCount;

        return GridItemsProviderResult.From(result.Items.ToList(), totalCount);
    }

    private static FilterOperator MapPropertyFilterOperatorToFilterOperator(PropertyFilterOperator propertyFilterOperator)
    {
        return propertyFilterOperator switch
        {
            PropertyFilterOperator.EqualsTo => FilterOperator.EqualsTo,
            PropertyFilterOperator.GreaterThan => FilterOperator.GreaterThan,
            PropertyFilterOperator.GreaterThanOrEqualTo => FilterOperator.GreaterThanOrEqualTo,
            PropertyFilterOperator.LessThan => FilterOperator.LessThan,
            PropertyFilterOperator.LessThanOrEqualTo => FilterOperator.LessThanOrEqualTo,
            PropertyFilterOperator.Contains => FilterOperator.Contains,
            _ => throw new ArgumentOutOfRangeException(nameof(propertyFilterOperator), propertyFilterOperator, null)
        };
    }

    public async Task UpdateLevels(List<int> updatedConfigurations, int? refillLevel, int? triggerLevel)
    {
        await _dataSource.UpdateLevelsAsync(updatedConfigurations, refillLevel, triggerLevel);

    }
    public async Task CopyLevels(string sourceInstrument, HashSet<string> selectedInstruments)
    {
        await _dataSource.CopyLevelsAsync(sourceInstrument, selectedInstruments);
    }
}