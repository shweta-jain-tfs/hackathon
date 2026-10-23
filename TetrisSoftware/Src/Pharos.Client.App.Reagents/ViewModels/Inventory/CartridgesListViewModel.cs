using Pharos.Client.App.Reagents.Models.Inventory;
using Pharos.Client.App.Reagents.Models.Inventory.Cartridges;
using Pharos.Client.Common.App.Extensions;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Client.App.Reagents.ViewModels.Inventory;

internal class CartridgesListViewModel : LoadingViewModel, IDisposable
{
    private readonly IEntityDataSource<CartridgeListResult> _cartridgeListResultDataSource;
    private readonly ICartridgesNotificationProvider _cartridgesNotificationProvider;
    private readonly IReagentDataSource _reagentDataSource;
    private IEnumerable<CartridgeListItem> _cartridges = [];

    public CartridgesListViewModel(
        IEntityDataSource<CartridgeListResult> cartridgeListResultDataSource,
        ICartridgesNotificationProvider cartridgesNotificationProvider,
        IReagentDataSource reagentDataSource)
    {
        _cartridgeListResultDataSource = cartridgeListResultDataSource;
        _cartridgesNotificationProvider = cartridgesNotificationProvider;
        _reagentDataSource = reagentDataSource;
    }

    #region Public/Protected Members

    public HashSet<CartridgeListItem>? SelectedCartridges { get; private set; } = new();

