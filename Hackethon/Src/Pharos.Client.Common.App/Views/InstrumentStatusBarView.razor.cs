using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.State.CurrentInstrument;
using Pharos.Client.Common.App.ViewModels;
using Pharos.Common.Web.Components.Health;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.Toaster;
using Pharos.Contracts.Security;

namespace Pharos.Client.Common.App.Views;

public partial class InstrumentStatusBarView : IDisposable
{
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = null!;
    [Inject] private ICurrentInstrumentViewModel InstrumentModel { get; set; } = null!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private IModalService Modal { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IToasterService ToasterService { get; set; } = null!;
    [Inject] private ITaskCardTranslator TaskCardTranslator { get; set; } = null!;
    [Inject] private InstrumentStateViewModel ViewModel { get; set; } = null!;

    [Parameter] public bool LockScreenMode { get; set; }

    private bool _playSound;

    private bool CanShowSkipTodayLink { get => ViewModel.ShowSkipTodayLink && field; set; }

    private string CssClass
    {
        get
        {
            var status = ViewModel.MapHealthStatus();

            return new CssBuilder("ph-statusbar")
                .AddClass("ph-statusbar--lockscreen-ok", LockScreenMode && status == HealthStatus.Ok)
                .AddClass("ph-statusbar--lockscreen-warning", LockScreenMode && status == HealthStatus.Warning)
                .AddClass("ph-statusbar--lockscreen-alarm", LockScreenMode && status == HealthStatus.Alarm)
                .AddClass("ph-statusbar--lockscreen-off", LockScreenMode && status == HealthStatus.Off)
                .AddClass("ph-statusbar--lockscreen-standby", LockScreenMode && status == HealthStatus.Standby)
                .Build();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        InstrumentModel.OnCurrentInstrumentChange += OnInstrumentStateChange;
        InstrumentModel.OnCurrentInstrumentSampleDrawerOpen += NavigateToSamplesPage;
        InstrumentModel.OnCurrentInstrumentReagentDrawerOpen += NavigateToReagentPage;
        InstrumentModel.OnCurrentInstrumentBottleDrawerOpen += NavigateToBottlePage;
        InstrumentModel.OnCurrentInstrumentLiquidDrawerOpen += NavigateToLiquidPage;
        InstrumentModel.OnCurrentInstrumentSolidWasteDrawerOpen += NavigateToSolidWastePage;
        InstrumentModel.OnTaskCardCreatedOrChanged += OnTaskCardCreatedOrChanged;
        ViewModel.OnStateRowsChanged += OnStateRowsChanged;

        await LoadManageInstrumentPermissionAsync();
    }

    /// <summary>
    /// Loads whether the signed-in user can manage instruments so skip actions can be shown only to authorized users.
    /// </summary>
    private async Task LoadManageInstrumentPermissionAsync()
    {
        CanShowSkipTodayLink = await AuthorizationService.IsAuthorized(Permissions.ManageInstruments);
    }

    private void OnInstrumentStateChange()
    {
        // Ensure UI updates run on the Blazor renderer thread.
        _ = InvokeAsync(() =>
        {
            _playSound = InstrumentModel.PlaySound;
            StateHasChanged();
        });
    }

    /// <summary>
    /// Called when the status rows have changed, either by the timer tick or by a change in the instrument state.
    /// </summary>
    private void OnStateRowsChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private void NavigateToSamplesPage()
    {
        var currentUri = Navigation.ToBaseRelativePath(Navigation.Uri);
        var isSamplesPage = currentUri.Equals(PageRoute.Samples, StringComparison.Ordinal);
        if (!Modal.HasOpenModals() && !isSamplesPage)
        {
            Navigation.NavigateTo(PageRoute.Samples);
        }
    }

    private void NavigateToReagentPage()
    {
        var currentUri = Navigation.ToBaseRelativePath(Navigation.Uri);
        var isReagentPage = currentUri.Equals(PageRoute.Reagent, StringComparison.Ordinal);
        if (!Modal.HasOpenModals() && !isReagentPage)
        {
            Navigation.NavigateTo(PageRoute.Reagent);
        }
    }

    private void NavigateToBottlePage()
    {
        var currentUri = Navigation.ToBaseRelativePath(Navigation.Uri);
        var isBottlePage = currentUri.Equals(PageRoute.Bottle, StringComparison.Ordinal);
        if (!Modal.HasOpenModals() && !isBottlePage)
        {
            Navigation.NavigateTo(PageRoute.Bottle);
        }
    }

    private void NavigateToLiquidPage()
    {
        var currentUri = Navigation.ToBaseRelativePath(Navigation.Uri);
        var isLiquidPage = currentUri.Equals(PageRoute.Liquid, StringComparison.Ordinal);
        if (!Modal.HasOpenModals() && !isLiquidPage)
        {
            Navigation.NavigateTo(PageRoute.Liquid);
        }
    }

    private void NavigateToSolidWastePage()
    {
        var currentUri = Navigation.ToBaseRelativePath(Navigation.Uri);
        var isSolidWastePage = currentUri.Equals(PageRoute.SolidWaste, StringComparison.Ordinal);
        if (!Modal.HasOpenModals() && !isSolidWastePage)
        {
            Navigation.NavigateTo(PageRoute.SolidWaste);
        }
    }

    private void OnTaskCardCreatedOrChanged(TaskCardType taskCardType, TaskCardState taskCardState)
    {
        if (IsCurrentPage(PageRoute.Tasks))
        {
            return;
        }

        if (taskCardState is not (TaskCardState.Warning or TaskCardState.Error))
        {
            return;
        }

        var toastType = taskCardState is TaskCardState.Error
            ? ToastType.Alarm
            : ToastType.Warning;

        ToasterService.ShowToast(toastType, TaskCardTranslator.GetTitle(taskCardType));
    }

    private bool IsCurrentPage(string pageRoute)
    {
        var currentPath = Navigation.ToBaseRelativePath(Navigation.Uri);
        var routePath = pageRoute.Trim('/');

        return currentPath.Equals(routePath, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        InstrumentModel.OnCurrentInstrumentChange -= OnInstrumentStateChange;
        InstrumentModel.OnCurrentInstrumentSampleDrawerOpen -= NavigateToSamplesPage;
        InstrumentModel.OnCurrentInstrumentReagentDrawerOpen -= NavigateToReagentPage;
        InstrumentModel.OnCurrentInstrumentBottleDrawerOpen -= NavigateToBottlePage;
        InstrumentModel.OnCurrentInstrumentLiquidDrawerOpen -= NavigateToLiquidPage;
        InstrumentModel.OnTaskCardCreatedOrChanged -= OnTaskCardCreatedOrChanged;
        ViewModel.OnStateRowsChanged -= OnStateRowsChanged;

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Skips the next scheduled automatic assay start for the current instrument.
    /// </summary>
    private async Task SkipToday()
    {
        await ViewModel.SkipNextScheduledAssayStart();
        ToasterService.ShowToast(ToastType.Confirmation, Loc["InstrumentStatusBarView_SkipToday_Toaster"]);
    }

    private void NavigateToSearch(string searchText)
    {
        // Trim whitespace from beginning and end of the search string
        var trimmedSearchText = searchText.Trim();

        //Navigate to search page
        Navigation.NavigateTo($"{PageRoute.Search.Default}?search={Uri.EscapeDataString(trimmedSearchText)}");
    }
}
