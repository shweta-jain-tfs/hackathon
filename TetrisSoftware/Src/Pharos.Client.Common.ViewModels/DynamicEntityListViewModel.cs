using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.Models.Notifications;

namespace Pharos.Client.Common.ViewModels;

/// <summary>
/// View model base for an in-memory entity list that stays up to date via create/update/delete notifications.
/// </summary>
/// <remarks>
/// Pick this when the UI needs:
/// - A full in-memory list of models as the binding source
/// - Automatic add/replace/remove when the backend changes
///
/// Alternatives:
/// <see cref="EntityListViewModel{TModel,TKey}"/> (manual refresh).
/// <see cref="VirtualGridViewModel{TModel,TKey}"/> (paging/virtualization).
/// </remarks>
/// <typeparam name="TModel">The entity model type stored in the list.</typeparam>
/// <typeparam name="TKey">The entity key type used for notifications and lookups.</typeparam>
public class DynamicEntityListViewModel<TModel, TKey> : EntityListViewModel<TModel, TKey>, IDisposable
    where TModel : IEntityModel<TKey>
{
    private readonly IEntityNotificationProvider<TModel, TKey> _notificationProvider;

    public DynamicEntityListViewModel(
        IEntityDataSource<TModel, TKey> dataService,
        IEntityNotificationProvider<TModel, TKey> notificationProvider) : base(dataService)
    {
        _notificationProvider = notificationProvider;
    }

    public override async Task LoadAsync()
    {
        CheckIsBusy();
        UnsubscribeFromNotifications();
        try
        {
            await base.LoadAsync();
            NeedsReload = false;
            SubscribeToNotifications();
        }
        catch
        {
            NeedsReload = true;
            throw;
        }
    }

    private int GetIndexOf(TKey id)
    {
        var existingModel = Items.SingleOrDefault(x => EqualityComparer<TKey>.Default.Equals(x.Id, id));
        if (existingModel == null)
        {
            return -1;
        }

        return IndexOf(existingModel);
    }

    private Task OnDeleted(TKey id)
    {
        var index = GetIndexOf(id);
        if (index >= 0)
        {
            RemoveAt(index);
        }
        return Task.CompletedTask;
    }

    private async Task OnUpdated(TKey id)
    {
        var index = GetIndexOf(id);
        if (index >= 0)
        {
            var model = await DataSource.GetByIdAsync(id);
            this[index] = model;
        }
    }

    private async Task OnCreated(TKey id)
    {
        var model = await DataSource.GetByIdAsync(id);
        Add(model);
    }

    private void OnError(object? sender, Exception e)
    {
        NeedsReload = true;
    }

    private void SubscribeToNotifications()
    {
        _notificationProvider.Error += OnError;
        _notificationProvider.Created += OnCreated;
        _notificationProvider.Updated += OnUpdated;
        _notificationProvider.Deleted += OnDeleted;
    }

    private void UnsubscribeFromNotifications()
    {
        _notificationProvider.Error -= OnError;
        _notificationProvider.Created -= OnCreated;
        _notificationProvider.Updated -= OnUpdated;
        _notificationProvider.Deleted -= OnDeleted;
    }

    public void Dispose()
    {
        UnsubscribeFromNotifications();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Convenience base for <see cref="DynamicEntityListViewModel{TModel,TKey}"/> with an <see cref="int"/> key.
/// </summary>
/// <typeparam name="TModel">The entity model type stored in the list.</typeparam>
public class DynamicEntityListViewModel<TModel>
    (IEntityDataSource<TModel, int> dataService, IEntityNotificationProvider<TModel, int> notificationProvider)
    : DynamicEntityListViewModel<TModel, int>(dataService, notificationProvider) where TModel : IEntityModel<int>;