    public bool CanUnloadSelectedCartridges
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(CanUnloadSelectedCartridges));
            }
        }
    }

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

    public int FreePositionsFront
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(FreePositionsFront));
            }
        }
    }

    public int TotalPositionsFront
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(TotalPositionsFront));
            }
        }
    }

    public int FreePositionsBack
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(FreePositionsBack));
            }
        }
    }

    public int TotalPositionsBack
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(nameof(TotalPositionsBack));
            }
        }
    }

    public event Action? GridRefreshRequested;

    protected override Task DoLoadAsync()
    {
        _cartridgesNotificationProvider.CartridgesListChanged += OnCartridgesListChanged;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _cartridgesNotificationProvider.CartridgesListChanged -= OnCartridgesListChanged;
    }

    public Task OnCartridgesListChanged()
    {
        RequestGridRefresh();
        UpdateCanUnloadSelectedCartridges();
        return Task.CompletedTask;
    }

    public void OnSelectedCartridgesChanged(HashSet<CartridgeListItem> selected)
    {
        SelectedCartridges = selected;
        UpdateCanUnloadSelectedCartridges();
    }

    public async ValueTask<GridItemsProviderResult<CartridgeListItem>> ProvideItems(GridItemsProviderRequest<CartridgeListItem> request)
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
            SortByProperty = request.SortByColumn == nameof(CartridgeListItem.Id) ? null : request.SortByColumn,
            SortByAscending = request.SortByAscending,
            Filters = filters,
            RequiresRowLimitBypass = request.RequiresRowLimitBypass,
            CancellationToken = request.CancellationToken
        };

        var result = await _cartridgeListResultDataSource.GetAsync(dataSourceRequest);
        var cartridgeListResult = result.Items.FirstOrDefault();
        var items = cartridgeListResult?.Items ?? Array.Empty<CartridgeListItem>();
        _cartridges = items;
        ClearSelection();
        TotalCount = result.TotalItemCount;
        TotalQuantity = cartridgeListResult?.TotalQuantity ?? 0;
        FreePositionsFront = cartridgeListResult?.FreePositionsFront ?? 0;
        TotalPositionsFront = cartridgeListResult?.TotalPositionsFront ?? 0;
        FreePositionsBack = cartridgeListResult?.FreePositionsRear ?? 0;
        TotalPositionsBack = cartridgeListResult?.TotalPositionsRear ?? 0;
        UpdateCanUnloadSelectedCartridges();
        return GridItemsProviderResult.From(items.ToList(), TotalCount);
    }

    public async Task UnloadSelectedCartridgesAsync(HashSet<int> unloadCartridgesIds)
    {
        var unloadCartridges = _cartridges.Where(x => unloadCartridgesIds.Contains(x.Id)).ToList();
        if (unloadCartridges.Count == 0)
        {
            return;
        }
        var unloadReagentRequestItems = MapUnloadCartridgeToReagentRequest(unloadCartridges);
        await _reagentDataSource.UnloadReagentAsync(unloadReagentRequestItems);
    }
    public async Task AbortUnloadCartridgesAsync(HashSet<int> unloadCartridgesIds)
    {
        var abortCartridges = _cartridges.Where(x => unloadCartridgesIds.Contains(x.Id)).ToList();
        if (abortCartridges.Count == 0)
        {
            return;
        }
        var unloadReagentRequestItems = MapUnloadCartridgeToReagentRequest(abortCartridges, true);
        await _reagentDataSource.UnloadReagentAsync(unloadReagentRequestItems);
    }

    internal bool IsUnloadSelectedCartridges()
    {
        if (SelectedCartridges?.Count > 0)
        {
            var updatedCartridges = _cartridges.Where(x => SelectedCartridges.Any(s => s.Id == x.Id)).ToHashSet();
            return updatedCartridges.Any(x => !IsInUnloadProhibitedState(x.Status));
        }

        return false;
    }

    internal HashSet<CartridgeListItem> GetCurrentItemSelection()
    {
        if (SelectedCartridges?.Count > 0)
        {
            return _cartridges.Where(x => SelectedCartridges.Any(s => s.Id == x.Id)).ToHashSet();
        }

        return new();
    }

    public bool ShouldDisplayUnits()
    {
        return _cartridges.GroupBy(c => c.Quantity.Unit).Count() == 1;
    }

    public string GetUnitsDisplayText()
    {
        return _cartridges.FirstOrDefault()?.Quantity.Unit.ToLocalizedString("Reagent") ?? string.Empty;
    }

    #endregion

    #region Private Members
    private void ClearSelection()
    {
        // Build a set of current cartridge IDs for fast lookup
        var currentCartridgeIds = _cartridges.Select(c => c.Id).ToHashSet();

        // Remove CartridgeListItems from SelectedCartridges that are no longer present
        SelectedCartridges?.RemoveWhere(cartridge => !currentCartridgeIds.Contains(cartridge.Id));
    }

    private void UpdateCanUnloadSelectedCartridges()
    {
        CanUnloadSelectedCartridges = IsUnloadSelectedCartridges();
    }

    private void RequestGridRefresh()
    {
        GridRefreshRequested?.Invoke();
    }
    #endregion

    #region Static Methods Members

    public static bool HasStatusError(ReagentPositionStatus status)
    {
        return status == ReagentPositionStatus.TransferNotPossible ||
               status == ReagentPositionStatus.UnreadableBarcode ||
               status == ReagentPositionStatus.AspirationError ||
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
        return status.ToLocalizedString("Reagent");
    }

    public static string GetExpiryDateOpenCell(ExpiryDateOpenCell expiryDateOpenCell)
    {
        if (expiryDateOpenCell is { HasPiercedCell: true })
        {
            var formattedDate = expiryDateOpenCell.ExpiryDate?.ToShortDateString();
            return $"{formattedDate} ({GetQuantity(expiryDateOpenCell.Quantity)})";
        }

        return "-";
    }

    public static string GetQuantity(Quantity quantity)
    {
        var unit = quantity.Unit.ToLocalizedString("Reagent");
        return $"{quantity.Value} {unit}";
    }

    public static string GetPosition(int position, CartridgeStorageArea storageArea)
    {
        var localized = storageArea.ToLocalizedString("Reagent");
        return storageArea == CartridgeStorageArea.Unspecified
            ? string.Empty
            : string.Concat(localized, "-", position);
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

    private static List<UnloadReagentRequestItem> MapUnloadCartridgeToReagentRequest(List<CartridgeListItem> unloadCartridges, bool abortUnload = false)
    {
        return unloadCartridges.Select(cartridge => new UnloadReagentRequestItem
        {
            Barcode = cartridge.Barcode,
            InstrumentNumber = cartridge.InstrumentNumber,
            Position = cartridge.Position,
            AbortUnload = abortUnload,
            StorageArea = GetReagentStorageArea(cartridge.StorageArea),
        }).ToList();
    }

    private static ReagentStorageArea GetReagentStorageArea(CartridgeStorageArea storageArea)
    {
        return storageArea switch
        {
            CartridgeStorageArea.BackStorage => ReagentStorageArea.RearCartridgeStorageArea,
            CartridgeStorageArea.FrontStorage => ReagentStorageArea.FrontCartridgeStorageArea,
            _ => (ReagentStorageArea)storageArea
        };
    }
    #endregion
}
