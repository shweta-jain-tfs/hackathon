using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Models.DataGrid;
using Pharos.Common.Web.Utilities;

namespace Pharos.Client.Common.ViewModels;

/// <summary>
/// View model base for virtualized (lazy-loading) grids that load paged results on demand.
/// </summary>
/// <remarks>
/// Pick this when the UI needs:
/// - Paging/virtualization (large datasets)
/// - Filtering/sorting at the data source level (not in-memory)
/// - Load-on-scroll via ProvideGridItems
/// - Refresh when created/updated/deleted notifications affect visible items
///
/// Notes:
/// - Rows may be recycled or recreated. Do not rely on per-row view model instances staying alive.
/// - Prefer binding rows directly to models. If you add a row wrapper, keep it stateless and
///   fully reconstructible from the model.
///
/// Alternatives:
/// <see cref="EntityListViewModel{TModel,TKey}"/> (in-memory list; manual refresh).
/// <see cref="DynamicEntityListViewModel{TModel,TKey}"/> (in-memory list; live updates).
/// <see cref="ListViewModel{TViewModel,TModel}"/> (non-virtualized; item view models).
/// </remarks>
/// <typeparam name="TModel">The entity model type displayed in the grid.</typeparam>
/// <typeparam name="TKey">The entity key type used for notifications and fetched-item tracking.</typeparam>
public abstract class VirtualGridViewModel<TModel, TKey> : IDisposable
    where TModel : IEntityModel<TKey>
{
    private readonly IPagedDataSource<TModel, TKey> _dataSource;
    private readonly IExceptionHandlerService _exceptionHandler;
    private readonly IEntityNotificationProvider<TModel, TKey>? _notificationProvider;
    private readonly AsyncThrottler _throttler;
    private bool _isSubscribedToNotifications;

    private Task<GridItemsProviderResult<TModel>>? _provideGridItemsTask;
    private HashSet<TKey> _fetchedIds;

    public event EventHandler? ViewRefreshNeeded;

    protected VirtualGridViewModel(
        IPagedDataSource<TModel, TKey> dataSource,
        IExceptionHandlerService exceptionHandler,
        IEntityNotificationProvider<TModel, TKey>? notificationProvider = null)
    {
        _dataSource = dataSource;
        _exceptionHandler = exceptionHandler;
        _notificationProvider = notificationProvider;
        _fetchedIds = [];
        _throttler = new(AsyncThrottler.DefaultThrottleWindow, name: $"{GetType().Name}.{nameof(OnViewRefreshNeeded)}");
    }

    public void Dispose()
    {
        UnsubscribeFromNotifications();
        _throttler.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask<GridItemsProviderResult<TModel>> ProvideGridItems(GridItemsProviderRequest<TModel> request)
    {
        if (_provideGridItemsTask != null)
        {
            await _provideGridItemsTask.WaitAsync(request.CancellationToken);
        }

        _provideGridItemsTask = InternalProvideGridItems(request);
        try
        {
            var result = await _provideGridItemsTask;
            _fetchedIds = result.Items.Select(x => x.Id).ToHashSet();
            return result;
        }
        catch (Exception ex)
        {
            _exceptionHandler.ShowExceptionMessage(ex);

            return new GridItemsProviderResult<TModel>
            {
                Items = new List<TModel>(),
                TotalItemCount = request.StartIndex + (request.Count ?? 0),
            };
        }
        finally
        {
            _provideGridItemsTask = null;
        }
    }

    private async Task<GridItemsProviderResult<TModel>> InternalProvideGridItems(GridItemsProviderRequest<TModel> request)
    {
        var dsRequest = ToDataSourceRequest(request);
        var result = await _dataSource.GetAsync(dsRequest);

        InitiateSubscriptions();
        return new GridItemsProviderResult<TModel>
        {
            Items = result.Items,
            TotalItemCount = result.TotalItemCount,
            GroupedCount = result.GroupedCount
        };
    }
    
    private void InitiateSubscriptions()
    {
        if (!_isSubscribedToNotifications)
        {
            SubscribeToNotifications();
            _isSubscribedToNotifications = true;
        }
    }

    protected virtual void SubscribeToNotifications()
    {
        if (_notificationProvider != null)
        {
            _notificationProvider.Created += ItemCreated;
            _notificationProvider.Updated += ItemChanged;
            _notificationProvider.Deleted += ItemChanged;
        }
    }

    protected void OnViewRefreshNeeded()
    {
        // Throttle view refreshes to prevent notification bursts from overloading the UI.
        _throttler.Push(() => ViewRefreshNeeded?.Invoke(this, EventArgs.Empty));
    }

    private Task ItemChanged(TKey id)
    {
        if (_fetchedIds.Contains(id))
        {
            OnViewRefreshNeeded();
        }
        return Task.CompletedTask;
    }

    private Task ItemCreated(TKey id)
    {
        OnViewRefreshNeeded();
        return Task.CompletedTask;
    }

    protected virtual void UnsubscribeFromNotifications()
    {
        if (_notificationProvider != null)
        {
            _notificationProvider.Created -= ItemCreated;
            _notificationProvider.Updated -= ItemChanged;
            _notificationProvider.Deleted -= ItemChanged;
        }
    }

    protected bool IsItemFetched(TKey id)
    {
        return _fetchedIds.Contains(id);
    }

    protected DataSourceRequest ToDataSourceRequest(GridItemsProviderRequest<TModel> request)
    {
        return request.ToDataSourceRequest(_dataSource is ISupportsRowLimitBypass);
    }
}

/// <summary>
/// Convenience base for <see cref="VirtualGridViewModel{TModel,TKey}"/> with an <see cref="int"/> key.
/// </summary>
/// <typeparam name="TModel">The entity model type displayed in the grid.</typeparam>
public abstract class VirtualGridViewModel<TModel>
    (IPagedDataSource<TModel, int> dataSource, IExceptionHandlerService exceptionHandler, IEntityNotificationProvider<TModel, int>? notificationProvider = null)
    : VirtualGridViewModel<TModel, int>(dataSource, exceptionHandler, notificationProvider) where TModel : IEntityModel<int>;