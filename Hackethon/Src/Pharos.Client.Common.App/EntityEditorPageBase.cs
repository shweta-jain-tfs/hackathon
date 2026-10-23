using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Breadcrumb;

namespace Pharos.Client.Common.App;

public abstract class EntityEditorPageBase<TViewModel, TModel, TKey> : ComponentBase, IDisposable where TViewModel : class, IEntityEditorViewModel<TModel, TKey> where TModel : IEntityModel<TKey>
{
    private bool _saving;

    [Parameter] public TKey? Id { get; set; }

    [Inject] protected TViewModel ViewModel { get; set; } = default!;

    protected TModel Entity => ViewModel.Entity;

    protected abstract List<BreadcrumbItem> Breadcrumbs { get; }

    public event EventHandler<bool>? OnAfterLoaded;

    protected async Task DiscardChangesAsync()
    {
        await InitializeAsync();
    }

    protected abstract void NavigateToParentList();

    protected override async Task OnInitializedAsync()
    {
        await InitializeAsync();

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected async Task SaveAsync()
    {
        _saving = true;
        try
        {
            if (await ViewModel.SaveAsync())
            {
                Id = Entity.Id;
                await ViewModel.LoadAsync(Entity.Id);
                await InvokeAsync(StateHasChanged);
                OnAfterLoaded?.Invoke(this, false);
            }
        }
        finally
        {
            _saving = false;
        }
    }

    protected async Task InitializeAsync()
    {
        try
        {
            if (Id != null && !Id.Equals(default(TKey)))
            {
                await ViewModel.LoadAsync(Id);
                OnAfterLoaded?.Invoke(this, false);
            }
            else
            {
                await ViewModel.LoadAsync();
                OnAfterLoaded?.Invoke(this, true);
            }
        }
        catch (ApiException e)
        {
            if (e.StatusCode != 404)
            {
                throw;
            }

            NavigateToParentList();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_saving)
        {
            return;
        }

        if (e.PropertyName == nameof(IEditorViewModel.IsModified))
        {
            InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        UnsubscribeFromNotifications();

        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }

    protected virtual void UnsubscribeFromNotifications()
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }
}

public abstract class EntityEditorPageBase<TViewModel, TModel> : EntityEditorPageBase<TViewModel, TModel, int> where TViewModel : class, IEntityEditorViewModel<TModel> where TModel : IEntityModel<int>;