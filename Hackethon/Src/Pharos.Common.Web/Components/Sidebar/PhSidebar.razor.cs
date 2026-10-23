using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Sidebar;

/// <summary>
/// Sidebar component to use in the main app layout.
/// Divided into sections.
/// Has a gray background color with optional gaps that use the global background color.
/// </summary>
public partial class PhSidebar
{
    /// <summary>
    /// Optional header section used for buttons.
    /// </summary>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>
    /// Main content that fills the sidebar.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Optional footer section. The height is determined by its contents.
    /// </summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>
    /// Leaves a gap between the sidebar and the top status bar and between the footer and the rest of the sidebar content.
    /// </summary>
    [Parameter] public bool UseSpacing { get; set; }

    /// <summary>
    /// Scrolls the main content vertically if it overflows. Use this if the height of the sidebar is fixed.
    /// </summary>
    [Parameter] public bool Scroll { get; set; }

    private string CssClasses => new CssBuilder("ph-sidebar")
        .AddClass("ph-sidebar--spaced", UseSpacing)
        .AddClass("ph-sidebar--scroll", Scroll)
        .Build();
}