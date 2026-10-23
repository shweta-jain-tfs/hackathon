using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.Common.ViewModels;

/// <summary>
/// View model base for editing a single entity with backend load/save and modification tracking.
/// </summary>
/// <remarks>
/// Pick this when the UI edits one entity and needs:
/// - Backend load/save for that entity
/// - IsModified tracking (for save/apply UI)
/// - Optional reload when the entity is updated elsewhere
///
/// Alternatives:
/// <see cref="DetailsPageViewModel{TViewModel,TModel,TKey}"/> (separate page-owned UI view model, includes delete notifications).
/// <see cref="LoadingViewModel"/> (loading only).
/// </remarks>
/// <typeparam name="TModel">The entity model being edited.</typeparam>
/// <typeparam name="TKey">The entity key type.</typeparam>
public abstract class EntityEditorViewModel<TModel, TKey> : LoadingViewModel, IEntityEditorViewModel<TModel, TKey>
    where TModel : class, IEntityModel<TKey>, new()
{
    private readonly IEntityNotificationProvider<TModel, TKey>? _notificationProvider;

    protected EntityEditorViewModel(IEntityDataService<TModel, TKey> dataService, IEntityNotificationProvider<TModel, TKey>? notificationProvider = null)
    {
        DataService = dataService;
        _notificationProvider = notificationProvider;

        if (_notificationProvider is not null)
        {
            _notificationProvider.Updated += NotificationProviderOnUpdated;
        }
    }

    public bool IsLoaded { get; set; }

    protected IEntityDataService<TModel, TKey> DataService { get; }

    public TModel Entity
    {
        get
        {
            field ??= CreateNewEntity();
            return field;
        }
        private set
        {
            field = value;
            OnPropertyChanged(nameof(Entity));
        }
    }

    public bool IsNewEntity { get; private set; }

    public bool IsModified
    {
        get;
        protected set
        {
            if (value == field)
            {
                return;
            }

            field = value;
            OnPropertyChanged(nameof(IsModified));
        }
    }

    public async Task LoadAsync(TKey entityId)
    {
        CheckIsBusy();
        IsBusy = true;
        try
        {
            await DoLoadAsync(entityId);
        }
        catch (ApiException e)
        {
            if (e.StatusCode != 404)
            {
                NeedsReload = true;
            }

            throw;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public virtual async Task<bool> SaveAsync()
    {
        if (!IsModified)
        {
            return false;
        }

        try
        {
            if (IsNewEntity)
            {
                var entityId = await DataService.CreateAsync(Entity);
                Entity.Id = entityId;
            }
            else
            {
                await DataService.UpdateAsync(Entity);
            }

            IsModified = false;
        }
        finally
        {
            NeedsReload = true;
        }

        return true;
    }

    public void Dispose()
    {
        UnsubscribeFromNotifications();
        GC.SuppressFinalize(this);
    }

    protected virtual void UnsubscribeFromNotifications()
    {
        if (_notificationProvider is not null)
        {
            _notificationProvider.Updated -= NotificationProviderOnUpdated;
        }
    }

    private async Task NotificationProviderOnUpdated(TKey id)
    {
        if (Equals(Entity.Id, id))
        {
            await DoLoadAsync(Entity.Id);
            OnPropertyChanged(nameof(IEditorViewModel.IsModified));
        }
    }

    protected override Task DoLoadAsync()
    {
        Entity = CreateNewEntity();
        IsNewEntity = true;
        IsModified = false;
        IsLoaded = true;
        return Task.CompletedTask;
    }

    protected virtual async Task DoLoadAsync(TKey entityId)
    {
        Entity = await DataService.GetByIdAsync(entityId);
        IsNewEntity = false;
        NeedsReload = false;
        IsModified = false;
        IsLoaded = true;
    }

    private static TModel CreateNewEntity()
    {
        return new TModel();
    }
}

/// <summary>
/// Convenience base for <see cref="EntityEditorViewModel{TModel,TKey}"/> with an <see cref="int"/> key.
/// </summary>
/// <typeparam name="TModel">The entity model being edited.</typeparam>
public abstract class EntityEditorViewModel<TModel>
    (IEntityDataService<TModel> dataService, IEntityNotificationProvider<TModel>? notificationProvider = null)
    : EntityEditorViewModel<TModel, int>(dataService, notificationProvider), IEntityEditorViewModel<TModel>
    where TModel : class, IEntityModel<int>, new();
