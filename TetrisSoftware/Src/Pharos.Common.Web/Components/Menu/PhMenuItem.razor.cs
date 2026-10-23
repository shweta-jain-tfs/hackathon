using Microsoft.AspNetCore.Components.Routing;
using Pharos.Common.Web.Components.Icons;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Menu;

public partial class PhMenuItem
{
    [Parameter, EditorRequired] public string? Uri { get; set; }
    [Parameter, EditorRequired] public string? Text { get; set; }
    [Parameter] public int? Number { get; set; }
    [Parameter] public NotificationLevel NotificationLevel { get; set; } = NotificationLevel.Info;

    /// <summary>
    /// Hide menu item if the logged in user does not have the specified permission
    /// </summary>
    [Parameter] public string? Permission { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>
    /// Whether to light up the menu item when the current URL is a child page.
    /// Set to <see cref="NavLinkMatch.All"/> to only match the current URI.
    /// </summary>
    [Parameter] public NavLinkMatch Match { get; set; } = NavLinkMatch.Prefix;

    [Parameter] public IconType? IconType { get; set; }

    private string NotificationCssClasses => new CssBuilder("ph-menu__notification")
        .AddClass("ph-menu__notification--warning", NotificationLevel == NotificationLevel.Warning)
        .AddClass("ph-menu__notification--error", NotificationLevel == NotificationLevel.Error)
        .Build();

    protected async Task OnClickHandler(MouseEventArgs ev)
    {
        await OnClick.InvokeAsync();
    }
}