using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.InstrumentManagement.Models.Dashboard;
using Pharos.Client.App.InstrumentManagement.Views.Dashboard;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Client.Common.App.State.CurrentInstrument;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.Modal;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.InstrumentManagement.Views;

public partial class DashboardPage
{
    [Inject] public IAuthorizationService AuthorizationService { get; set; } = null!;
    [Inject] private ICurrentInstrumentViewModel InstrumentModel { get; set; } = null!;
    [Inject] private InstrumentStateDataSource InstrumentStateDataSource { get; set; } = null!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;
    [Inject] private IModalService Modal { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private readonly List<(string Text, Func<Task> OnClick, bool Disabled)> _dropdownItems = new();
    private bool _isDisabledEndAssayButton;
    private bool _isDisabledStartAssayButton;
    private bool _isDisabledGoToStandbyButton;
    public bool ShowEndAssayButton { get; private set; }
    public bool ShowStartAssayButton { get; private set; }
    public bool ShowGoToStandbyButton { get; private set; }
    private bool ShowLightButton => InstrumentModel.State != InstrumentState.PowerSaving;

    protected override async Task OnInitializedAsync()
    {
        await InstrumentModel.LoadData();
        await UpdateUIBasedOnInstrumentState();
        InstrumentModel.OnCurrentInstrumentChange += async () => await UpdateUIBasedOnInstrumentState();
        InstrumentStateDataSource.OnControlInstrumentStateCompleted += async (isSuccess) => await HandleControlInstrumentStateCompleted(isSuccess);
    }

    private async Task HandleControlInstrumentStateCompleted(bool isSuccess)
    {
        if (!isSuccess)
        {
            // Reset flag so UI buttons are re-enabled when the request failed.
            InstrumentModel.IsStateChangeRequested = false;
        }

        await UpdateUIBasedOnInstrumentState();
    }

    /// <summary>
    /// Update the UI based on the instrument state
    /// </summary>
    /// <returns></returns>
    private async Task UpdateUIBasedOnInstrumentState()
    {
        await CreateMoreMenuOptions();
        await DetermineStartAssayButtonVisibility();
        await DetermineEndAssayButtonVisibility();
        await DetermineGoToStandbyButtonVisibility();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Create the options for the more menu
    /// </summary>
    /// <returns></returns>
    private async Task CreateMoreMenuOptions()
    {
        _dropdownItems.Clear();

        var state = InstrumentModel.State;
        var canManageInstruments = await AuthorizationService.IsAuthorized(Permissions.ManageInstruments);
        var canRunServiceTools = await AuthorizationService.IsAuthorized(Permissions.RunServiceTools);
        var isHoodUnlocked = InstrumentModel.HoodLockState is LockState.ClosedUnlocked or LockState.Open;

        if (ShouldShowEnterStandbyOption(state, canManageInstruments, canRunServiceTools))
        {
            _dropdownItems.Add((Loc["DashboardPage_EnterStandby"], EnterStandby, isHoodUnlocked));
        }

        if (ShouldShowPowerSaveOption(state, canManageInstruments))
        {
            _dropdownItems.Add((Loc["DashboardPage_PowerSave"], PowerSave, isHoodUnlocked));
        }

        if (ShouldShowPowerOffOption(state, canManageInstruments))
        {
            _dropdownItems.Add((Loc["DashboardPage_PowerOff"], PowerOff, InstrumentModel.IsStateChangeRequested));
        }

        if (ShouldShowAbortAssayOption(state, canRunServiceTools))
        {
            _dropdownItems.Add((Loc["DashboardPage_AbortAssay"], AssayAbort, false));
        }
    }

    private static bool ShouldShowPowerSaveOption(InstrumentState state, bool canManageInstruments)
    {
        return state == InstrumentState.Standby && canManageInstruments;
    }

    private static bool ShouldShowPowerOffOption(InstrumentState state, bool canManageInstruments)
    {
        return state == InstrumentState.Standby && canManageInstruments;
    }

    private static bool ShouldShowAbortAssayOption(InstrumentState state, bool canRunServiceTools)
    {
        // Check states where assay can be aborted with service tools permission.
        if (!canRunServiceTools)
        {
            return false;
        }

        return state
            is InstrumentState.Assay
            or InstrumentState.AssayCmr
            or InstrumentState.AssayCmrFinished
            or InstrumentState.AssayFinishOrdered
            or InstrumentState.AssayFinishing
            or InstrumentState.BlankMeasurement
            or InstrumentState.MaintenanceBlankMeasurement
            or InstrumentState.MaintenanceCleaning
            or InstrumentState.MaintenanceCleaningHypochlorite
            or InstrumentState.MaintenanceExtendedRinsing
            or InstrumentState.MaintenanceManualCleaning
            or InstrumentState.MaintenanceRinsing
            or InstrumentState.Priming
            or InstrumentState.ServiceEjectingCapWell
            or InstrumentState.ServiceVerifyingStorage;
    }

    /// <summary>
    /// Determines whether the Enter Standby option should be visible in the more menu.
    /// </summary>
    private static bool ShouldShowEnterStandbyOption(InstrumentState state, bool canManageInstruments, bool canRunServiceTools)
    {
        // States where the instrument can be put into standby with service tools permission.
        if (canRunServiceTools && state == InstrumentState.CmrPrepare)
        {
            return true;
        }

        // States where the instrument can be put into standby with manage instruments permission.
        if (canManageInstruments && state is InstrumentState.BlankMeasurement or InstrumentState.Priming)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determine the visibility of the End Assay button
    /// </summary>
    /// <returns></returns>
    private async Task DetermineEndAssayButtonVisibility()
    {
        // Should End Assay button be shown?
        ShowEndAssayButton = InstrumentModel.State == InstrumentState.Assay &&
                             await AuthorizationService.IsAuthorized(Permissions.ManageInstruments);

        // Disable End Assay Button while waiting for instrument to report new state.
        _isDisabledEndAssayButton = ShowEndAssayButton && InstrumentModel.IsStateChangeRequested;
    }

    /// <summary>
    /// Determine the visibility of the Start Assay button
    /// </summary>
    /// <returns></returns>
    private async Task DetermineStartAssayButtonVisibility()
    {
        var isAuthorizedToStartAssayInStandby = InstrumentModel.State == InstrumentState.Standby &&
                                                await AuthorizationService.IsAuthorized(Permissions.ManageInstruments);

        var isAuthorizedToStartAssayInCmrPrepare = InstrumentModel.State == InstrumentState.CmrPrepare &&
                                                   await AuthorizationService.IsAuthorized(Permissions.RunServiceTools);

        ShowStartAssayButton = isAuthorizedToStartAssayInStandby || isAuthorizedToStartAssayInCmrPrepare;

        var isHoodUnlocked = InstrumentModel.HoodLockState is
            LockState.ClosedUnlocked or LockState.Open;

        _isDisabledStartAssayButton = ShowStartAssayButton &&
                                      (isHoodUnlocked || InstrumentModel.MaintenanceNeeded || InstrumentModel.IsStateChangeRequested);
    }

    /// <summary>
    /// Determine the visibility of the Go to standby button
    /// </summary>
    private async Task DetermineGoToStandbyButtonVisibility()
    {
        ShowGoToStandbyButton = (InstrumentModel.State is InstrumentState.AssayCmrFinished)
            && await AuthorizationService.IsAuthorized(Permissions.RunServiceTools);

        _isDisabledGoToStandbyButton = ShowGoToStandbyButton && InstrumentModel.IsStateChangeRequested;
    }

    /// <summary>
    /// Abort the assay
    /// </summary>
    private async Task AssayAbort()
    {
        var modal = Modal.Show<AssayAbortModalView>(new ModalOptions { Title = Loc["DashboardPage_AssayAbortModal_Title"], ModalHeight = ModalSize.Small, ModalWidth = ModalSize.Small });

        var result = await modal.Result;

        if (!result.Canceled)
        {
            _ = InstrumentStateDataSource.AssayAbortAsync();
        }
    }

    /// <summary>
    /// End the assay
    /// </summary>
    /// <param name="obj"></param>
    private async Task AssayEnd(MouseEventArgs obj)
    {
        var modal = Modal.Show<AssayEndModalView>(new ModalOptions { Title = Loc["DashboardPage_AssayEndModal_Title"], ModalHeight = ModalSize.Small, ModalWidth = ModalSize.Small });

        var result = await modal.Result;

        if (!result.Canceled)
        {
            // Mark that a state-change request was sent so buttons stay disabled until instrument updates.
            InstrumentModel.IsStateChangeRequested = true;
            await DetermineEndAssayButtonVisibility();

            var endAssayOption = (EndAssayOptions)result.Data!;

            _ = InstrumentStateDataSource.AssayEndAsync(endAssayOption);
        }
    }

    /// <summary>
    /// Start the assay
    /// </summary>
    /// <param name="obj"></param>
    private async Task AssayStart(MouseEventArgs obj)
    {
        var isCmrPrepare = InstrumentModel.State == InstrumentState.CmrPrepare;

        var modal = isCmrPrepare
            ? Modal.Show<AssayStartCmrModalView>(new ModalOptions
            {
                Title = Loc["DashboardPage_AssayStartCmrModal_Title"],
                ModalHeight = ModalSize.XSmall,
                ModalWidth = ModalSize.Small
            })
            : Modal.Show<AssayStartModalView>(new ModalOptions
            {
                Title = Loc["DashboardPage_AssayStartModal_Title"],
                ModalHeight = ModalSize.Large,
                ModalWidth = ModalSize.Medium
            });

        var result = await modal.Result;

        if (!result.Canceled)
        {
            InstrumentModel.IsStateChangeRequested = true;
            await DetermineStartAssayButtonVisibility();

            _ = isCmrPrepare
                ? InstrumentStateDataSource.AssayStartCmrAsync()
                : InstrumentStateDataSource.AssayStartAsync();
        }
    }

    /// <summary>
    /// Enter power save mode
    /// </summary>
    private async Task PowerSave()
    {
        var modal = Modal.Show<PowerSaveModalView>(
            new ModalOptions
            {
                Title = Loc["DashboardPage_PowerSaveModal_Title"],
                ModalHeight = ModalSize.XSmall,
                ModalWidth = ModalSize.Small
            });

        var result = await modal.Result;

        if (!result.Canceled)
        {
            var success = await InstrumentStateDataSource.PowerSaveAsync();

            if (success)
            {
                NavigationManager.NavigateTo($"{AccountRoutes.Logout}?returnUrl={Uri.EscapeDataString(PageRoute.LockScreen)}",
                    new NavigationOptions
                    {
                        ForceLoad = true,
                        ReplaceHistoryEntry = true
                    });
            }
            else
            {
                var errorModal = Modal.Show<PowerSaveErrorModalView>(
                    new ModalOptions
                    {
                        Title = Loc["DashboardPage_PowerSaveError_Title"],
                        ModalHeight = ModalSize.XSmall,
                        ModalWidth = ModalSize.Small,
                        Status = ModalStatus.Alarm
                    });

                await errorModal.Result;
            }
        }
    }

    /// <summary>
    /// Go to power off state.
    /// </summary>
    private async Task PowerOff()
    {
        var modal = Modal.ShowConfirm(Loc["DashboardPage_PowerOffModal_Message"], Loc["DashboardPage_PowerOffModal_Cancel"], Loc["DashboardPage_PowerOffModal_Ok"], new ModalOptions
        {
            Title = Loc["DashboardPage_PowerOffModal_Title"],
            Status = ModalStatus.Warning,
            ModalHeight = ModalSize.XSmall,
            ModalWidth = ModalSize.Small
        }, true);

        var result = await modal.Result;
        if (result.Canceled)
        {
            return;
        }

        InstrumentModel.IsStateChangeRequested = true;
        await UpdateUIBasedOnInstrumentState();

        _ = InstrumentStateDataSource.PowerOffAsync();
    }

    /// <summary>
    /// Enter unlock hood mode
    /// </summary>
    private async Task UnlockHoodClicked()
    {
        await InstrumentStateDataSource.UnlockHoodAsync();
    }

    /// <summary>
    /// Request the instrument to toggle its light.
    /// </summary>
    private async Task OnLightOnOffClicked()
    {
        await InstrumentStateDataSource.ControlLightAsync();
    }

    /// <summary>
    /// Enter standby mode.
    /// </summary>
    private async Task EnterStandby()
    {
        var modal = Modal.ShowConfirm(Loc["DashboardPage_GotoStandby_Message"], Loc["DashboardPage_GotoStandby_Cancel"], Loc["DashboardPage_GotoStandby_Ok"], new ModalOptions
        {
            Title = Loc["DashboardPage_GotoStandby_Title"],
            Status = ModalStatus.Info,
            ModalHeight = ModalSize.XSmall,
            ModalWidth = ModalSize.Small
        }, true);

        var result = await modal.Result;
        if (result.Canceled)
        {
            return;
        }

        // Mark that a state-change request was sent so button stay disabled until the instrument responds.
        InstrumentModel.IsStateChangeRequested = true;
        await DetermineGoToStandbyButtonVisibility();

        _ = InstrumentStateDataSource.StandbyAsync();
    }

    /// <summary>
    /// Enter standby mode from the button click.
    /// </summary>
    private Task Standby(MouseEventArgs obj)
    {
        return EnterStandby();
    }
}
