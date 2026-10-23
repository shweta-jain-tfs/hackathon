using System.ComponentModel;
using Pharos.Client.App.Request.ViewModels;
using Pharos.Client.App.Request.Views.RequestDetail;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Validation;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.TabControl;
using Pharos.Common.Web.Components.Toaster;
using Pharos.Contracts.Security;
using static Pharos.Client.Common.App.PageRouting.PageRoute;

namespace Pharos.Client.App.Request.Views;

public partial class RequestDetailPage : DetailsPageBase<RequestDetailsViewModel, RequestDetails>
{
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;
    public enum TabItems
    {
        Details,
        Patient,
        Tests,
        Tubes
    }

    public static readonly string HeaderViewName = "Header";
    public static readonly string DetailsTabName = TabItems.Details.ToString();
    public static readonly string PatientTabName = TabItems.Patient.ToString();
    public static readonly string TubesTabName = TabItems.Tubes.ToString();

    public const string TabQueryParameterName = "tab";

    [SupplyParameterFromQuery(Name = TabQueryParameterName)]
    public string? Tab { get; set; }

    private int _originalPreDilutionFactor;

    private HeaderView? _headerViewReference;
    private TestListView? _testsViewReference;
    private readonly RequestDetailPageViewModel _pageViewModel;
    private PhValidationContext? _validation;

    private bool CanViewPatientInfo =>
        HasViewPersonalInfoPermission &&
        DetailViewModel?.UiSettings?.RequestSettings?.IsPatientInformationEnabled == true;

    private bool HasViewPersonalInfoPermission { get; set; }

    private bool HasManageTestOrdersPermission { get; set; }

    private string TitleTests => DetailViewModel == null ? "Tests" : $"Tests ({DetailViewModel.TestOrders.Count})";

    public RequestDetailPage(
        RequestDetailPageViewModel pageViewModel,
        IStringLocalizer<Translations> loc,
        NavigationManager navigation,
        IModalService modal,
        IToasterService toaster,
        IExceptionHandlerService exceptionHandler) : base(pageViewModel, loc, navigation, modal, toaster, exceptionHandler)
    {
        _pageViewModel = pageViewModel;
        _pageViewModel.RequestsDeleted += OnRequestsDeleted;
    }

    private void OnRequestsDeleted(object? sender, List<int> requestIds)
    {
        if (DetailViewModel is null)
        {
            return;
        }

        if (requestIds.Contains(DetailViewModel.Model.Id))
        {
            NavigateToParentList();
        }
    }

    protected override string ParentPage => PageRoute.Requests;

    protected override string DetailsPage(int id)
    {
        return SampleDetails.DetailsPage(id);
    }

    protected override async Task OnParametersSetAsync()
    {
        HasViewPersonalInfoPermission = await AuthorizationService.IsAuthorized(Permissions.ViewPersonalAndHealthData);
        HasManageTestOrdersPermission = await AuthorizationService.IsAuthorized(Permissions.ManageTestOrders);

        await base.OnParametersSetAsync();
    }

    /// <summary>
    /// Default tab for the request detail page.
    /// </summary>
    protected int DefaultTab => ResolveRequestedTab() ?? (int)TabItems.Tests;

    protected string? TabStateId => ResolveRequestedTab().HasValue || DetailViewModel?.IsNewEntity == true ? null : "tabs";

    protected override List<BreadcrumbItem> GetInitialBreadcrumbs()
    {
        return [new BreadcrumbItem(Loc["RequestDetailPage_Navigation_Requests"], ParentPage)];
    }

    private int? ResolveRequestedTab()
    {
        if (string.IsNullOrWhiteSpace(Tab))
        {
            return null;
        }

        return Enum.TryParse<TabItems>(Tab, ignoreCase: true, out var tab)
            ? (int)tab
            : null;
    }

    protected override void AddBreadcrumbForViewModel()
    {
        var breadcrumbItem = DetailViewModel!.IsNewEntity ? new BreadcrumbItem(Loc["RequestDetailPage_Navigation_RequestDetails_NewRequest"], "") : new BreadcrumbItem(DetailViewModel.SampleIdDisplay ?? "", "");
        Breadcrumbs.Add(breadcrumbItem);
    }

