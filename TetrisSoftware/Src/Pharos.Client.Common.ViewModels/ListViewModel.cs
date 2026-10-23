using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.Models.Notifications;

namespace Pharos.Client.Common.ViewModels;

/// <summary>
/// View model base for non-virtualized lists that load all models and wrap each item in an item view model.
/// </summary>
/// <remarks>
/// Pick this when the UI needs:
/// - A full in-memory list of item view models (one <see cref="ViewModelItem{TModel}"/> per model)
/// - Per-row UI state on the item view model (for example dirty tracking)
///
/// Not intended for virtualized/lazy-loading UI, where item view models may be created and dropped as you scroll.
///
/// Alternatives:
/// <see cref="EntityListViewModel{TModel,TKey}"/> (bind models directly).
/// <see cref="VirtualGridViewModel{TModel,TKey}"/> (paging/virtualization).
/// </remarks>
/// <typeparam name="TViewModel">Item view model type created per model.</typeparam>
/// <typeparam name="TModel">Model type loaded from the items source.</typeparam>
public abstract class ListViewModel<TViewModel, TModel> : List<TViewModel>, IDisposable where TViewModel : ViewModelItem<TModel>
{
    private readonly IItemsSource<TModel> _itemsSource;
    private readonly IRefreshNotificationProvider? _refreshNotificationProvider;

    protected ListViewModel(IItemsSource<TModel> itemsSource, IRefreshNotificationProvider? refreshNotificationProvider)
    {
        _itemsSource = itemsSource;
        if (refreshNotificationProvider != null)
        {
            _refreshNotificationProvider = refreshNotificationProvider;
            _refreshNotificationProvider.RefreshNeeded += OnRefreshNeeded;
        }
    }

    private async void OnRefreshNeeded(object? sender, EventArgs e)
    {
        await LoadAsync();
    }

    public async Task LoadAsync()
    {
        var models = await _itemsSource.GetAsync();

        Clear();

        foreach (var model in models)
        {
            Add(CreateViewModel(model));
        }

        OnViewRefreshNeeded();
    }

    public event EventHandler? ViewRefreshNeeded;

    protected abstract TViewModel CreateViewModel(TModel model);

    public void Dispose()
    {
        Disposing();
        if (_refreshNotificationProvider != null)
        {
            _refreshNotificationProvider.RefreshNeeded -= OnRefreshNeeded;
        }
        GC.SuppressFinalize(this);
    }

    protected virtual void Disposing()
    {
    }

    protected void OnViewRefreshNeeded()
    {
        ViewRefreshNeeded?.Invoke(this, EventArgs.Empty);
    }
}