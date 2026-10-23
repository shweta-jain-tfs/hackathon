using System.Net;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.ViewModels;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Breadcrumb;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.Toaster;

namespace Pharos.Client.Common.App;

/// <summary>
/// Represents the base class for a details page.
/// Utilizes the DetailsPageViewModel to load, save and listen to changes to the TModel.
/// The save and discard buttons on the details page are handled by this class.
/// The list of breadcrumbs is managed by this class. But can the derived class needs to add the initial breadcrumbs and can add more breadcrumbs to the list when the TViewModel is created.
/// </summary>
/// <typeparam name="TViewModel">The type of the details view model that is used by the details view</typeparam>
/// <typeparam name="TModel">The underlying model that is sent to/from the backend</typeparam>
/// <typeparam name="TKey">The key of the underlying model</typeparam>
public abstract class DetailsPageBase<TViewModel, TModel, TKey> : ComponentBase, IDisposable where TKey : notnull where TViewModel : ViewModelItem<TModel>
{
    private readonly DetailsPageViewModel<TViewModel, TModel, TKey> _pageViewModel;
    private readonly IExceptionHandlerService _exceptionHandler;
    private bool _isSaving;
    private TKey? _id;

    protected DetailsPageBase(
        DetailsPageViewModel<TViewModel, TModel, TKey> pageViewModel,
        IStringLocalizer<Translations> loc,
        NavigationManager navigation,
        IModalService modal,
        IToasterService toaster,
        IExceptionHandlerService exceptionHandler)
    {
        _pageViewModel = pageViewModel;
        _exceptionHandler = exceptionHandler;
        _pageViewModel.ModelUpdated += OnModelUpdated;
        _pageViewModel.ModelDeleted += OnModelDeleted;
        Loc = loc;
        Navigation = navigation;
        Modal = modal;
        Toaster = toaster;
        Breadcrumbs = [];
    }

    private async void OnModelUpdated(object? sender, TKey id)
    {
        if (_id is null)
        {
            return;
        }

        if (_id.Equals(id) && !_isSaving)
        {
            if (_pageViewModel.IsModified)
            {
                //TODO, maybe notify user that the data has been changed and ask if they want to reload the page
                Toaster.ShowToast(ToastType.Information, "The data has been updated by another user.");
                return;
            }

            OnDiscardChanges();

            await LoadAndSubscribe();

            StateHasChanged();
        }
    }

    private void OnModelDeleted(object? sender, TKey id)
    {
        if (_id is null)
        {
            return;
        }

        if (_id.Equals(id))
        {
            //TODO, maybe notify user that the data has been deleted by another user
            Toaster.ShowToast(_pageViewModel.IsModified ? ToastType.Warning : ToastType.Information, "The data has been deleted by another user.");
        }
    }

    protected IStringLocalizer<Translations> Loc { get; }
    protected NavigationManager Navigation { get; }
    protected IModalService Modal { get; }
    protected IToasterService Toaster { get; }

    [Parameter] public TKey? Id { get; set; }

    protected TViewModel? DetailViewModel { get; private set; }

    protected abstract string ParentPage { get; }

    protected abstract string DetailsPage(TKey id);

    protected List<BreadcrumbItem> Breadcrumbs { get; }

    protected override async Task OnParametersSetAsync()
    {
        _id = Id;
        await Initialize();
    }

    private async Task LoadAndSubscribe()
    {
        DetailViewModel = await _pageViewModel.LoadAsync(_id);
        SubscribeToViewModel();
    }

    private void InitializeBreadcrumbs()
    {
        Breadcrumbs.Clear();
        Breadcrumbs.AddRange(GetInitialBreadcrumbs());
    }

    /// <summary>
    /// The derived class should provide the initial breadcrumbs for the details page.
    /// More breadcrumbs can be added when the TViewModel is created (AddBreadcrumbForViewModel).
    /// </summary>
    protected abstract List<BreadcrumbItem> GetInitialBreadcrumbs();

    /// <summary>
    /// The derived class can add bread crumb for the newly created ViewModel.
    /// </summary>
    protected abstract void AddBreadcrumbForViewModel();

    protected async Task DiscardChanges()
    {
        OnDiscardChanges();
        Disposing();
        await Initialize();
    }

    private async Task Initialize()
    {
        InitializeBreadcrumbs();
        await LoadAndSubscribe();
        AddBreadcrumbForViewModel();
    }

    /// <summary>
    /// Reset any view-specific state if needed
    /// </summary>
    protected virtual void OnDiscardChanges()
    {
    }

    /// <summary>
    /// The derived class can start using the DetailViewModel when this method is called.
    /// </summary>
    protected abstract void SubscribeToViewModel();

    protected abstract Task<bool> SaveChanges(bool invokedFromNavigationLock);

    protected async Task<bool> SaveChanges(TModel model, TKey id, bool invokedFromNavigationLock)
    {
        try
        {
            _isSaving = true;

            var (newId, isNew) = await _pageViewModel.SaveAsync(model, id);
            _id = newId;

            OnSaveCompleted();

            await NavigateToDetails(isNew, invokedFromNavigationLock);

            return true;
        }
        catch (ApiException e)
        {
            if (e.StatusCode == (int)HttpStatusCode.Gone)
            {
                // This object is impossible to save, because it has been deleted by another user.
                // Navigate back to the list page to avoid user staying on a page with stale data.
                _exceptionHandler.ShowApiException(e, ExceptionDisplayMode.ModalKeepOnNavigation);
                DetailViewModel?.SetAsUnmodified();
                NavigateToParentList();
            }
            else
            {
                _exceptionHandler.ShowApiException(e);
            }
        }
        catch (Exception e)
        {
            _exceptionHandler.ShowExceptionMessage(e);
        }
        finally
        {
            _isSaving = false;
        }

        return false;
    }

    private async Task NavigateToDetails(bool isNew, bool invokedFromNavigationLock)
    {
        if (invokedFromNavigationLock)
        {
            return;
        }

        if (isNew)
        {
            // For a new item, navigate to the details page with the new id.
            // This will trigger the page to reload and load the data for the new id.
            Navigation.NavigateTo(DetailsPage(_id!), false, true);
        }
        else
        {
            // Reload data
            OnDiscardChanges();
            Disposing();
            await Initialize();
        }
    }

    protected virtual void OnSaveCompleted() { }

    protected void NavigateToParentList()
    {
        Navigation.NavigateTo(ParentPage);
    }

    public void Dispose()
    {
        Disposing();

        _pageViewModel.ModelUpdated -= OnModelUpdated;
        _pageViewModel.ModelDeleted -= OnModelDeleted;
        _pageViewModel.Dispose();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Unhook events and clean up resources in the derived class
    /// </summary>
    protected abstract void Disposing();
}

public abstract class DetailsPageBase<TViewModel, TModel> : DetailsPageBase<TViewModel, TModel, int> where TViewModel : ViewModelItem<TModel>
{
    protected DetailsPageBase(
        DetailsPageViewModel<TViewModel, TModel, int> pageViewModel,
        IStringLocalizer<Translations> loc,
        NavigationManager navigation,
        IModalService modal,
        IToasterService toaster,
        IExceptionHandlerService exceptionHandler) : base(pageViewModel, loc, navigation, modal, toaster, exceptionHandler)
    {
    }
}