using Pharos.Client.App.Parameters.Requestors.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.Toaster;

namespace Pharos.Client.App.Parameters.Requestors.Views;

public partial class RequestorDetailsPage : DetailsPageBase<RequestorDetailsViewModel, RequestorDetails>
{
    public RequestorDetailsPage(
        RequestorDetailPageViewModel pageViewModel,
        IStringLocalizer<Translations> loc,
        NavigationManager navigation,
        IModalService modal,
        IToasterService toasterService,
        IExceptionHandlerService exceptionHandler) : base(pageViewModel, loc, navigation, modal, toasterService, exceptionHandler)
    {
    }

    protected override string ParentPage => PageRoute.Parameters.Requestors;

    protected override string DetailsPage(int id)
    {
        return PageRoute.Parameters.RequestorDetails.DetailsPage(id);
    }

    protected override List<BreadcrumbItem> GetInitialBreadcrumbs()
    {
        return [
            new BreadcrumbItem(Loc["NavMenu_Parameters"], PageRoute.Parameters.Default),
            new BreadcrumbItem(Loc["NavMenu_Requestors"], ParentPage),
            new BreadcrumbItem("...", "") // use constant number of items to avoid changing the layout of the breadcrumbs when loading the page or pressing cancel
            ];
    }

    protected override void AddBreadcrumbForViewModel()
    {
        var breadcrumbItem = DetailViewModel!.IsNewEntity
            ? new BreadcrumbItem(Loc["RequestorDetailsPage_New"], "")
            : new BreadcrumbItem(DetailViewModel.RequestorId ?? "", "");
        Breadcrumbs[^1] = breadcrumbItem;
    }

    protected override void SubscribeToViewModel()
    {
        DetailViewModel!.PropertyChanged += DetailViewModelOnPropertyChanged;
    }

    protected override async Task<bool> SaveChanges(bool invokedFromNavigationLock)
    {
        if (DetailViewModel is null)
        {
            return false;
        }

        return await SaveChanges(DetailViewModel.Model, DetailViewModel.Model.Id, invokedFromNavigationLock);
    }

    protected override void OnSaveCompleted()
    {
        if (DetailViewModel is null)
        {
            return;
        }

        // Show success toaster message
        var toasterText = DetailViewModel.IsNewEntity
            ? string.Format(@Loc["RequestorDetailsPage_RequestorCreated"], DetailViewModel.RequestorId)
            : string.Format(@Loc["RequestorDetailsPage_RequestorUpdated"], DetailViewModel.RequestorId);

        Toaster.ShowToast(ToastType.Confirmation, toasterText);
    }

    protected override void Disposing()
    {
        if (DetailViewModel is not null)
        {
            DetailViewModel.PropertyChanged -= DetailViewModelOnPropertyChanged;
        }
    }

    private void DetailViewModelOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RequestorDetailsViewModel.IsModified) or nameof(RequestorDetailsViewModel.RequestorId))
        {
            StateHasChanged();
        }
    }

    private bool IsValid()
    {
        var hasRequestorId = !string.IsNullOrEmpty(DetailViewModel?.RequestorId);

        return hasRequestorId;
    }

    private async Task ResetCounters()
    {
        var modal = Modal.ShowConfirm(
            Loc["RequestorDetailsPage_ResetCountersDialogMessage"],
            Loc["RequestorDetailsPage_ResetCountersDialogCancel"],
            Loc["RequestorDetailsPage_ResetCountersDialogConfirm"],
            new ModalOptions { Title = Loc["RequestorDetailsPage_ResetCountersDialogHeader"], ModalHeight = ModalSize.XSmall, ModalWidth = ModalSize.Small },
            true);

        var result = await modal.Result;
        if (result.Canceled)
        {
            return;
        }

        await DetailViewModel!.ResetCountersAsync();
    }

    /// <summary>
    /// Disable all fields until RequestorId has value.
    /// </summary>
    /// <returns></returns>
    private bool IsEmptyRequestorId()
    {
        return string.IsNullOrWhiteSpace(DetailViewModel?.RequestorId);
    }
}