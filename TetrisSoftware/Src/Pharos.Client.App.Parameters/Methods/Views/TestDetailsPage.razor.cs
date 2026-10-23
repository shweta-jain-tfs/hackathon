using System.ComponentModel;
using Pharos.Client.App.Parameters.Methods.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Validation;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.Methods.Views;

public partial class TestDetailsPage : DetailsPageBase<MethodTestDetailsViewModel, MethodTestDetails>
{
    private PhValidationContext? _validation;

    public TestDetailsPage(
        MethodTestDetailsPageViewModel pageViewModel,
        IStringLocalizer<Translations> loc,
        NavigationManager navigation,
        IModalService modal,
        IToasterService toasterService,
        IExceptionHandlerService exceptionHandler) : base(pageViewModel, loc, navigation, modal, toasterService, exceptionHandler)
    {
    }

    public enum TabItems
    {
        General,
        Report,
        LaboratoryNames,
        Advanced
    }

    protected override string ParentPage => DetailViewModel != null ? $"{PageRoute.Parameters.MethodDetails.Default}/{DetailViewModel.Model.MethodId}" : string.Empty;

    protected override string DetailsPage(int id)
    {
        return PageRoute.Parameters.TestDetails.DetailsPage(id);
    }

    protected override List<BreadcrumbItem> GetInitialBreadcrumbs()
    {
        return
        [
            new BreadcrumbItem(Loc["NavMenu_Parameters"], PageRoute.Parameters.Default),
            new BreadcrumbItem(Loc["NavMenu_Methods"], PageRoute.Parameters.Methods),
            new BreadcrumbItem("...", ""),
            new BreadcrumbItem("...", "")
        ];
    }

    protected override void AddBreadcrumbForViewModel()
    {
        Breadcrumbs[^2] = new BreadcrumbItem(DetailViewModel!.Model.MethodName, ParentPage);
        Breadcrumbs[^1] = new BreadcrumbItem(DetailViewModel!.Model.TestName, "");
    }

    protected override void SubscribeToViewModel()
    {
        DetailViewModel!.PropertyChanged += DetailViewModelOnPropertyChanged;
    }

    private void DetailViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MethodDetailsViewModel.IsModified))
        {
            StateHasChanged();
        }
    }

    protected override async Task<bool> SaveChanges(bool invokedFromNavigationLock)
    {
        if (DetailViewModel is null || !IsValid())
        {
            return false;
        }

        if (!_validation?.Validate() ?? false)
        {
            return false;
        }

        return await SaveChanges(DetailViewModel.Model, DetailViewModel.Model.Id, invokedFromNavigationLock);
    }

    private bool IsValid()
    {
        return (_validation?.IsValid() ?? true) && (DetailViewModel?.IsLaboratoryTestNamesValid() ?? true);
    }

    protected override void Disposing()
    {
        if (DetailViewModel is not null)
        {
            DetailViewModel.PropertyChanged -= DetailViewModelOnPropertyChanged;
        }
    }

    private bool TabHasError(string tabName)
    {
        return !_validation?.IsValid(tabName) ?? false;
    }

    private void OnValidationContextStateChanged(ValidationContextState state)
    {
        StateHasChanged();
    }
}