using Pharos.Client.App.Reagents.Models.Inventory.Bottles;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Client.App.Reagents.ViewModels.Inventory;
internal class BottlesListViewModel(
    IEntityDataSource<BottleListResult> bottlesListResultDataSource,
    IBottlesNotificationProvider bottlesNotificationProvider) : LoadingViewModel, IDisposable
{
    private IEnumerable<BottleListItem> _bottles = [];

    // TODO: using a property and a backing field for separate purposes is confusing, consider using a single property with a private setter.
    public IEnumerable<BottleListItem> Bottles { get; set; } = [];

    public int FreePositions
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(FreePositions));
            }
        }
    }

    public int TotalPositions
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(TotalPositions));
            }
        }
    }

    public async ValueTask<GridItemsProviderResult<BottleListItem>> ProvideItems(GridItemsProviderRequest<BottleListItem> request)
    {
        // Map column filters to DataSourceFilter  
        ICollection<DataSourceFilter> filters = request.ColumnFilters?
            .SelectMany(cf => cf.FilterExpressions.Select(expr =>
                new DataSourceFilter(
                    cf.PropertyName,
                    MapPropertyFilterOperatorToFilterOperator(expr.Operator),
                    expr.UntypedValue
                )))
            .ToList() ?? new List<DataSourceFilter>();

        var dataSourceRequest = new DataSourceRequest
        {
            StartIndex = request.StartIndex,
            Count = request.Count,
            SortByProperty = request.SortByColumn == nameof(BottleListItem.Id) ? null : request.SortByColumn,
            SortByAscending = request.SortByAscending,
            Filters = filters,
            CancellationToken = request.CancellationToken
        };

        var result = await bottlesListResultDataSource.GetAsync(dataSourceRequest);
        var bottleListResult = result.Items.FirstOrDefault();
        var items = bottleListResult?.Items ?? Array.Empty<BottleListItem>();
        _bottles = items;
        TotalPositions = bottleListResult?.TotalPositions ?? 0;
        FreePositions = bottleListResult?.FreePositions ?? 0;
        var totalCount = result.TotalItemCount;

        return GridItemsProviderResult.From(_bottles.ToList(), totalCount);
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

    protected override Task DoLoadAsync()
    {
        bottlesNotificationProvider.BottlesListChanged += OnInventoryBottlesUpdated;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        bottlesNotificationProvider.BottlesListChanged -= OnInventoryBottlesUpdated;
    }

    public Task OnInventoryBottlesUpdated()
    {
        OnPropertyChanged(nameof(Bottles));
        return Task.CompletedTask;
    }

    public static bool HasStatusError(ReagentPositionStatus status)
    {
        return status == ReagentPositionStatus.UndetectedVolume ||
               status == ReagentPositionStatus.DisconnectedBottle ||
               status == ReagentPositionStatus.UnexpectedVolume ||
               status == ReagentPositionStatus.UnknownBottle ||
               status == ReagentPositionStatus.Expired;
    }

    public static string GetStatusText(ReagentPositionStatus status)
    {
        var state = status.ToLocalizedString("Reagent");
        return state;
    }

    public static string GetPositionText(ArticleType? articleType, int position)
    {
        if (articleType != null && position > 0)
        {
            var bottleString = articleType.ToLocalizedString();
            return $"{bottleString} {position}";
        }

        return string.Empty;

    }

    public static string GetQuantity(Quantity quantity)
    {
        if (quantity != null && quantity.Value >= 0)
        {
            var unit = quantity.Unit.ToLocalizedString("Reagent");
            return $"{quantity.Value} {unit}";
        }
        return string.Empty;
    }
}