    protected override void SubscribeToViewModel()
    {
        _originalPreDilutionFactor = DetailViewModel!.PreDilutionFactor;
        DetailViewModel!.PropertyChanged += DetailViewModelOnPropertyChanged;
        DetailViewModel.ValidationNeededForRequestId += ValidationNeededForRequestId;
    }

    private void ValidationNeededForRequestId(object? sender, EventArgs e)
    {
        _validation?.Validate(DetailsTabName);
    }

    protected override void OnDiscardChanges()
    {
        _headerViewReference?.DiscardChanges();
        _testsViewReference?.DiscardChanges();
    }

    protected override async Task<bool> SaveChanges(bool invokedFromNavigationLock)
    {
        if (DetailViewModel is null)
        {
            return false;
        }

        if (!_validation?.Validate() ?? false)
        {
            return false;
        }

        // Confirm if pre-dilution has changed
        if (DetailViewModel.PreDilutionFactor != _originalPreDilutionFactor)
        {
            var message = string.Format(
                Loc["RequestDetailPage_PreDilutionChangedMessage"],
                _originalPreDilutionFactor,
                DetailViewModel.PreDilutionFactor);

            var modal = Modal.ShowConfirm(
                message,
                Loc["RequestDetailPage_PreDilutionChangedCancel"],
                Loc["RequestDetailPage_PreDilutionChangedSave"],
                new ModalOptions { Title = Loc["RequestDetailPage_PreDilutionChangedTitle"], ModalHeight = ModalSize.Small, ModalWidth = ModalSize.Small });

            var result = await modal.Result;
            if (result.Canceled)
            {
                return false;
            }
        }

        return await SaveChanges(DetailViewModel.Model, DetailViewModel.Model.Id, invokedFromNavigationLock);
    }

    protected override void OnSaveCompleted()
    {
        if (DetailViewModel is null)
        {
            return;
        }

        // Show toaster when request is saved.
        var toasterText = string.Format(@Loc["RequestDetailPage_RequestIsSaved"], DetailViewModel.SampleId);
        Toaster.ShowToast(ToastType.Confirmation, toasterText);
    }

    protected override void Disposing()
    {
        _pageViewModel.RequestsDeleted -= OnRequestsDeleted;
        if (DetailViewModel is not null)
        {
            DetailViewModel.PropertyChanged -= DetailViewModelOnPropertyChanged;
            DetailViewModel.ValidationNeededForRequestId -= ValidationNeededForRequestId;
        }
    }

    private void DetailViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RequestDetailsViewModel.IsModified) or nameof(RequestDetailsViewModel.SampleId) or nameof(RequestDetailsViewModel.RequestId) or nameof(RequestDetailsViewModel.SampleTubes) or nameof(RequestDetailsViewModel.RequiredVolumeDisplay))
        {
            StateHasChanged();
        }
    }

    private bool IsValid()
    {
        var validationPassed = _validation?.IsValid() == true;
        var hasSampleId = !string.IsNullOrEmpty(DetailViewModel?.SampleId);

        return validationPassed && hasSampleId;
    }

    /// <summary>
    /// Determines whether the Tubes tab should show an error indicator in the tab header.
    /// </summary>
    /// <returns>True when the Tubes tab has validation errors or tube issues; otherwise false.</returns>
    private bool HasTubesTabError()
    {
        var hasTubeIssues = DetailViewModel?.SampleTubes.Any(x => x.Error != SampleTubeError.None) == true;
        return HasTabValidationError(TubesTabName) || hasTubeIssues;
    }

    /// <summary>
    /// Resolves the symbol type for the Tubes tab header.
    /// </summary>
    /// <returns>Warning when the Tubes tab has errors; otherwise None.</returns>
    private TabControlSymbolType GetTubesTabSymbolType()
    {
        return HasTubesTabError() ? TabControlSymbolType.Warning : TabControlSymbolType.None;
    }

    /// <summary>
    /// Determines whether the specified tab should show a validation error indicator in the tab header.
    /// </summary>
    /// <param name="tabName">The tab section name.</param>
    /// <returns>True when the tab has validation errors; otherwise false.</returns>
    private bool HasTabValidationError(string tabName)
    {
        return !(_validation?.IsValid(tabName) ?? true);
    }

    private void OnValidationContextStateChanged(ValidationContextState state)
    {
        StateHasChanged();
    }
}