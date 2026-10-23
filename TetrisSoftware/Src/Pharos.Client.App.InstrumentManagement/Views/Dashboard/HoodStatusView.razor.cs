using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Utilities;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.InstrumentManagement.Views.Dashboard;

public partial class HoodStatusView
{
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [Parameter] public bool IsStandbyState { get; set; }
    [Parameter] public LockState HoodLockState { get; set; }
    [Parameter] public bool IsUnlockInProgress { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnUnlockHoodClick { get; set; }

    private bool _canManageInstruments;

    private bool CanUnlock => GetCanUnlock();

    private HoodStatus State => HoodLockState switch
    {
        LockState.Open => HoodStatus.Open,
        LockState.ClosedUnlocked => HoodStatus.ClosedUnlocked,
        _ when _canManageInstruments && IsStandbyState && IsUnlockInProgress => HoodStatus.UnlockInProgress,
        _ => HoodStatus.ClosedLocked
    };

    protected override async Task OnParametersSetAsync()
    {
        _canManageInstruments = await AuthorizationService.IsAuthorized(Permissions.ManageInstruments);
    }

    private bool GetCanUnlock()
    {
        return _canManageInstruments && IsStandbyState && HoodLockState == LockState.ClosedLocked && !IsUnlockInProgress;
    }

    private string CssClasses => new CssBuilder("ph-hood-status")
        .AddClass("ph-hood-status--action", CanUnlock)
        .AddClass("ph-hood-status--unlocking", !CanUnlock && State == HoodStatus.UnlockInProgress)
        .AddClass("ph-hood-status--unlocked", !CanUnlock && State == HoodStatus.ClosedUnlocked)
        .AddClass("ph-hood-status--open", !CanUnlock && State == HoodStatus.Open)
        .AddClass("ph-hood-status--locked", !CanUnlock && State == HoodStatus.ClosedLocked)
        .Build();

    private string HoodStatusText => State switch
    {
        HoodStatus.UnlockInProgress => Loc["DashboardPage_HoodUnlocking"],
        HoodStatus.ClosedUnlocked => Loc["DashboardPage_HoodUnlocked"],
        HoodStatus.Open => Loc["DashboardPage_HoodOpen"],
        HoodStatus.ClosedLocked when CanUnlock => Loc["DashboardPage_UnlockHood"],
        _ => Loc["DashboardPage_HoodLocked"]
    };
}

public enum HoodStatus
{
    ClosedLocked,
    UnlockInProgress,
    ClosedUnlocked,
    Open
}
