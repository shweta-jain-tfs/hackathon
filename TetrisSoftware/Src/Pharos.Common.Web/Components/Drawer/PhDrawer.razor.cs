using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Drawer;
public partial class PhDrawer : ComponentBase
{
    #region Properties

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public string ActionName { get; set; } = string.Empty;

    [Parameter]
    public bool IsButtonVisible { get; set; }

    [Parameter]
    public bool IsDrawerDim { get; set; }

    [Parameter]
    public PhDrawerLockState DoorState { get; set; }

    [Parameter]
    public string? DoorStateText { get; set; }

    [Parameter]
    public DrawerPosition Position { get; set; }

    [Parameter]
    public EventCallback OnUnlockDrawerClick { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool IsScanningOngoing { get; set; }

    [Parameter]
    public bool HasManageSamplePermission { get; set; }

    public string IndicatorLedCss { get; private set; } = string.Empty;
    public string DrawerContainerCss { get; private set; } = string.Empty;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        SetDrawerIndicatorCss();
        SetDrawerContainerCss();
    }

    #endregion


    #region Private Methods

    private async Task OnUnlockDrawerClicked()
    {
        await OnUnlockDrawerClick.InvokeAsync();
    }

    private void SetDrawerIndicatorCss()
    {
        switch (DoorState)
        {
            case PhDrawerLockState.UnlockInProgress:
                IndicatorLedCss = new CssBuilder("ph-drawer__indicator-led").AddClass("ph-drawer__indicator-led-animation", true).Build();
                break;
            case PhDrawerLockState.ClosedUnlocked:
                IndicatorLedCss = new CssBuilder("").AddClass("ph-drawer__indicator-led", true).Build();
                break;
            case PhDrawerLockState.Open:
                IndicatorLedCss = string.Empty;
                break;
            default:
                break;
        }
    }

    private void SetDrawerContainerCss()
    {
        DrawerContainerCss = new CssBuilder("ph-drawer__placeholder-container")
            .AddClass("ph-drawer__placeholder-container--drawer-scan", IsScanningOngoing)
            .AddClass("ph-drawer__placeholder-container--highlight", DoorState == PhDrawerLockState.Open)
            .AddClass("ph-drawer__placeholder-container--dim", IsDrawerDim)
            .Build();
    }

    #endregion
}

public enum PhDrawerLockState
{
    ClosedLocked,
    UnlockInProgress,
    ClosedUnlocked,
    Open
}

public enum DrawerPosition
{
    Center,
    Left,
    Right
}