using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.Models.Notifications;

namespace Pharos.Client.Common.ViewModels;

/// <summary>
/// View model base for an entity details page. Loads/saves one entity, reacts to update/delete notifications, and owns a UI view model.
/// </summary>
/// <remarks>
/// Pick this when the page needs:
/// - Backend load/save for one entity
/// - UI refresh when the entity is updated or deleted
/// - A separate UI view model instance owned by the page
///
/// Alternatives:
/// <see cref="EntityEditorViewModel{TModel,TKey}"/> (single VM).
/// </remarks>
/// <typeparam name="TViewModel">UI-facing view model instance created and owned by this page view model.</typeparam>
/// <typeparam name="TModel">The model sent to/from the backend.</typeparam>
/// <typeparam name="TKey">The model key type.</typeparam>
public abstract class DetailsPageViewModel<TViewModel, TModel, TKey> : IDisposable where TKey : notnull where TViewModel : ViewModelItem<TModel>
{
    private readonly IDetailsPageDataService<TModel, TKey> _dataService;
    private readonly IDetailsNotificationProvider<TKey>? _notificationProvider;
    private readonly KeyedDebouncer<TKey> _debouncer = new(500);
    private bool _isSubscribedToNotification;

    protected TViewModel? CurrentDetailsViewModel { get; private set; }

    protected DetailsPageViewModel(IDetailsPageDataService<TModel, TKey> dataService, IDetailsNotificationProvider<TKey>? notificationProvider)
    {
        _dataService = dataService;
        _notificationProvider = notificationProvider;
    }

    public bool IsModified => CurrentDetailsViewModel?.IsModified ?? false;

    public void Dispose()
    {
        UnsubscribeFromNotifications();
        _debouncer.Dispose();
        GC.SuppressFinalize(this);
    }

    private void SubscribeToNotifications()
    {
        if (_isSubscribedToNotification)
        {
            return;
        }

        if (_notificationProvider is not null)
        {
            _notificationProvider.ModelUpdated += NotificationProviderOnModelUpdated;
            _notificationProvider.ModelDeleted += NotificationProviderOnModelDeleted;
        }

        OnSubscribeToNotifications();

        _isSubscribedToNotification = true;
    }

    protected virtual void OnSubscribeToNotifications() { }

    private void UnsubscribeFromNotifications()
    {
        if (_notificationProvider is not null)
        {
            _notificationProvider.ModelUpdated -= NotificationProviderOnModelUpdated;
            _notificationProvider.ModelDeleted -= NotificationProviderOnModelDeleted;
        }

        OnUnsubscribeFromNotifications();

        _isSubscribedToNotification = false;
    }

    protected virtual void OnUnsubscribeFromNotifications() { }

    private void NotificationProviderOnModelUpdated(object? sender, TKey id)
    {
        _debouncer.Debounce(id, _ =>
        {
            ModelUpdated?.Invoke(this, id);
            return Task.CompletedTask;
        });
    }

    private void NotificationProviderOnModelDeleted(object? sender, TKey id)
    {
        ModelDeleted?.Invoke(this, id);
    }

    public event EventHandler<TKey>? ModelUpdated;
    public event EventHandler<TKey>? ModelDeleted;

    public async Task<TViewModel> LoadAsync(TKey? id)
    {
        SubscribeToNotifications();

        if (IsNewItem(id))
        {
            CurrentDetailsViewModel = await CreateViewModel(await CreateDefaultModel());
            return CurrentDetailsViewModel;
        }

        var model = await _dataService.GetByIdAsync(id!);
        CurrentDetailsViewModel = await CreateViewModel(model);
        return CurrentDetailsViewModel;
    }

    private static bool IsNewItem(TKey? id)
    {
        return id != null && id.Equals(default(TKey));
    }

    /// <summary>
    ///     The derived class is responsible to create a new instance of the TModel with all the default values set.
    ///     This includes creating any child objects/lists that are needed by the TViewModel.
    /// </summary>
    /// <returns>A new instance of the TModel</returns>
    protected abstract Task<TModel> CreateDefaultModel();

    /// <summary>
    ///     The derived class is responsible to create a new instance of the TViewModel with the given TModel.
    ///     The derived class can also load additional data that is needed by the TViewModel.
    /// </summary>
    /// <param name="model">The underlying model for the TViewModel</param>
    /// <returns>A new instance if the TViewModel</returns>
    protected abstract Task<TViewModel> CreateViewModel(TModel model);

    public async Task<(TKey Id, bool IsNew)> SaveAsync(TModel model, TKey id)
    {
        if (IsNewItem(id))
        {
            var newId = await _dataService.CreateAsync(model);
            SetAsUnmodified();
            return (newId, true);
        }

        await _dataService.UpdateAsync(model);
        SetAsUnmodified();
        return (id, false);
    }

    private void SetAsUnmodified()
    {
        CurrentDetailsViewModel?.SetAsUnmodified();
    }
}

public abstract class DetailsPageViewModel<TViewModel, TModel> : DetailsPageViewModel<TViewModel, TModel, int> where TViewModel : ViewModelItem<TModel>
{
    protected DetailsPageViewModel(IDetailsPageDataService<TModel> dataService, IDetailsNotificationProvider<int>? notificationProvider) : base(dataService, notificationProvider)
    {
    }
}