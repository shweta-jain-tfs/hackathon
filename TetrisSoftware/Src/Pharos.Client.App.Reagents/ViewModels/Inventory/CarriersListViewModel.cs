using Pharos.Client.App.Reagents.Models.Inventory;
using Pharos.Client.App.Reagents.Models.Inventory.Carriers;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Client.App.Reagents.ViewModels.Inventory;

internal class CarriersListViewModel : LoadingViewModel, IDisposable
{
    private readonly IEntityDataSource<CarrierListResult> _carrierListResultDataSource;
    private readonly ICarriersNotificationProvider _carriersNotificationProvider;
    private readonly IReagentDataSource _reagentDataSource;
    private IEnumerable<CarrierListItem> _carriers = [];

    public CarriersListViewModel(
        IEntityDataSource<CarrierListResult> carrierListResultDataSource,
        ICarriersNotificationProvider carriersNotificationProvider,
        IReagentDataSource reagentDataSource)
    {
        _carrierListResultDataSource = carrierListResultDataSource;
        _carriersNotificationProvider = carriersNotificationProvider;
        _reagentDataSource = reagentDataSource;
    }

    #region Public/Protected Methods Members

    public HashSet<CarrierListItem>? SelectedCarriers { get; private set; } = new();

    public int TotalCount
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(TotalCount));
            }
        }
    }

    public int TotalQuantity
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(TotalQuantity));
            }
        }
    }

    public int FreePositions
    {
        get;
        set
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
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(TotalPositions));
            }
        }
    }

    public bool CanUnloadSelectedCarriers
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(CanUnloadSelectedCarriers));
            }
        }
    }

    public event Action? GridRefreshRequested;

    protected override Task DoLoadAsync()
    {
        _carriersNotificationProvider.CarriersListChanged += OnInventoryCarriersUpdated;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _carriersNotificationProvider.CarriersListChanged -= OnInventoryCarriersUpdated;
    }

    public Task OnInventoryCarriersUpdated()
    {
        RequestGridRefresh();
        UpdateCanUnloadSelectedCarriers();
        return Task.CompletedTask;
    }

    public void OnSelectedCarriersChanged(HashSet<CarrierListItem> selected)
    {
        SelectedCarriers = selected;
        UpdateCanUnloadSelectedCarriers();
    }

    public async ValueTask<GridItemsProviderResult<CarrierListItem>> ProvideItems(GridItemsProviderRequest<CarrierListItem> request)
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
            SortByProperty = request.SortByColumn == nameof(CarrierListItem.Id) ? null : request.SortByColumn,
            SortByAscending = request.SortByAscending,
            Filters = filters,
            RequiresRowLimitBypass = request.RequiresRowLimitBypass,
            CancellationToken = request.CancellationToken
        };

        var result = await _carrierListResultDataSource.GetAsync(dataSourceRequest);
        var carrierListResult = result.Items.FirstOrDefault();
        var items = carrierListResult?.Items ?? Array.Empty<CarrierListItem>();
        _carriers = items;
        UpdateSelection();
        TotalCount = result.TotalItemCount;
        TotalQuantity = carrierListResult?.TotalQuantity ?? 0;
        FreePositions = carrierListResult?.FreePositions ?? 0;
        TotalPositions = carrierListResult?.TotalPositions ?? 0;
        UpdateCanUnloadSelectedCarriers();
        return GridItemsProviderResult.From(items.ToList(), TotalCount);
    }

    public async Task UnloadSelectedCarriersAsync(HashSet<int> unloadCarriersIds)
    {
        var unloadCarriers = _carriers.Where(x => unloadCarriersIds.Contains(x.Id)).ToList();
        if (unloadCarriers.Count == 0)
        {
            return;
        }
        var unloadReagentRequestItems = MapUnloadCarrierToReagentRequest(unloadCarriers);
        await _reagentDataSource.UnloadReagentAsync(unloadReagentRequestItems);
    }

    internal async Task AbortUnloadCarriersAsync(HashSet<int> unloadCarriersIds)
    {
        var abortCarriers = _carriers.Where(x => unloadCarriersIds.Contains(x.Id)).ToList();
        if (abortCarriers.Count == 0)
        {
            return;
        }
        var unloadReagentRequestItems = MapUnloadCarrierToReagentRequest(abortCarriers, true);
        await _reagentDataSource.UnloadReagentAsync(unloadReagentRequestItems);
    }

    internal bool IsUnloadSelectedCarriers()
    {
        if (SelectedCarriers?.Count > 0)
        {
            var updatedCarriers = _carriers.Where(x => SelectedCarriers.Any(s => s.Id == x.Id)).ToHashSet();
            return updatedCarriers.Any(x => !IsInUnloadProhibitedState(x.Status));
        }

        return false;
    }

    internal HashSet<CarrierListItem> GetCurrentItemSelection()
    {
        if (SelectedCarriers?.Count > 0)
        {
            return _carriers.Where(x => SelectedCarriers.Any(s => s.Id == x.Id)).ToHashSet();
        }

        return [];
    }

    #endregion

    #region Private Methods Members

    private void UpdateSelection()
    {
        // Build a set of current carrier IDs for fast lookup
        var currentCarrierIds = _carriers.Select(c => c.Id).ToHashSet();

        // Remove CarrierListItems from SelectedCarriers that are no longer present
        SelectedCarriers?.RemoveWhere(carrier => !currentCarrierIds.Contains(carrier.Id));
    }

    private void RequestGridRefresh()
    {
        GridRefreshRequested?.Invoke();
    }

    private void UpdateCanUnloadSelectedCarriers()
    {
        CanUnloadSelectedCarriers = IsUnloadSelectedCarriers();
    }

    #endregion

    #region Static Methods Members

    public static string GetQuantity(Quantity quantity)
    {
        var unit = quantity.Unit.ToLocalizedString("Reagent");
        return $"{quantity.Value} {unit}";
    }

    public static bool HasStatusError(ReagentPositionStatus status)
    {
        return status == ReagentPositionStatus.TransferNotPossible ||
               status == ReagentPositionStatus.DispenseError ||
               status == ReagentPositionStatus.UnreadableBarcode ||
               status == ReagentPositionStatus.Expired;
    }

    public static bool IsInUnloadProhibitedState(ReagentPositionStatus status)
    {
        return status == ReagentPositionStatus.Reserved
            || status == ReagentPositionStatus.Unloading
            || status == ReagentPositionStatus.TransferNotPossible;
    }

    public static string GetStatusText(ReagentPositionStatus status)
    {
        var state = status.ToLocalizedString("Reagent");
        return state;
    }

    public static string GetTypeText(ArticleType articleType)
    {
        var state = articleType.ToLocalizedString();
        return state;
    }

    private static List<UnloadReagentRequestItem> MapUnloadCarrierToReagentRequest(List<CarrierListItem> unloadCarriers, bool abortUnload = false)
    {
        return unloadCarriers.Select(carrier => new UnloadReagentRequestItem
        {
            Barcode = carrier.Barcode,
            Udi = carrier.Udi,
            InstrumentNumber = carrier.InstrumentNumber,
            Position = carrier.Position,
            AbortUnload = abortUnload,
            StorageArea = ReagentStorageArea.InternalCarrierStorageArea
        }).ToList();
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

    #endregion
}
