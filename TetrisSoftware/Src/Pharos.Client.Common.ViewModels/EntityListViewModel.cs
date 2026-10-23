using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.Common.ViewModels;

/// <summary>
/// View model base for an in-memory list of entity models that is refreshed on demand.
/// </summary>
/// <remarks>
/// Pick this when the UI needs:
/// - A full in-memory list of models as the binding source (ObservableCollection)
/// - Manual refresh via LoadAsync
/// - Simple busy state while loading
///
/// Alternatives:
/// <see cref="VirtualGridViewModel{TModel,TKey}"/> (paging/virtualization).
/// <see cref="ListViewModel{TViewModel,TModel}"/> (maps models to item view models).
/// <see cref="DynamicEntityListViewModel{TModel,TKey}"/> (create/update/delete notifications).
/// </remarks>
/// <typeparam name="TModel">The entity model type stored in the list.</typeparam>
/// <typeparam name="TKey">The entity key type.</typeparam>
public class EntityListViewModel<TModel, TKey> : ObservableCollection<TModel>, IListViewModel<TModel>
    where TModel : IEntityModel<TKey>
{
    private bool _notificationsEnabled = true;

    public EntityListViewModel(IEntityDataSource<TModel, TKey> dataSource)
    {
        DataSource = dataSource;
    }

    public bool IsBusy
    {
        get;
        protected set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsBusy)));
            }
        }
    }

    public bool NeedsReload
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(NeedsReload)));
            }
        }
    }

    protected IEntityDataSource<TModel, TKey> DataSource { get; }

    public virtual async Task LoadAsync()
    {
        CheckIsBusy();
        IsBusy = true;
        try
        {
            var result = await DataSource.GetAsync(new DataSourceRequest
            {
                CancellationToken = CancellationToken.None
            });

            ReloadObservableCollection(result.Items);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected void CheckIsBusy()
    {
        if (IsBusy)
        {
            throw new InvalidOperationException("Concurrent updates of the view-model are not allowed.");
        }
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (_notificationsEnabled)
        {
            base.OnCollectionChanged(e);
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (_notificationsEnabled)
        {
            base.OnPropertyChanged(e);
        }
    }

    protected void ReloadObservableCollection(IEnumerable<TModel> dataModels)
    {
        _notificationsEnabled = false;
        try
        {
            CheckReentrancy();
            ClearItems();
            foreach (var model in dataModels)
            {
                Add(model);
            }
        }
        finally
        {
            _notificationsEnabled = true;
        }

        OnPropertyChanged(EventArgsCache.CountPropertyChanged);
        OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);
        OnCollectionChanged(EventArgsCache.ResetCollectionChanged);
    }
}

/// <summary>
/// Convenience base for <see cref="EntityListViewModel{TModel,TKey}"/> with an <see cref="int"/> key.
/// </summary>
/// <typeparam name="TModel">The entity type stored in the list.</typeparam>
public class EntityListViewModel<TModel>(IEntityDataSource<TModel, int> dataSource) : EntityListViewModel<TModel, int>(dataSource) where TModel : IEntityModel<int>;

internal static class EventArgsCache
{
    public static readonly PropertyChangedEventArgs CountPropertyChanged = new("Count");
    public static readonly PropertyChangedEventArgs IndexerPropertyChanged = new("Item[]");
    public static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged = new(NotifyCollectionChangedAction.Reset);
}
