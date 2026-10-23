using Pharos.Common.Web.Components.Popover;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Menu;

public partial class PhMenuGroup
{
    #region Parameters
    [CascadingParameter] internal MenuContext? MenuContext { get; set; }
    [Parameter] public SubMenuType? SubMenu { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public bool Disabled { get; set; }

    [Parameter, EditorRequired] public string? Text { get; set; }

    /// <summary>
    /// Hide sub menu if the logged in user does not have the specified permission
    /// </summary>
    [Parameter] public string? Permission { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }

    #endregion


    public enum SubMenuType
    {
        Expanding, // Reagents
        FloatingRight, // More
    }

    private bool _isOpen;

    private PhPopover? _popover;

    protected string CssClassItems => new CssBuilder("ph-menu")
        .AddClass("ph-menu--floating", SubMenu == SubMenuType.FloatingRight)
        .AddClass("ph-menu--submenu", SubMenu == SubMenuType.Expanding)
        .AddClass("ph-menu--open", SubMenu == SubMenuType.Expanding && _isOpen)
        .Build();

    protected string? PopoverId => SubMenu == SubMenuType.FloatingRight ? "phmenugroup-more" : null;

    protected bool ShowCurrentSubMenuItemBox => SubMenu == SubMenuType.FloatingRight && MenuContext?.HasActiveRouteSubMenu != true;

    protected async Task OnClickGroup(MouseEventArgs ev)
    {
        await OnClick.InvokeAsync();
        if (Disabled)
        {
            return;
        }

        _isOpen = !_isOpen;
    }

    private void SetOpenState(bool isOpen)
    {
        _isOpen = isOpen;
    }

    private void ClosePopover()
    {
        _popover?.Hide();
    }
}